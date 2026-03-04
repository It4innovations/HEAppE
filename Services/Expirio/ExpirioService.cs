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
            var dto = JsonSerializer.Deserialize<KerberosExchangeResponse>(content);
            return dto?.Content ?? throw new ExpirioException("Empty ticket in response.");
        }
        else
        {
            string details = $"Status code: {response.StatusCode}.\nReason: {response.ReasonPhrase}.\nContent: {content}";
            _logger.Error($"[Expirio Error] Kerberos exchange failed. Details: {details}");

            switch (response.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                    throw new ExpirioBadRequestException("Bad Expirio Kerberos ticket request", details);
                case HttpStatusCode.Unauthorized:
                    throw new ExpirioUnauthorizedException("Unauthorized Expirio Kerberos ticket request", details);
                case HttpStatusCode.NotFound:
                    throw new ExpirioNotFoundException("Not Found on Expirio Kerberos ticket request", details);
                case HttpStatusCode.InternalServerError:
                    throw new ExpirioServerException("Internal Server Error on Expirio Kerberos ticket request", details);
                case HttpStatusCode.BadGateway:
                    throw new ExpirioUpstreamException("Bad Gateway on Expirio Kerberos ticket request", details);
                default:
                    throw new ExpirioException("Error while getting Expirio Kerberos ticket.", details);
            }
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
            var dto = JsonSerializer.Deserialize<ExchangeResponse>(content);
            return dto?.Content ?? throw new ExpirioException("Empty data in response.");
        }
        else
        {
            string details = $"Status code: {response.StatusCode}.\nReason: {response.ReasonPhrase}.\nContent: {content}";
            _logger.Error($"[Expirio Error] Token exchange failed. Details: {details}");

            switch (response.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                    throw new ExpirioBadRequestException("Bad Expirio data request", details);
                case HttpStatusCode.Unauthorized:
                    throw new ExpirioUnauthorizedException("Unauthorized Expirio data request", details);
                case HttpStatusCode.NotFound:
                    throw new ExpirioNotFoundException("Not Found on Expirio data request", details);
                case HttpStatusCode.InternalServerError:
                    throw new ExpirioServerException("Internal Server Error on Expirio data request", details);
                case HttpStatusCode.BadGateway:
                    throw new ExpirioUpstreamException("Bad Gateway on Expirio data request", details);
                default:
                    throw new ExpirioException("Error while getting Expirio data.", details);
            }
        }
    }
}