using System.Net;
using System.Threading.Tasks;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.JobReporting;

[Trait("Category", "Integration")]
public class JobReportingTests : IntegrationTestBase
{
    public JobReportingTests(HEAppEWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ListAdaptorUserGroups_WithValidSession_ReturnsOk()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.GetAsync($"/heappe/JobReporting/ListAdaptorUserGroups?sessionCode={sessionCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task JobsDetailedReport_WithValidSession_ReturnsOk()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.GetAsync($"/heappe/JobReporting/JobsDetailedReport?sessionCode={sessionCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UserResourceUsageReport_WithValidSession_ReturnsOk()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.GetAsync($"/heappe/JobReporting/UserResourceUsageReport?sessionCode={sessionCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
