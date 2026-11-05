using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.ExternalAuthentication.Configuration;
using Microsoft.Extensions.Logging;

namespace HEAppE.Services.TokenExchange.Strategies;

/// <summary>
/// Strategy that validates a subject token against an OAuth2/OIDC introspection endpoint (RFC 7662)
/// and passes the original subject token through when active.
/// Used both as a standalone strategy ("Introspection") and for pre-exchange introspection
/// (when <see cref="TokenExchangeTargetConfiguration.IntrospectSubjectToken"/> is true).
/// </summary>
public class IntrospectionExchangeStrategy : ITokenExchangeStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<IntrospectionExchangeStrategy> _logger;
    private readonly ConcurrentDictionary<string, string> _introspectionEndpointCache = new();

    public string StrategyName => "Introspection";

    public IntrospectionExchangeStrategy(
        IHttpClientFactory httpClientFactory,
        ILogger<IntrospectionExchangeStrategy> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<TokenExchangeResult> ExchangeAsync(
        TokenExchangeTargetConfiguration target,
        string subjectToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subjectToken))
        {
            return TokenExchangeResult.Failed(target.Name, "Subject token is null or empty.");
        }

        try
        {
            var introspectionEndpoint = await GetIntrospectionEndpointAsync(target, cancellationToken);
            if (string.IsNullOrEmpty(introspectionEndpoint))
            {
                return TokenExchangeResult.Failed(target.Name, "Introspection endpoint could not be determined.");
            }

            var client = _httpClientFactory.CreateClient("TokenExchangeClient");

            var clientId = ResolveFirstNonEmpty(
                target.IntrospectionClientId,
                target.ClientId,
                JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration?.ClientId,
                JwtTokenIntrospectionConfiguration.ClientId);

            var clientSecret = ResolveFirstNonEmpty(
                target.IntrospectionClientSecret,
                target.ClientSecret,
                JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration?.ClientSecret,
                JwtTokenIntrospectionConfiguration.ClientSecret);

            var formData = new Dictionary<string, string>
            {
                { "token", subjectToken },
                { "token_type_hint", "access_token" }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, introspectionEndpoint)
            {
                Content = new FormUrlEncodedContent(formData)
            };

            if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
            {
                var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
            }

            var response = await client.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Token introspection failed for target {Target} at {Endpoint}. Status: {Status}. Response: {Response}",
                    target.Name, introspectionEndpoint, response.StatusCode, content);
                return TokenExchangeResult.Failed(target.Name, $"HTTP {response.StatusCode}: {content}");
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            bool isActive = root.TryGetProperty("active", out var activeProp) &&
                            activeProp.ValueKind == JsonValueKind.True;

            if (!isActive)
            {
                _logger.LogWarning("Token introspection for target {Target} returned active=false.", target.Name);
                return TokenExchangeResult.Failed(target.Name, "Token introspection failed: token is not active.");
            }

            if (root.TryGetProperty("exp", out var expProp) && expProp.TryGetInt64(out var exp))
            {
                if (DateTimeOffset.FromUnixTimeSeconds(exp) <= DateTimeOffset.UtcNow)
                {
                    _logger.LogWarning("Token introspection for target {Target} returned expired token (exp={Exp}).", target.Name, exp);
                    return TokenExchangeResult.Failed(target.Name, "Token introspection failed: token has expired.");
                }
            }

            _logger.LogDebug("Token introspection succeeded for target {Target}.", target.Name);
            return TokenExchangeResult.Succeeded(target.Name, subjectToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during token introspection for target {Target}.", target.Name);
            return TokenExchangeResult.Failed(target.Name, ex.Message);
        }
    }

    internal async Task<string> GetIntrospectionEndpointAsync(
        TokenExchangeTargetConfiguration target,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(target.IntrospectionEndpoint))
        {
            return target.IntrospectionEndpoint;
        }

        var authority = ResolveFirstNonEmpty(
            target.IntrospectionAuthority,
            target.Authority,
            JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration?.GetEffectiveAuthority(),
            JwtTokenIntrospectionConfiguration.Authority);

        if (string.IsNullOrWhiteSpace(authority))
        {
            return string.Empty;
        }

        authority = authority.TrimEnd('/');

        if (_introspectionEndpointCache.TryGetValue(authority, out var cachedEndpoint))
        {
            return cachedEndpoint;
        }

        var discoveryEndpoint = authority + "/.well-known/openid-configuration";
        var client = _httpClientFactory.CreateClient("TokenExchangeClient");

        try
        {
            var response = await client.GetAsync(discoveryEndpoint, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("introspection_endpoint", out var endpointProp))
                {
                    var introspectionEndpoint = endpointProp.GetString();
                    if (!string.IsNullOrEmpty(introspectionEndpoint))
                    {
                        _introspectionEndpointCache[authority] = introspectionEndpoint;
                        return introspectionEndpoint;
                    }
                }
            }
            else
            {
                _logger.LogWarning(
                    "Failed to discover introspection endpoint from {DiscoveryEndpoint}. Status: {Status}",
                    discoveryEndpoint, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to discover introspection endpoint from {DiscoveryEndpoint}.", discoveryEndpoint);
        }

        if (authority.Contains("/realms/", StringComparison.OrdinalIgnoreCase))
        {
            var fallbackEndpoint = $"{authority}/protocol/openid-connect/token/introspect";
            _introspectionEndpointCache[authority] = fallbackEndpoint;
            return fallbackEndpoint;
        }

        return string.Empty;
    }

    private static string ResolveFirstNonEmpty(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }
}
