using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using HEAppE.ExternalAuthentication.Configuration;
using Microsoft.Extensions.Configuration;
using log4net;

namespace HEAppE.Services.AuthMiddleware;

public interface ILexisTokenService
{
    Task<string> ExchangeLexisTokenForFipAsync(string lexisAccessToken);
    string ExchangeLexisTokenForFip(string lexisToken) =>
        ExchangeLexisTokenForFipAsync(lexisToken).GetAwaiter().GetResult();
}

public class LexisTokenService : ILexisTokenService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private static readonly ILog Log = LogManager.GetLogger(typeof(LexisTokenService));

    public LexisTokenService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<string> ExchangeLexisTokenForFipAsync(string lexisAccessToken)
    {
        if (string.IsNullOrWhiteSpace(lexisAccessToken))
        {
            Log.Warn("ExchangeLexisTokenForFipAsync: lexisAccessToken is null or empty.");
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

        Log.Debug($"[TokenExchange Request] URL: {tokenEndpoint}, ClientID: {cfg.ClientId}, Scope: {cfg.Scope}");

        var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(payload));
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Log.Error($"[TokenExchange Response] Error: {response.StatusCode}, Content: {responseContent}");
            throw new Exception($"Token exchange failed: {response.StatusCode} - {responseContent}");
        }

        Log.Debug($"[TokenExchange Response] Success: {response.StatusCode}");

        var json = JsonSerializer.Deserialize<JsonElement>(responseContent);
        string exchangedAccessToken = json.GetProperty("access_token").GetString();
        
        return await GetFipTokenInfoAsync(exchangedAccessToken);
    }

    private async Task<string> GetFipTokenInfoAsync(string exchangedLexisAccessToken)
    {
        if (string.IsNullOrWhiteSpace(exchangedLexisAccessToken))
            throw new ArgumentException("FIP access token is required.", nameof(exchangedLexisAccessToken));

        var cfg = JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration;
        var userinfoUrl = $"{cfg.BaseUrl}/realms/{cfg.Realm}/broker/{cfg.Broker}/token";

        Log.Debug($"[FipTokenInfo Request] URL: {userinfoUrl}");

        var client = _httpClientFactory.CreateClient("LexisTokenExchangeClient");
        var request = new HttpRequestMessage(HttpMethod.Get, userinfoUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", exchangedLexisAccessToken);

        var response = await client.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Log.Error($"[FipTokenInfo Response] Error: {response.StatusCode}, Content: {responseContent}");
            throw new Exception($"Failed to retrieve FIP token info: {response.StatusCode} - {responseContent}");
        }

        Log.Debug("[FipTokenInfo Response] Success");

        var json = JsonSerializer.Deserialize<JsonElement>(responseContent);
        return json.GetProperty("access_token").GetString();
    }
}