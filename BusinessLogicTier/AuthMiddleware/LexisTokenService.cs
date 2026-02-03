using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using HEAppE.ExternalAuthentication.Configuration;
using Microsoft.Extensions.Configuration;

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

    public LexisTokenService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    /// <summary>
    /// Exchanges an existing LEXIS-issued access token for a FIP (HEAppE) access token.
    /// </summary>
    public async Task<string> ExchangeLexisTokenForFipAsync(string lexisAccessToken)
    {
        if (string.IsNullOrWhiteSpace(lexisAccessToken))
        {
            //throw new ArgumentException("LEXIS access token is required.", nameof(lexisAccessToken));
            return null;
        }

        
        var client = _httpClientFactory.CreateClient("LexisTokenExchangeClient");
        var cfg = JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration;

        var tokenEndpoint = $"{cfg.BaseUrl}/realms/{cfg.Realm}/protocol/openid-connect/token";

        var exchangeRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:token-exchange",
            ["client_id"] = cfg.ClientId,
            ["client_secret"] = cfg.ClientSecret,
            ["subject_token"] = lexisAccessToken,
            ["subject_token_type"] = "urn:ietf:params:oauth:token-type:access_token",
            ["requested_token_type"] = "urn:ietf:params:oauth:token-type:access_token",
            ["scope"] = cfg.Scope
        });

        var response = await client.PostAsync(tokenEndpoint, exchangeRequest);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Token exchange failed: {response.StatusCode} - {error}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        string exchangedAccessToken = json.GetProperty("access_token").GetString();
        //call GetFipTokenInfoAsync
        var fipTokenInfo = await GetFipTokenInfoAsync(exchangedAccessToken);
        return fipTokenInfo;
    }

    /// <summary>
    /// Retrieves FIP token info from EFP broker using the exchanged token.
    /// </summary>
    private async Task<string> GetFipTokenInfoAsync(string exchangedLexisAccessToken)
    {
        if (string.IsNullOrWhiteSpace(exchangedLexisAccessToken))
            throw new ArgumentException("FIP access token is required.", nameof(exchangedLexisAccessToken));

        var cfg = JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration;
        var userinfoUrl = $"{cfg.BaseUrl}/realms/{cfg.Realm}/broker/{cfg.Broker}/token";

        var client = _httpClientFactory.CreateClient("LexisTokenExchangeClient");
        var request = new HttpRequestMessage(HttpMethod.Get, userinfoUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", exchangedLexisAccessToken);

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to retrieve FIP token info: {response.StatusCode} - {error}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("access_token").GetString();
    }
}
