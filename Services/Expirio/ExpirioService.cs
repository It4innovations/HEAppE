#pragma warning disable CS8602, CS8604, CS8603
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Services.Expirio.Exceptions;
using Services.Expirio.Models;
using Microsoft.Extensions.Configuration;
using log4net;
using System.Net;
using Services.Expirio.Configuration;
using System.Net.Http.Headers;
using System.Reflection;

namespace HEAppE.Services.Expirio;

public class ExpirioService : IExpirioService
{
    protected readonly ILog _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private const string CLIENT_NAME = "ExpirioClient";

    public ExpirioService(IHttpClientFactory httpClientFactory)
    {
        _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> ExchangeTokenForKerberosAsync(KerberosExchangeRequest request, string token, CancellationToken cancellationToken = default)
    {
        _logger.Info("[Expirio] Method: ExchangeTokenForKerberos");

        var jsonRequest = JsonSerializer.Serialize(request);
        var url = $"{ExpirioSettings.BaseUrl}/kerberos/exchange";
        
        _logger.Debug($"[Expirio Request] POST {url} | Body: {jsonRequest}");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json")
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var client = _httpClientFactory.CreateClient(CLIENT_NAME);
        using var response = await client.SendAsync(httpRequest, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            _logger.Debug($"[Expirio Response] Success ({response.StatusCode}). Content length: {content.Length}");
            return ParseTokenResponse(content);
        }
        else
        {
            HandleErrorResponse(response, content, "Kerberos ticket");
            return null; 
        }
    }

    public async Task<string> ExchangeTokenAsync(ExchangeRequest request, string token, CancellationToken cancellationToken = default)
    {
        _logger.Info("[Expirio] Method: ExchangeToken");

        var jsonRequest = JsonSerializer.Serialize(request);
        var url = $"{ExpirioSettings.BaseUrl}/exchange";

        _logger.Debug($"[Expirio Request] POST {url} | Body: {jsonRequest}");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json")
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var client = _httpClientFactory.CreateClient(CLIENT_NAME);
        using var response = await client.SendAsync(httpRequest, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            _logger.Debug($"[Expirio Response] Success ({response.StatusCode}). Content: {content}");
            return ParseTokenResponse(content);
        }
        else
        {
            HandleErrorResponse(response, content, "data");
            return null;
        }
    }

    private string ParseTokenResponse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ExpirioException("Empty response from Expirio.");

        if (content.TrimStart().StartsWith("<", StringComparison.OrdinalIgnoreCase) || content.Contains("<html", StringComparison.OrdinalIgnoreCase))
        {
            _logger.Error($"[Expirio Error] Unexpected HTML response received despite 200 OK status. Content: {content}");
            throw new ExpirioException("Failed to parse token. Received HTML instead of JSON token payload.");
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind == JsonValueKind.String)
            {
                return doc.RootElement.GetString();
            }

            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("Content", out var contentProp))
            {
                return contentProp.GetString();
            }
            
            return content.Trim('"');
        }
        catch (JsonException)
        {
            return content.Trim('"');
        }
    }

    private void HandleErrorResponse(HttpResponseMessage response, string content, string context)
    {
        string details = $"Status code: {response.StatusCode}.\nReason: {response.ReasonPhrase}.\nContent: {content}";
        _logger.Error($"[Expirio Error] Exchange failed for {context}. Details: {details}");

        switch (response.StatusCode)
        {
            case HttpStatusCode.BadRequest:
                throw new ExpirioBadRequestException($"Bad Expirio {context} request", details);
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
}