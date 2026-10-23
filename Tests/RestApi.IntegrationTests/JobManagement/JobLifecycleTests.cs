using System.Net;
using System.Threading.Tasks;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.JobManagement;

[Trait("Category", "Integration")]
public class JobLifecycleTests : IntegrationTestBase
{
    public JobLifecycleTests(HEAppEWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateJob_EmptyModel_ReturnsBadRequest()
    {
        var response = await _client.PostJsonAsync<object>("/heappe/JobManagement/CreateJob", null!);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitJob_InvalidJobId_ReturnsBadRequestOrNotFound()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.PostJsonAsync<object>("/heappe/JobManagement/SubmitJob", new
        {
            CreatedJobInfoId = 999999L,
            SessionCode = sessionCode
        });
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DryRunJob_NonExistentProject_ReturnsNotFoundOrBadRequest()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.PostJsonAsync<object>("/heappe/JobManagement/DryRunJob", new
        {
            ProjectId = 999999L,
            ClusterNodeTypeId = 1L,
            Nodes = 1,
            TasksPerNode = 1,
            WallTimeInMinutes = 10,
            SessionCode = sessionCode
        });
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }
}
