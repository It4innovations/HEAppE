using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using HEAppE.RestApiModels.Dictionaries;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.Dictionary;

[Trait("Category", "Integration")]
public class DictionaryTests : IClassFixture<HEAppEWebApplicationFactory>
{
    private readonly ApiClient _client;

    public DictionaryTests(HEAppEWebApplicationFactory factory)
    {
        var httpClient = factory.CreateClient();
        _client = new ApiClient(httpClient);
    }

    [Fact]
    public async Task GetClusterAuthenticationCredentialsAuthTypes_AnonymousAccess_ReturnsOk()
    {
        _client.ClearAuth();
        var response = await _client.GetAsync("/heappe/Dictionary/GetClusterAuthenticationCredentialsAuthTypes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await _client.GetJsonAsync<List<DictionaryItemModel>>("/heappe/Dictionary/GetClusterAuthenticationCredentialsAuthTypes");
        items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetJobStates_AnonymousAccess_ReturnsJobStates()
    {
        _client.ClearAuth();
        var items = await _client.GetJsonAsync<List<DictionaryItemModel>>("/heappe/Dictionary/GetJobStates");
        items.Should().NotBeEmpty();
        items.Should().Contain(i => i.Name == "Submitted" || i.Name == "Running" || i.Name == "Finished");
    }

    [Fact]
    public async Task GetSchedulerTypes_AnonymousAccess_ReturnsSupportedSchedulers()
    {
        _client.ClearAuth();
        var items = await _client.GetJsonAsync<List<DictionaryItemModel>>("/heappe/Dictionary/GetSchedulerTypes");
        items.Should().NotBeEmpty();
        items.Should().Contain(i => i.Name == "Slurm" || i.Name == "PbsPro" || i.Name == "QScheduler");
    }

    [Fact]
    public async Task Health_Endpoint_ReturnsHealthy()
    {
        _client.ClearAuth();
        var response = await _client.GetAsync("/heappe/Health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
