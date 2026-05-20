#pragma warning disable CS8602, CS8604, CS8603
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.Services.Expirio.Exceptions;
using HEAppE.Services.Expirio.Models;
using Microsoft.Extensions.Configuration;
using System.Net;
using HEAppE.Services.Expirio.Configuration;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.Extensions.Logging;

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
                logger.LogDebug($"[Expirio Response] Success ({response.StatusCode}). Content length: {content.Length}. Content: {content}");
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<KerberosCredentialResponse>(content, options);
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
                logger.LogDebug($"[Expirio Response] Success ({response.StatusCode}). Content: {content}");
                return ParseTokenResponse(content, logger);
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

    public async Task<bool> ExchangeTokensAsync(string fipToken, string hpcToken, ILogger logger, CancellationToken cancellationToken = default)
    {
        // Placeholder implementation
        return true;
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
}