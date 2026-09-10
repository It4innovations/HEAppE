using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.Management.Models;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;

namespace HEAppE.RestApi.IntegrationTests.Management;

[Trait("Category", "Integration")]
public class ManagementCrudTests : ManagementTestBase
{
    public ManagementCrudTests(HEAppEWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ListProjects_AdminUser_ReturnsOk()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.GetAsync($"/heappe/Management/Projects?sessionCode={sessionCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var projects = await _client.GetJsonAsync<List<ProjectExt>>($"/heappe/Management/Projects?sessionCode={sessionCode}");
        projects.Should().NotBeNull();
        projects.Should().NotBeEmpty();
    }
}
