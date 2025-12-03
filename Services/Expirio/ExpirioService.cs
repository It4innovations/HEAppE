using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Services.Expirio.Exceptions;
using Services.Expirio.Models;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace Services.Expirio;

public class ExpirioService : IExpirioService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public ExpirioService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<string> ExchangeTokenForKerberosAsync(string providerName, string token, CancellationToken cancellationToken = default)
    {
        var request = new KerberosExchangeRequest { ProviderName = providerName, Token = token };
        var jsonRequest = JsonSerializer.Serialize(request);
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/kerberos/exchange")
        {
            Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json")
        };
        // Add API token header if configured
        //if (!string.IsNullOrEmpty(_configuration.ApiToken))
        //    httpRequest.Headers.Add("X-Api-Token", _configuration.ApiToken);

        var httpClient = _httpClientFactory.CreateClient("ExpirioTokenForKerberosExchangeClient");
        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var dto = JsonSerializer.Deserialize<KerberosExchangeResponse>(content);
            return dto?.Ticket ?? throw new ExpirioException("Empty ticket in response.");
        }
        else
        {
            string details = $"Status code: {response.StatusCode}.\nReason: {response.ReasonPhrase}.\nContent: {content}";
            switch(response.StatusCode)
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