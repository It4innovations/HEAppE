using System.Net;
using System.Threading.Tasks;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.QScheduler;

[Trait("Category", "Integration")]
public class QSchedulerResultTests : IClassFixture<HEAppEWebApplicationFactory>
{
    private readonly ApiClient _client;

    public QSchedulerResultTests(HEAppEWebApplicationFactory factory)
    {
        var httpClient = factory.CreateClient();
        _client = new ApiClient(httpClient);
        _client.SetApiKey("admin", "Passw0rd");
    }

    [Fact]
    public async Task GetTaskArtifact_MissingArtifactName_ReturnsBadRequestOrValidationException()
    {
        var response = await _client.GetAsync("/heappe/QScheduler/GetTaskArtifact?submittedTaskId=1&artifactName=");
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MachineArchitecture_MissingNodeType_ReturnsBadRequestOrValidationException()
    {
        var response = await _client.GetAsync("/heappe/QScheduler/MachineArchitecture");
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }
}
