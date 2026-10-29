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

public class ClientCredentialsExchangeStrategy : ITokenExchangeStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ClientCredentialsExchangeStrategy> _logger;
    private readonly ConcurrentDictionary<string, string> _tokenEndpointCache = new();

    public string StrategyName => "ClientCredentials";

    public ClientCredentialsExchangeStrategy(
        IHttpClientFactory httpClientFactory,
        ILogger<ClientCredentialsExchangeStrategy> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<TokenExchangeResult> ExchangeAsync(
        TokenExchangeTargetConfiguration target,
        string subjectToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenEndpoint = await GetTokenEndpointAsync(target, cancellationToken);
            if (string.IsNullOrEmpty(tokenEndpoint))
            {
                return TokenExchangeResult.Failed(target.Name, "Token endpoint could not be determined.");
            }

            var client = _httpClientFactory.CreateClient("TokenExchangeClient");
            
            var formData = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" }
            };

            if (!string.IsNullOrEmpty(target.Scope))
            {
                formData.Add("scope", target.Scope);
            }
            
            if (!string.IsNullOrEmpty(target.ClientId) && !string.IsNullOrEmpty(target.ClientSecret))
            {
                var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{target.ClientId}:{target.ClientSecret}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(formData)
            };

            var response = await client.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Client credentials exchange failed for target {Target}. Status: {Status}. Response: {Response}", target.Name, response.StatusCode, content);
                return TokenExchangeResult.Failed(target.Name, $"HTTP {response.StatusCode}: {content}");
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            string? accessToken = null;
            if (root.TryGetProperty("access_token", out var accessTokenProp))
            {
                accessToken = accessTokenProp.GetString();
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                return TokenExchangeResult.Failed(target.Name, "Response did not contain an access_token.");
            }

            var result = TokenExchangeResult.Succeeded(target.Name, accessToken);

            if (root.TryGetProperty("expires_in", out var expiresInProp) && expiresInProp.TryGetInt32(out var expiresIn))
            {
                result.ExpiresIn = expiresIn;
            }

            if (root.TryGetProperty("issued_token_type", out var issuedTokenTypeProp))
            {
                result.IssuedTokenType = issuedTokenTypeProp.GetString();
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during client credentials exchange for target {Target}.", target.Name);
            return TokenExchangeResult.Failed(target.Name, ex.Message);
        }
    }

    private async Task<string> GetTokenEndpointAsync(TokenExchangeTargetConfiguration target, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(target.TokenEndpoint))
        {
            return target.TokenEndpoint;
        }

        var authority = target.Authority;
        if (string.IsNullOrEmpty(authority))
        {
            authority = JwtTokenIntrospectionConfiguration.Authority;
        }

        if (string.IsNullOrEmpty(authority))
        {
            return string.Empty;
        }

        if (_tokenEndpointCache.TryGetValue(authority, out var cachedEndpoint))
        {
            return cachedEndpoint;
        }

        var discoveryEndpoint = authority.TrimEnd('/') + "/.well-known/openid-configuration";
        var client = _httpClientFactory.CreateClient("TokenExchangeClient");

        try
        {
            var response = await client.GetAsync(discoveryEndpoint, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("token_endpoint", out var tokenEndpointProp))
                {
                    var tokenEndpoint = tokenEndpointProp.GetString();
                    if (!string.IsNullOrEmpty(tokenEndpoint))
                    {
                        _tokenEndpointCache[authority] = tokenEndpoint;
                        return tokenEndpoint;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to discover token endpoint from {DiscoveryEndpoint}.", discoveryEndpoint);
        }

        return string.Empty;
    }
}
