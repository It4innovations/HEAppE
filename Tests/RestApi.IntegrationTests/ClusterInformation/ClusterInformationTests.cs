using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.ClusterInformation;

[Trait("Category", "Integration")]
public class ClusterInformationTests : IntegrationTestBase
{
    public ClusterInformationTests(HEAppEWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ListAvailableClusters_WithValidSessionCode_ReturnsClustersWithNavigationProperties()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.GetAsync($"/heappe/ClusterInformation/ListAvailableClusters?sessionCode={sessionCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var clusters = await _client.GetJsonAsync<List<ClusterExt>>($"/heappe/ClusterInformation/ListAvailableClusters?sessionCode={sessionCode}");
        clusters.Should().NotBeNullOrEmpty();

        foreach (var cluster in clusters)
        {
            cluster.ShouldHaveLoadedNodeTypes();
            cluster.ShouldHaveLoadedFileTransferMethods();
        }
    }

    [Fact]
    public async Task ListClusterNodeTypes_WithValidSessionCode_ReturnsNodeTypes()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.GetAsync($"/heappe/Management/ClusterNodeTypes?sessionCode={sessionCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var nodeTypes = await _client.GetJsonAsync<List<ClusterNodeTypeExt>>($"/heappe/Management/ClusterNodeTypes?sessionCode={sessionCode}");
        nodeTypes.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ListAvailableClusters_WithoutAuth_ReturnsUnauthorized()
    {
        _client.ClearAuth();
        var response = await _client.GetAsync("/heappe/ClusterInformation/ListAvailableClusters?sessionCode=invalid-session");
        response.StatusCode.Should().Match(sc => sc == HttpStatusCode.Unauthorized || sc == HttpStatusCode.Forbidden || sc == HttpStatusCode.BadRequest);
    }
}
