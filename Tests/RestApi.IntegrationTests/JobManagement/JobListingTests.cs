using System.Net;
using System.Threading.Tasks;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.JobManagement;

[Trait("Category", "Integration")]
public class JobListingTests : IntegrationTestBase
{
    public JobListingTests(HEAppEWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ListJobsForCurrentUser_WithValidSession_ReturnsJobsWithTasks()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.GetAsync($"/heappe/JobManagement/ListJobsForCurrentUser?sessionCode={sessionCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var jobs = await _client.GetJsonAsync<System.Collections.Generic.List<HEAppE.ExtModels.JobManagement.Models.SubmittedJobInfoExt>>(
            $"/heappe/JobManagement/ListJobsForCurrentUser?sessionCode={sessionCode}");
        jobs.Should().NotBeNull();

        foreach (var job in jobs)
        {
            job.Tasks.Should().NotBeNull("because SubmittedJobInfoExt.Tasks must be loaded via .Include()");
        }
    }
}
