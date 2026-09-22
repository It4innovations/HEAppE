using System.Net;
using System.Threading.Tasks;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.JobManagement;

[Trait("Category", "Integration")]
public class JobLifecycleTests : IClassFixture<HEAppEWebApplicationFactory>
{
    private readonly ApiClient _client;

    public JobLifecycleTests(HEAppEWebApplicationFactory factory)
    {
        var httpClient = factory.CreateClient();
        _client = new ApiClient(httpClient);
        _client.SetApiKey("admin");
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
        var response = await _client.PostJsonAsync<object>("/heappe/JobManagement/SubmitJob?submittedJobInfoId=-1", null!);
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }
}
