using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.FaultInjection;

[Trait("Category", "Integration")]
public class FaultInjectionTests : IClassFixture<HEAppEWebApplicationFactory>
{
    private readonly ApiClient _client;
    private readonly HEAppEWebApplicationFactory _factory;

    public FaultInjectionTests(HEAppEWebApplicationFactory factory)
    {
        _factory = factory;
        var httpClient = factory.CreateClient();
        _client = new ApiClient(httpClient);
        _client.SetApiKey("admin", "Passw0rd");
    }

    [Fact]
    public async Task ConcurrentRequests_ShouldHandleLoadGracefully()
    {
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.GetAsync("/heappe/Dictionary/GetJobStates"));
        }

        var responses = await Task.WhenAll(tasks);
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
    }
}
