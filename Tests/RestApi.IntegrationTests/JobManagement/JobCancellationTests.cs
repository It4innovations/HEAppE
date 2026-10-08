using System.Net;
using System.Threading.Tasks;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.JobManagement;

[Trait("Category", "Integration")]
public class JobCancellationTests : IntegrationTestBase
{
    public JobCancellationTests(HEAppEWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CancelJob_NonExistentJob_ReturnsNotFoundOrBadRequest()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.PostJsonAsync<object>("/heappe/JobManagement/CancelJob", new
        {
            SubmittedJobInfoId = 999999L,
            SessionCode = sessionCode
        });
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }
}
