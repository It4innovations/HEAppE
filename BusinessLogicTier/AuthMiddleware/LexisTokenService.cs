using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Exceptions.External;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HEAppE.Services.AuthMiddleware;

public interface ILexisTokenService
{
    Task<string> ExchangeLexisTokenForIdpAsync(string lexisAccessToken);
    Task IntrospectLexisTokenAsync(string lexisAccessToken);
}

public class LexisTokenService : ILexisTokenService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LexisTokenService> _log;
    private readonly ConcurrentDictionary<string, string> _introspectionEndpointCache = new();

    public LexisTokenService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<LexisTokenService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _log = logger;
    }

    public async Task IntrospectLexisTokenAsync(string lexisAccessToken)
    {
        if (string.IsNullOrWhiteSpace(lexisAccessToken))
        {
            _log.LogWarning("IntrospectLexisTokenAsync: lexisAccessToken is null or empty.");
            throw new AuthenticationTypeException("InvalidToken", "LexisTokenIntrospection")
            {
                ServiceName = "LexisTokenIntrospection",
                Details = "LEXIS access token is null or empty."
            };
        }

        var cfg = JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration;
        var introspectionEndpoint = await GetLexisIntrospectionEndpointAsync(cfg);

        if (string.IsNullOrWhiteSpace(introspectionEndpoint))
        {
            _log.LogError("IntrospectLexisTokenAsync: Introspection endpoint could not be determined.");
            throw new AuthenticationTypeException("ExternalApiError", "LexisTokenIntrospection")
            {
                ServiceName = "LexisTokenIntrospection",
                Details = "LEXIS Keycloak introspection endpoint is not configured or could not be discovered."
            };
        }

        var clientId = !string.IsNullOrWhiteSpace(cfg.ClientId) ? cfg.ClientId : ExternalAuthConfiguration.ClientId;
        var clientSecret = !string.IsNullOrWhiteSpace(cfg.ClientSecret) ? cfg.ClientSecret : ExternalAuthConfiguration.SecretId;

        var payload = new Dictionary<string, string>
        {
            ["token"] = lexisAccessToken,
            ["token_type_hint"] = "access_token"
        };

        var client = _httpClientFactory.CreateClient("LexisTokenExchangeClient");
        using var request = new HttpRequestMessage(HttpMethod.Post, introspectionEndpoint)
        {
            Content = new FormUrlEncodedContent(payload)
        };

        if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
        {
            var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
        }

        _log.LogDebug("[LexisIntrospection Request] URL: {IntrospectionEndpoint}, ClientID: {ClientId}", introspectionEndpoint, clientId);

        try
        {
            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _log.LogError("[LexisIntrospection Response] Error: {StatusCode}", response.StatusCode);
                throw new AuthenticationTypeException("ExternalApiError", "LexisTokenIntrospection")
                {
                    ServiceName = "LexisTokenIntrospection",
                    Details = $"LEXIS token introspection failed with status code {response.StatusCode}."
                };
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            bool isActive = root.TryGetProperty("active", out var activeProp) &&
                            activeProp.ValueKind == JsonValueKind.True;

            if (!isActive)
            {
                _log.LogWarning("[LexisIntrospection Response] Token is not active.");
                throw new AuthenticationTypeException("InvalidToken", "LexisTokenIntrospection")
                {
                    ServiceName = "LexisTokenIntrospection",
                    Details = "LEXIS token introspection failed: token is not active."
                };
            }

            if (root.TryGetProperty("exp", out var expProp) && expProp.TryGetInt64(out var exp))
            {
                if (DateTimeOffset.FromUnixTimeSeconds(exp) <= DateTimeOffset.UtcNow)
                {
                    _log.LogWarning("[LexisIntrospection Response] Token has expired (exp={Exp}).", exp);
                    throw new AuthenticationTypeException("InvalidToken", "LexisTokenIntrospection")
                    {
                        ServiceName = "LexisTokenIntrospection",
                        Details = "LEXIS token introspection failed: token has expired."
                    };
                }
            }

            _log.LogDebug("[LexisIntrospection Response] Success: token is active.");
        }
        catch (AuthenticationTypeException)
        {
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _log.LogError(ex, "[LexisIntrospection Timeout] Request to {IntrospectionEndpoint} timed out.", introspectionEndpoint);
            throw new AuthenticationTypeException("ExternalApiTimeout", ex, "LexisTokenIntrospection")
            {
                ServiceName = "LexisTokenIntrospection",
                Details = $"LEXIS token introspection request to {introspectionEndpoint} timed out."
            };
        }
        catch (JsonException ex)
        {
            _log.LogError("[LexisIntrospection] Invalid JSON format.");
            throw new AuthenticationTypeException("ExternalApiError", ex, "LexisTokenIntrospection")
            {
                ServiceName = "LexisTokenIntrospection",
                Details = $"Failed to parse LEXIS token introspection response: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[LexisIntrospection] Unexpected error.");
            throw new AuthenticationTypeException("ExternalApiError", ex, "LexisTokenIntrospection")
            {
                ServiceName = "LexisTokenIntrospection",
                Details = $"Unexpected error during LEXIS token introspection: {ex.Message}"
            };
        }
    }

    private async Task<string> GetLexisIntrospectionEndpointAsync(LexisTokenFlowConfiguration cfg)
    {
        if (!string.IsNullOrWhiteSpace(cfg.IntrospectionEndpoint))
        {
            return cfg.IntrospectionEndpoint;
        }

        var authority = cfg.GetEffectiveAuthority();
        if (string.IsNullOrWhiteSpace(authority) && !string.IsNullOrWhiteSpace(ExternalAuthConfiguration.BaseUrl))
        {
            authority = !string.IsNullOrWhiteSpace(ExternalAuthConfiguration.RealmName)
                ? $"{ExternalAuthConfiguration.BaseUrl.TrimEnd('/')}/realms/{ExternalAuthConfiguration.RealmName}"
                : ExternalAuthConfiguration.BaseUrl.TrimEnd('/');
        }

        if (string.IsNullOrWhiteSpace(authority))
        {
            return string.Empty;
        }

        if (_introspectionEndpointCache.TryGetValue(authority, out var cachedEndpoint))
        {
            return cachedEndpoint;
        }

        var discoveryEndpoint = $"{authority}/.well-known/openid-configuration";
        var client = _httpClientFactory.CreateClient("LexisTokenExchangeClient");

        try
        {
            var response = await client.GetAsync(discoveryEndpoint);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("introspection_endpoint", out var endpointProp))
                {
                    var discovered = endpointProp.GetString();
                    if (!string.IsNullOrEmpty(discovered))
                    {
                        _introspectionEndpointCache[authority] = discovered;
                        return discovered;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[LexisIntrospection] Failed to discover introspection endpoint from {DiscoveryEndpoint}.", discoveryEndpoint);
        }

        var fallbackEndpoint = $"{authority}/protocol/openid-connect/token/introspect";
        _introspectionEndpointCache[authority] = fallbackEndpoint;
        return fallbackEndpoint;
    }

    public async Task<string> ExchangeLexisTokenForIdpAsync(string lexisAccessToken)
    {
        if (string.IsNullOrWhiteSpace(lexisAccessToken))
        {
            _log.LogWarning("ExchangeLexisTokenForIdpAsync: lexisAccessToken is null or empty.");
            return null;
        }

        var client = _httpClientFactory.CreateClient("LexisTokenExchangeClient");
        var cfg = JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration;
        var tokenEndpoint = $"{cfg.BaseUrl}/realms/{cfg.Realm}/protocol/openid-connect/token";

        var payload = new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:token-exchange",
            ["client_id"] = cfg.ClientId,
            ["client_secret"] = cfg.ClientSecret,
            ["subject_token"] = lexisAccessToken,
            ["subject_token_type"] = "urn:ietf:params:oauth:token-type:access_token",
            ["requested_token_type"] = "urn:ietf:params:oauth:token-type:access_token",
            ["scope"] = cfg.Scope
        };

        _log.LogDebug("[TokenExchange Request] URL: {TokenEndpoint}, ClientID: {ClientId}, Scope: {Scope}", tokenEndpoint, cfg.ClientId, cfg.Scope);

        try
        {
            var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(payload));
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _log.LogError("[TokenExchange Response] Error: {StatusCode}", response.StatusCode);
                throw new AuthenticationTypeException("ExternalApiError", "LexisTokenExchange")
                {
                    ServiceName = "LexisTokenExchange",
                    Details = $"Token exchange failed with status code {response.StatusCode}."
                };
            }

            _log.LogDebug("[TokenExchange Response] Success: {StatusCode}", response.StatusCode);

            var json = JsonSerializer.Deserialize<JsonElement>(responseContent);
            string exchangedAccessToken = json.GetProperty("access_token").GetString();
            return await GetIdpTokenInfoAsync(exchangedAccessToken);
        }
        catch (TaskCanceledException ex)
        {
            _log.LogError(ex, "[TokenExchange Timeout] Request to {TokenEndpoint} timed out.", tokenEndpoint);
            throw new AuthenticationTypeException("ExternalApiTimeout", ex, "LexisTokenExchange") 
            { 
                ServiceName = "LexisTokenExchange",
                Details = $"Token exchange request to {tokenEndpoint} timed out." 
            };
        }
        catch (JsonException ex)
        {
            _log.LogError("[TokenExchange] Invalid JSON format.");
            throw new AuthenticationTypeException("ExternalApiError", ex, "LexisTokenExchange")
            {
                ServiceName = "LexisTokenExchange",
                Details = $"Failed to parse token exchange response: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[TokenExchange] Unexpected error.");
            throw new AuthenticationTypeException("ExternalApiError", ex, "LexisTokenExchange")
            {
                ServiceName = "LexisTokenExchange",
                Details = $"Unexpected error during LEXIS token exchange: {ex.Message}"
            };
        }
    }

    private async Task<string> GetIdpTokenInfoAsync(string exchangedLexisAccessToken)
    {
        if (string.IsNullOrWhiteSpace(exchangedLexisAccessToken))
            throw new ArgumentException("IdP access token is required.", nameof(exchangedLexisAccessToken));

        var cfg = JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration;
        var userinfoUrl = $"{cfg.BaseUrl}/realms/{cfg.Realm}/broker/{cfg.Broker}/token";

        _log.LogDebug("[IdpTokenInfo Request] URL: {UserinfoUrl}", userinfoUrl);

        var client = _httpClientFactory.CreateClient("LexisTokenExchangeClient");
        var request = new HttpRequestMessage(HttpMethod.Get, userinfoUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", exchangedLexisAccessToken);

        try
        {
            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _log.LogError("[IdpTokenInfo Response] Error: {StatusCode}", response.StatusCode);
                throw new AuthenticationTypeException("ExternalApiError", "IdpTokenInfo")
                {
                    ServiceName = "IdpTokenInfo",
                    Details = $"Failed to retrieve IdP token info with status code {response.StatusCode}."
                };
            }

            try
            {
                var json = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var token = json.GetProperty("access_token").GetString();
                _log.LogDebug("[IdpTokenInfo Response] Success. Token: {Token}", HEAppE.Utils.StringUtils.MaskToken(token));
                return token;
            }
            catch (JsonException ex)
            {
                _log.LogError("[IdpTokenInfo] Invalid JSON format.");
                throw new AuthenticationTypeException("ExternalApiError", ex, "IdpTokenInfo")
                {
                    ServiceName = "IdpTokenInfo",
                    Details = $"Failed to parse IdP token info response: {ex.Message}"
                };
            }
        }
        catch (TaskCanceledException ex)
        {
            _log.LogError(ex, "[IdpTokenInfo Timeout] Request to {UserinfoUrl} timed out.", userinfoUrl);
            throw new AuthenticationTypeException("ExternalApiTimeout", ex, "IdpTokenInfo")
            {
                ServiceName = "IdpTokenInfo",
                Details = $"Failed to retrieve IdP token info due to timeout at {userinfoUrl}."
            };
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[IdpTokenInfo] Unexpected error.");
            throw new AuthenticationTypeException("ExternalApiError", ex, "IdpTokenInfo")
            {
                ServiceName = "IdpTokenInfo",
                Details = $"Unexpected error during IdP token retrieval: {ex.Message}"
            };
        }
    }
}