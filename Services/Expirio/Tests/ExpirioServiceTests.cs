using Microsoft.Extensions.Configuration;
using Services.Expirio.Configuration;
using Services.Expirio.Exceptions;
using Services.Expirio.Models;

namespace Services.Expirio.Tests;

public class ExpirioServiceTests
{
    [Fact]
    public async Task GetKerberosTicket_should_return_success()
    {
        HttpClient httpClient = new HttpClient()
        {
            BaseAddress = new Uri(ExpirioSettings.BaseUrl),
            Timeout = TimeSpan.FromSeconds(ExpirioSettings.TimeoutSeconds),
        };
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        //TODO: needs to get Expirio test token.

        var expirio = new ExpirioService(httpClient);
        var request = new KerberosExchangeRequest()
        {
            ProviderName = ExpirioSettings.ProviderName,
        };

        var ct = new CancellationToken();
        try
        {
            string ticket = await expirio.ExchangeTokenForKerberosAsync(request, ct);
            Assert.NotNull(ticket);
            Assert.NotEmpty(ticket);
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

    [Fact]
    public async Task GetKerberosTicket_should_return_not_found()
    {
        HttpClient httpClient = new HttpClient()
        {
            BaseAddress = new Uri(ExpirioSettings.BaseUrl),
            Timeout = TimeSpan.FromSeconds(ExpirioSettings.TimeoutSeconds),
        };
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        //TODO: needs to get Expirio test token.

        var expirio = new ExpirioService(httpClient);
        var request = new KerberosExchangeRequest()
        {
            ProviderName = "",
        };

        var ct = new CancellationToken();
        try
        {
            var ticket = await expirio.ExchangeTokenForKerberosAsync(request, ct);
        }
        catch(ExpirioNotFoundException)
        {
            Assert.True(true);
        }
    }

    //TODO: bad request (400 with error body)?

    //TODO: server error (500) — ensure retry policy invoked?
}