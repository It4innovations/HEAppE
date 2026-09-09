using System.Net;
using System.Threading.Tasks;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.Authentication;

[Trait("Category", "Integration")]
public class AuthenticationTests : IClassFixture<HEAppEWebApplicationFactory>
{
    private readonly HEAppEWebApplicationFactory _factory;

    public AuthenticationTests(HEAppEWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private ApiClient CreateClient() => new(_factory.CreateClient());

    [Fact]
    public async Task AuthenticateUserPassword_WithValidCredentials_ReturnsSessionCode()
    {
        var client = CreateClient();
        var authModel = new
        {
            Credentials = new
            {
                Username = "admin",
                Password = "Passw0rd"
            }
        };

        var response = await client.PostJsonAsync("/heappe/UserAndLimitationManagement/AuthenticateUserPassword", authModel);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var sessionCode = (await response.Content.ReadAsStringAsync()).Trim('"');
        sessionCode.Should().NotBeNullOrWhiteSpace();

        // Verify that authenticated session can list clusters
        var clustersResponse = await client.GetAsync($"/heappe/ClusterInformation/ListAvailableClusters?sessionCode={sessionCode}");
        clustersResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthenticateUserPassword_WithInvalidPassword_ReturnsError()
    {
        var client = CreateClient();
        var authModel = new
        {
            Credentials = new
            {
                Username = "admin",
                Password = "WrongPassword123!"
            }
        };

        var response = await client.PostJsonAsync("/heappe/UserAndLimitationManagement/AuthenticateUserPassword", authModel);
        response.StatusCode.Should().Match(sc => sc == HttpStatusCode.BadRequest || sc == HttpStatusCode.Unauthorized || sc == HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AuthenticateUserPassword_WithNonExistentUser_ReturnsError()
    {
        var client = CreateClient();
        var authModel = new
        {
            Credentials = new
            {
                Username = "nonexistent_user_12345",
                Password = "Passw0rd"
            }
        };

        var response = await client.PostJsonAsync("/heappe/UserAndLimitationManagement/AuthenticateUserPassword", authModel);
        response.StatusCode.Should().Match(sc => sc == HttpStatusCode.BadRequest || sc == HttpStatusCode.Unauthorized || sc == HttpStatusCode.Forbidden || sc == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Request_WithInvalidSessionCode_ReturnsForbiddenOrUnauthorized()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/heappe/ClusterInformation/ListAvailableClusters?sessionCode=00000000-0000-0000-0000-000000000000");
        response.StatusCode.Should().Match(sc => sc == HttpStatusCode.Forbidden || sc == HttpStatusCode.Unauthorized || sc == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Request_WithMalformedApiKey_ReturnsUnauthorized()
    {
        var client = CreateClient();
        client.SetApiKey("malformed_api_key_without_colon");
        var response = await client.GetAsync("/heappe/ClusterInformation/ListAvailableClusters");

        response.StatusCode.Should().Match(sc => sc == HttpStatusCode.Unauthorized || sc == HttpStatusCode.Forbidden);
    }
}
