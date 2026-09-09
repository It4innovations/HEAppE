using System.Net;
using System.Threading.Tasks;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.JobManagement;

[Trait("Category", "Integration")]
public class JobCancellationTests : IClassFixture<HEAppEWebApplicationFactory>
{
    private readonly ApiClient _client;

    public JobCancellationTests(HEAppEWebApplicationFactory factory)
    {
        var httpClient = factory.CreateClient();
        _client = new ApiClient(httpClient);
        _client.SetApiKey("admin", "Passw0rd");
    }

    [Fact]
    public async Task CancelJob_NonExistentJob_ReturnsNotFoundOrBadRequest()
    {
        var response = await _client.PostJsonAsync<object>("/heappe/JobManagement/CancelJob?submittedJobInfoId=999999", null);
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }
}
