using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using HEAppE.ExternalAuthentication.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HEAppE.Services.AuthMiddleware;

public interface ILexisTokenService
{
    Task<string> ExchangeLexisTokenForFipAsync(string lexisAccessToken);
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

    public async Task<string> ExchangeLexisTokenForFipAsync(string lexisAccessToken)
    {
        if (string.IsNullOrWhiteSpace(lexisAccessToken))
        {
            _log.LogWarning("ExchangeLexisTokenForFipAsync: lexisAccessToken is null or empty.");
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
                _log.LogError("[TokenExchange Response] Error: {StatusCode}, Content: {Content}", response.StatusCode, responseContent);
                throw new Exception($"Token exchange failed: {response.StatusCode} - {responseContent}");
            }

            _log.LogDebug("[TokenExchange Response] Success: {StatusCode}", response.StatusCode);

            var json = JsonSerializer.Deserialize<JsonElement>(responseContent);
            string exchangedAccessToken = json.GetProperty("access_token").GetString();
            return await GetFipTokenInfoAsync(exchangedAccessToken);
        }
        catch (TaskCanceledException)
        {
            _log.LogError("[TokenExchange Timeout] Request to {TokenEndpoint} timed out.", tokenEndpoint);
            throw;
        }
        catch (JsonException ex)
        {
            _log.LogError("[TokenExchange] Invalid JSON format.");
            throw new Exception($"Failed to parse token exchange response: {ex.Message}", ex);
        }
    }

    private async Task<string> GetFipTokenInfoAsync(string exchangedLexisAccessToken)
    {
        if (string.IsNullOrWhiteSpace(exchangedLexisAccessToken))
            throw new ArgumentException("FIP access token is required.", nameof(exchangedLexisAccessToken));

        var cfg = JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration;
        var userinfoUrl = $"{cfg.BaseUrl}/realms/{cfg.Realm}/broker/{cfg.Broker}/token";

        _log.LogDebug("[FipTokenInfo Request] URL: {UserinfoUrl}", userinfoUrl);

        var client = _httpClientFactory.CreateClient("LexisTokenExchangeClient");
        var request = new HttpRequestMessage(HttpMethod.Get, userinfoUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", exchangedLexisAccessToken);

        var response = await client.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _log.LogError("[FipTokenInfo Response] Error: {StatusCode}, Content: {Content}", response.StatusCode, responseContent);
            throw new Exception($"Failed to retrieve FIP token info: {response.StatusCode} - {responseContent}");
        }

        _log.LogDebug("[FipTokenInfo Response] Success");

        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(responseContent);
            return json.GetProperty("access_token").GetString();
        }
        catch (JsonException ex)
        {
            _log.LogError("[FipTokenInfo] Invalid JSON format. Response content: {Content}", responseContent);
            throw new Exception($"Failed to parse FIP token info response: {ex.Message}", ex);
        }
    }
}