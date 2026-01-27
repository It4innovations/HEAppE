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
using HEAppE.BusinessLogicTier;

namespace Services.Expirio;

public class ExpirioService : IExpirioService
{
    protected readonly ILog _logger;
    private readonly HttpClient _httpClient;

    public ExpirioService(HttpClient httpClient)
    {
        _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        //_logger = loggerFactory.CreateLogger("HEAppE.Services.Expirio");
        _httpClient = httpClient;
    }

    public async Task<string> ExchangeTokenForKerberosAsync(KerberosExchangeRequest request, string token, CancellationToken cancellationToken = default)
    {
        _logger.Info("Endpoint: \"ExpirioService\" Method: \"ExchangeTokenForKerberos\"\n\n");

        var jsonRequest = JsonSerializer.Serialize(request);
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{ExpirioSettings.BaseUrl}/kerberos/exchange")
        {
            Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json")
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var dto = JsonSerializer.Deserialize<KerberosExchangeResponse>(content);
            return dto?.Content ?? throw new ExpirioException("Empty ticket in response.");
        }
        else
        {
            string details = $"Status code: {response.StatusCode}.\nReason: {response.ReasonPhrase}.\nContent: {content}";
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
}