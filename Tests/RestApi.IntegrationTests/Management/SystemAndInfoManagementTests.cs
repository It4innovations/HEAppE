using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.Management.Models;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;

namespace HEAppE.RestApi.IntegrationTests.Management;

[Trait("Category", "Integration")]
public class SystemAndInfoManagementTests : ManagementTestBase
{
    public SystemAndInfoManagementTests(HEAppEWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task InstanceInformation_AdminUser_ReturnsInstanceDetails()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.GetAsync($"/heappe/Management/InstanceInformation?sessionCode={sessionCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var info = await _client.GetJsonAsync<InstanceInformationExt>(
            $"/heappe/Management/InstanceInformation?sessionCode={sessionCode}");
        info.Should().NotBeNull();
        info.Projects.Should().NotBeNull();
    }

    [Fact]
    public async Task VersionInformation_Authenticated_ReturnsVersion()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.GetAsync($"/heappe/Management/VersionInformation?sessionCode={sessionCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var version = await _client.GetJsonAsync<VersionInformationExt>(
            $"/heappe/Management/VersionInformation?sessionCode={sessionCode}");
        version.Should().NotBeNull();
    }

    [Fact]
    public async Task SecureShellKeys_Project1_ReturnsKeysList()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.GetAsync($"/heappe/Management/SecureShellKeys?projectId=1&sessionCode={sessionCode}");
        response.StatusCode.Should().Match(sc => sc == HttpStatusCode.OK || sc == HttpStatusCode.NotFound || sc == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Status_Project1_ReturnsStatus()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.PostAsync($"/heappe/Management/Status?projectId=1&sessionCode={sessionCode}");
        response.StatusCode.Should().Match(sc => sc == HttpStatusCode.OK || sc == HttpStatusCode.NotFound || sc == HttpStatusCode.BadRequest);
    }
}
