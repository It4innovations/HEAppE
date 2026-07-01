using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HEAppE.Exceptions.Internal;

namespace HEAppE.Services.FirecRest;

public class FirecRestTokenService : IFirecRestTokenService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FirecRestTokenService> _logger;

    public FirecRestTokenService(IHttpClientFactory httpClientFactory, ILogger<FirecRestTokenService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> GetTokenAsync(string clientId, string clientSecret, string firecRestIdpUrl)
    {
        _logger.LogInformation("[FirecRestTokenService] Method: GetToken");
        _logger.LogDebug($"[FirecRestTokenService Request] POST {firecRestIdpUrl} | Body: grant_type=client_credentials, client_id={clientId}, client_secret=***");

        try
        {
            var tokenRequestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            });

            var httpClient = _httpClientFactory.CreateClient("FirecREST");
            using var request = new HttpRequestMessage(HttpMethod.Post, firecRestIdpUrl);
            request.Content = tokenRequestContent;

            var tokenResponse = await httpClient.SendAsync(request);
            var responseContent = await tokenResponse.Content.ReadAsStringAsync();

            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogError($"[FirecRestTokenService Response] Failure ({tokenResponse.StatusCode}).");
                throw new FirecRestException($"Failed to obtain OAuth2 token for FirecRest API. Status: {tokenResponse.StatusCode}.")
                {
                    CommandError = "Token request failed"
                };
            }

            var tokenData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            if (tokenData.TryGetProperty("access_token", out var accessTokenElement) && accessTokenElement.GetString() is { } token)
            {
                _logger.LogDebug($"[FirecRestTokenService Response] Success ({tokenResponse.StatusCode}). Token: {HEAppE.Utils.StringUtils.MaskToken(token)}");
                return token;
            }

            throw new FirecRestException("Invalid OAuth2 response: access_token not found or is null")
            {
                CommandError = "Missing access token"
            };
        }
        catch (Exception ex) when (ex is not FirecRestException)
        {
            _logger.LogError(ex, $"Failed to retrieve FirecRest authentication token: {ex.Message}");
            throw new FirecRestException($"Failed to retrieve FirecRest authentication token: {ex.Message}", ex)
            {
                CommandError = "Token retrieval exception"
            };
        }
    }
}
