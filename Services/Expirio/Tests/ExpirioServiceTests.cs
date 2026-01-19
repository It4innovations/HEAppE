using Microsoft.Extensions.Configuration;
using Services.Expirio.Configuration;
using Services.Expirio.Exceptions;
using Services.Expirio.Models;

namespace Services.Expirio.Tests;

public class ExpirioServiceTests
{

    public async Task GetKerberosTicket_should_return_success()
    {
        HttpClient httpClient = new HttpClient()
        {
            BaseAddress = new Uri(ExpirioSettings.BaseUrl),
            Timeout = TimeSpan.FromSeconds(ExpirioSettings.TimeoutSeconds),
        };
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        var expirio = new ExpirioService(httpClient);
        var request = new KerberosExchangeRequest()
        {
            ProviderName = ExpirioSettings.ProviderName,
        };

        var ct = new CancellationToken();
        try
        {
            var ticket = await expirio.ExchangeTokenForKerberosAsync(request, ct);
            //TODO: check ticket
            //Assert.
        }
        catch(Exception)
        {}
    }

    [Fact]
    public async Task GetKerberosTicket_should_return_unauthorized()
    {
        HttpClient httpClient = new HttpClient()
        {
            BaseAddress = new Uri(ExpirioSettings.BaseUrl),
            Timeout = TimeSpan.FromSeconds(ExpirioSettings.TimeoutSeconds),
        };
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        var expirio = new ExpirioService(httpClient);
        var request = new KerberosExchangeRequest()
        {
            ProviderName = ExpirioSettings.ProviderName,
        };

        var ct = new CancellationToken();
        try
        {
            var ticket = await expirio.ExchangeTokenForKerberosAsync(request, ct);
        }
        catch(ExpirioUnauthorizedException)
        {
            Assert.True(true);
        }
    }

    //TODO: bad request (400 with error body)?

    //TODO: server error (500) — ensure retry policy invoked?
}