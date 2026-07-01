using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
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
}

public class LexisTokenService : ILexisTokenService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LexisTokenService> _log;

    public LexisTokenService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<LexisTokenService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _log = logger;
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