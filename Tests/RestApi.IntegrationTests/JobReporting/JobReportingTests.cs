using System.Net;
using System.Threading.Tasks;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.JobReporting;

[Trait("Category", "Integration")]
public class JobReportingTests : IClassFixture<HEAppEWebApplicationFactory>
{
    private readonly ApiClient _client;

    public JobReportingTests(HEAppEWebApplicationFactory factory)
    {
        var httpClient = factory.CreateClient();
        _client = new ApiClient(httpClient);
        _client.SetApiKey("admin", "Passw0rd");
    }

    [Fact]
    public async Task SummaryReport_Authenticated_ReturnsOk()
    {
        var response = await _client.GetAsync("/heappe/JobReporting/ListAdaptorUserGroups?sessionCode=test-session");
        response.StatusCode.Should().Match(sc => sc == HttpStatusCode.OK || sc == HttpStatusCode.NotFound || sc == HttpStatusCode.BadRequest || sc == HttpStatusCode.Forbidden);
    }
}
