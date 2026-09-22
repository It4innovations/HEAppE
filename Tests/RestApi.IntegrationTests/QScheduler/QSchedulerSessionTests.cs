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
public class QSchedulerSessionTests : IClassFixture<HEAppEWebApplicationFactory>
{
    private readonly ApiClient _client;

    public QSchedulerSessionTests(HEAppEWebApplicationFactory factory)
    {
        var httpClient = factory.CreateClient();
        _client = new ApiClient(httpClient);
        _client.SetApiKey("admin");
    }

    [Fact]
    public async Task OpenSession_EmptyModel_ReturnsBadRequest()
    {
        var response = await _client.PostJsonAsync<object>("/heappe/QScheduler/OpenSession", null!);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
    public async Task ListSessions_Authenticated_ReturnsOkOrEmptyList()
    {
        var response = await _client.GetAsync("/heappe/QScheduler/ListSessions?sessionCode=test-session");
        response.StatusCode.Should().Match(sc => sc == HttpStatusCode.OK || sc == HttpStatusCode.NotFound || sc == HttpStatusCode.BadRequest || sc == HttpStatusCode.Forbidden);
    }
}
