using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.QScheduler;

[Trait("Category", "Integration")]
public class QSchedulerJobTests : IClassFixture<HEAppEWebApplicationFactory>
{
    private readonly HttpClient _httpClient;
    private readonly ApiClient _client;

    public QSchedulerJobTests(HEAppEWebApplicationFactory factory)
    {
        _httpClient = factory.CreateClient();
        _client = new ApiClient(_httpClient);
        _client.SetApiKey("admin", "Passw0rd");
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
}
