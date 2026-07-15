#pragma warning disable CS8602, CS8604, CS8603
using System.Reflection;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.Services.Expirio.Configuration;
using HEAppE.Services.Expirio.Exceptions;
using HEAppE.Services.Expirio.Models;

namespace HEAppE.Services.Expirio;

public class ExpirioService : IExpirioService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private const string CLIENT_NAME = "ExpirioClient";

    public ExpirioService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> ExchangeTokenForKerberosAsync(KerberosExchangeRequest request, string token, ILogger logger, CancellationToken cancellationToken = default)
    {
        var response = await PerformKerberosExchangeAsync(request, token, logger, cancellationToken);
        return response?.Content;
    }

    public async Task<string?> GetEnrichedUsernameAsync(string token, ILogger logger, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[Expirio] Method: GetEnrichedUsername");
        
        var request = new KerberosExchangeRequest
        {
            ProviderName = ExpirioSettings.ProviderName
        };

        var response = await PerformKerberosExchangeAsync(request, token, logger, cancellationToken);
        return response?.PreferredUsername;
    }

    private async Task<KerberosCredentialResponse> PerformKerberosExchangeAsync(KerberosExchangeRequest request, string token, ILogger logger, CancellationToken cancellationToken)
    {
        var jsonRequest = JsonSerializer.Serialize(request);
        var url = $"{ExpirioSettings.BaseUrl}/kerberos/exchange";
        
        logger.LogDebug($"[Expirio Request] POST {url} | Body: {jsonRequest}");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json")
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var client = _httpClientFactory.CreateClient(CLIENT_NAME);
        try 
        {
            using var response = await client.SendAsync(httpRequest, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var deserialized = JsonSerializer.Deserialize<KerberosCredentialResponse>(content, options);
                logger.LogDebug($"[Expirio Response] Success ({response.StatusCode}). Content length: {content.Length}. Content: {HEAppE.Utils.StringUtils.MaskToken(deserialized?.Content)}");
                return deserialized;
            }
            else
            {
                HandleErrorResponse(response, content, "Kerberos ticket exchange", logger);
                return null; 
            }
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, $"[Expirio Timeout] Request to {url} timed out.");
            throw new ExpirioUpstreamException("Request to Expirio timed out", ex, "Connection to Expirio service timed out.");
        }
        catch (JsonException ex)
        {
            logger.LogError($"[Expirio] JSON Parsing failed: {ex.Message}");
            return null;
        }
    }

    public async Task<string> ExchangeTokenAsync(ExchangeRequest request, string token, ILogger logger, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[Expirio] Method: ExchangeToken");

        var jsonRequest = JsonSerializer.Serialize(request);
        var url = $"{ExpirioSettings.BaseUrl}/exchange";

        logger.LogDebug($"[Expirio Request] POST {url} | Body: {jsonRequest}");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json")
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var client = _httpClientFactory.CreateClient(CLIENT_NAME);
        try
        {
            using var response = await client.SendAsync(httpRequest, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var exchangedToken = ParseTokenResponse(content, logger);
                logger.LogDebug($"[Expirio Response] Success ({response.StatusCode}). Content: {HEAppE.Utils.StringUtils.MaskToken(exchangedToken)}");
                return exchangedToken;
            }
            else
            {
                HandleErrorResponse(response, content, "data", logger);
                return null;
            }
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, $"[Expirio Timeout] Request to {url} timed out.");
            throw new ExpirioUpstreamException("Request to Expirio timed out", ex, "Connection to Expirio service timed out.");
        }
    }

    private void HandleErrorResponse(HttpResponseMessage response, string content, string context, ILogger logger)
    {
        string details = $"Status code: {response.StatusCode}.\nReason: {response.ReasonPhrase}.\nContent: {content}";
        logger.LogError($"[Expirio Error] Exchange failed for {context}. Details: {details}");

        switch (response.StatusCode)
        {
            case HttpStatusCode.BadRequest:
                throw new ExpirioBadRequestException($"Bad Request on Expirio {context} request", details);
            case HttpStatusCode.Unauthorized:
                throw new ExpirioUnauthorizedException($"Unauthorized Expirio {context} request", details);
            case HttpStatusCode.NotFound:
                throw new ExpirioNotFoundException($"Not Found on Expirio {context} request", details);
            case HttpStatusCode.InternalServerError:
                throw new ExpirioServerException($"Internal Server Error on Expirio {context} request", details);
            case HttpStatusCode.BadGateway:
                throw new ExpirioUpstreamException($"Bad Gateway on Expirio {context} request", details);
            default:
                throw new ExpirioException($"Error while getting Expirio {context}.", details);
        }
    }

    public Task<bool> ExchangeTokensAsync(string idpToken, string hpcToken, ILogger logger, CancellationToken cancellationToken = default)
    {
        // Placeholder implementation
        return Task.FromResult(true);
    }

    private string ParseTokenResponse(string content, ILogger logger)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var response = JsonSerializer.Deserialize<ExchangeResponse>(content, options);
            return response?.Content;
        }
        catch (JsonException ex)
        {
            logger.LogError($"[Expirio] JSON Parsing failed: {ex.Message}");
            return null;
        }
    }

    public async Task<Dictionary<string, dynamic>> ExchangeFirecrestCredentialsAsync(string token, Dictionary<string, string> customConfiguration, ILogger logger, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[Expirio] Method: FirecrestCredentials");
        var result = new Dictionary<string, dynamic>();

        if (customConfiguration == null || !customConfiguration.TryGetValue("ExpirioSecretName", out var secretName) || string.IsNullOrEmpty(secretName))
        {
            logger.LogWarning("[Expirio] ExchangeFirecrestCredentialsAsync skipped: ExpirioSecretName is missing or empty in custom configuration.");
            return result;
        }

        var client = _httpClientFactory.CreateClient(CLIENT_NAME);

        var httpUrl = $"{ExpirioSettings.BaseUrl}/secret/text/{secretName}";
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, httpUrl);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.SendAsync(httpRequest, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            logger.LogDebug($"[Expirio Response] Success ({response.StatusCode}).");

            using var doc = JsonDocument.Parse(content);

            JsonElement targetElement = doc.RootElement;
            if (doc.RootElement.TryGetProperty("metadata", out var metadataProp))
            {
                targetElement = metadataProp;
            }

            var clientIdKey = customConfiguration.TryGetValue("ExpirioClientIdKey", out var cIdKey) ? cIdKey : "client_id";
            var clientSecretKey = customConfiguration.TryGetValue("ExpirioClientSecretKey", out var cSecKey) ? cSecKey : "client_secret";

            string? clientId = null, clientSecret = null;

            // extract properties
            if (targetElement.TryGetProperty(clientIdKey, out var contentProp))
                clientId = contentProp.GetString();
            else if (targetElement.TryGetProperty("clientId", out contentProp))
                clientId = contentProp.GetString();

            if (targetElement.TryGetProperty(clientSecretKey, out contentProp))
                clientSecret = contentProp.GetString();
            else if (targetElement.TryGetProperty("clientSecret", out contentProp))
                clientSecret = contentProp.GetString();

            // add them to result if they exist
            if (!String.IsNullOrEmpty(clientId))
                result.Add("clientId", clientId);

            if (!String.IsNullOrEmpty(clientSecret))
                result.Add("clientSecret", clientSecret);

            logger.LogDebug($"[Expirio Response] Success ({response.StatusCode}). ClientId: {clientId}, ClientSecret: {HEAppE.Utils.StringUtils.MaskToken(clientSecret)}");

            return result;
        }
        else
        {
            HandleErrorResponse(response, content, "firecrest", logger);
        }

        return result;
    }
}