using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.QScheduler;

[Trait("Category", "Integration")]
public class QSchedulerSessionTests : IntegrationTestBase
{
    public QSchedulerSessionTests(HEAppEWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task OpenSession_EmptyModel_ReturnsBadRequest()
    {
        var response = await _client.PostJsonAsync<object>("/heappe/QScheduler/OpenSession", null!);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OpenSession_ValidModel_LoadsNavigationPropertiesSuccessfully()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var model = new HEAppE.RestApiModels.JobManagement.OpenQSchedulerSessionModel
        {
            SessionCode = sessionCode,
            ClusterId = 3, // QSchedulerTestCluster
            ProjectId = 2, // QuantumProject (linked to Cluster 3 in seed.ci.njson)
            MachineId = "TestMachine",
            WalltimeLimit = 3600
        };

        var response = await _client.PostJsonAsync("/heappe/QScheduler/OpenSession", model);

        // Verification: If the bug was present, EF Core threw ArgumentException 400: "Project with ID '2' is not referenced to the cluster with ID '3'"
        // With navigation properties properly loaded, it either succeeds (200 OK) or attempts connection to QScheduler service
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotContain("is not referenced to the cluster",
            "because ClusterProjects navigation property must be loaded by GetByIdWithAggregationsAsync");
        response.StatusCode.Should().Match(sc => sc == HttpStatusCode.OK || sc == HttpStatusCode.InternalServerError || sc == HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task CloseSession_EmptyModel_ReturnsBadRequest()
    {
        var response = await _client.DeleteAsync("/heappe/QScheduler/CloseSession");
        response.StatusCode.Should().Match(sc => sc == HttpStatusCode.BadRequest || sc == HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task GetSessionInfo_InvalidSessionId_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/heappe/QScheduler/GetSessionInfo?sessionId=0");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSessionInfo_NonExistingSession_ReturnsNotFound()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.GetAsync($"/heappe/QScheduler/GetSessionInfo?sessionId=999999&sessionCode={sessionCode}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListSessions_Authenticated_ReturnsOkList()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.GetAsync($"/heappe/QScheduler/ListSessions?sessionCode={sessionCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var sessions = await _client.GetJsonAsync<List<QSchedulerSessionInfoExt>>($"/heappe/QScheduler/ListSessions?sessionCode={sessionCode}");
        sessions.Should().NotBeNull();
    }
}
