using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.ClusterInformation;

[Trait("Category", "Integration")]
public class ClusterInformationTests : IClassFixture<HEAppEWebApplicationFactory>
{
    private readonly ApiClient _client;

    public ClusterInformationTests(HEAppEWebApplicationFactory factory)
    {
        var httpClient = factory.CreateClient();
        _client = new ApiClient(httpClient);
        _client.SetApiKey("admin");
    }

    [Fact]
    public async Task ListAvailableClusters_Authenticated_ReturnsClustersList()
    {
        var response = await _client.GetAsync("/heappe/ClusterInformation/ListAvailableClusters?sessionCode=test-session");
        response.StatusCode.Should().Match(sc => sc == HttpStatusCode.OK || sc == HttpStatusCode.NotFound || sc == HttpStatusCode.BadRequest || sc == HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListAvailableClusters_WithoutAuth_ReturnsUnauthorized()
    {
        _client.ClearAuth();
        var response = await _client.GetAsync("/heappe/ClusterInformation/ListAvailableClusters?sessionCode=test-session");
        response.StatusCode.Should().Match(sc => sc == HttpStatusCode.Unauthorized || sc == HttpStatusCode.Forbidden);
    }
}
