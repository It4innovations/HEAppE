using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;

namespace HEAppE.RestApi.IntegrationTests.QScheduler;

[Trait("Category", "Integration")]
public class QSchedulerMachineInfoTests : IClassFixture<HEAppEWebApplicationFactory>
{
    private readonly ApiClient _client;

    public QSchedulerMachineInfoTests(HEAppEWebApplicationFactory factory)
    {
        var httpClient = factory.CreateClient();
        _client = new ApiClient(httpClient);
    }

    [Fact]
    public async Task MachineInfo_MissingSessionCode_ReturnsForbiddenOrBadRequest()
    {
        _client.ClearAuth();
        var response = await _client.GetAsync("/heappe/QScheduler/MachineInfo?clusterNodeTypeId=1&projectId=1");
        response.StatusCode.Should().Match(sc => sc == HttpStatusCode.Forbidden || sc == HttpStatusCode.BadRequest || sc == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MachineInfo_InvalidNodeType_ReturnsBadRequest()
    {
        _client.ClearAuth();
        var response = await _client.GetAsync("/heappe/QScheduler/MachineInfo?clusterNodeTypeId=0&projectId=1&sessionCode=dummy-test-session-code-12345");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MachineInfo_ClusterInformationRoute_MissingSessionCode_ReturnsForbiddenOrBadRequest()
    {
        _client.ClearAuth();
        var response = await _client.GetAsync("/heappe/ClusterInformation/MachineInfo?clusterNodeTypeId=1&projectId=1");
        response.StatusCode.Should().Match(sc => sc == HttpStatusCode.Forbidden || sc == HttpStatusCode.BadRequest || sc == HttpStatusCode.Unauthorized);
    }
}
