using System.Net;
using System.Threading.Tasks;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.QScheduler;

[Trait("Category", "Integration")]
public class QSchedulerResultTests : IntegrationTestBase
{
    public QSchedulerResultTests(HEAppEWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetTaskArtifact_NonExistentTask_ReturnsNotFoundOrBadRequest()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.GetAsync($"/heappe/QScheduler/GetTaskArtifact?submittedTaskId=999999&artifactName=result.json&sessionCode={sessionCode}");
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTaskResult_NonExistentTask_ReturnsNotFoundOrBadRequest()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.GetAsync($"/heappe/QScheduler/GetTaskResult?submittedTaskId=999999&sessionCode={sessionCode}");
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MachineArchitecture_MissingNodeType_ReturnsBadRequestOrValidationException()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.GetAsync($"/heappe/QScheduler/MachineArchitecture?sessionCode={sessionCode}");
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }
}
