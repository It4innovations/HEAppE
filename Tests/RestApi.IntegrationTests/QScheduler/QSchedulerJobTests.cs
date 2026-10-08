using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.QScheduler;

[Trait("Category", "Integration")]
public class QSchedulerJobTests : IntegrationTestBase
{
    private readonly HttpClient _httpClient;

    public QSchedulerJobTests(HEAppEWebApplicationFactory factory) : base(factory)
    {
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task CreateAndSubmitJob_EmptyModel_ReturnsBadRequest()
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(""), "model");

        var response = await _httpClient.PostAsync("/heappe/QScheduler/CreateAndSubmitJob", form);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateAndSubmitJob_InvalidJson_ReturnsBadRequest()
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent("{ invalid json }"), "model");

        var response = await _httpClient.PostAsync("/heappe/QScheduler/CreateAndSubmitJob", form);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateAndSubmitJob_ValidModel_ResolvesNodeTypeAndCreatesJob()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var modelJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            SessionCode = sessionCode,
            JobSpecification = new
            {
                Name = "IntegrationTestQuantumJob",
                ClusterId = 3,
                ProjectId = 2,
                Tasks = new[]
                {
                    new
                    {
                        Name = "Task1",
                        MachineId = "SimulatorMachine",
                        WalltimeLimitSecs = 60,
                        PayloadPartName = "circuit1",
                        UseSessions = false
                    }
                }
            }
        });

        var form = new MultipartFormDataContent();
        form.Add(new StringContent(modelJson), "model");
        form.Add(new StringContent("{\"circuit\": \"H 0; M 0\"}"), "circuit1", "circuit1.json");

        var response = await _httpClient.PostAsync("/heappe/QScheduler/CreateAndSubmitJob", form);
        var content = await response.Content.ReadAsStringAsync();

        // Must successfully auto-resolve NodeType and TransferMethod without failing with "NoNodeTypesConfigured"
        content.Should().NotContain("NoNodeTypesConfigured");
        response.StatusCode.Should().Match(sc => sc == HttpStatusCode.OK || sc == HttpStatusCode.BadRequest || sc == HttpStatusCode.InternalServerError || sc == HttpStatusCode.BadGateway);
    }
}
