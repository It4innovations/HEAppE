using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using HEAppE.ExtModels.Management.Models;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;

namespace HEAppE.RestApi.IntegrationTests.Credentials;

[Trait("Category", "Integration")]
public class CredentialsTests : IntegrationTestBase
{
    public CredentialsTests(HEAppEWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetCredentials_AdminSession_ReturnsCredentialsList()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.GetAsync($"/heappe/Credentials/GetCredentials?projectId=1&sessionCode={sessionCode}");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var credentials = await _client.GetJsonAsync<List<CredentialResponseExt>>(
            $"/heappe/Credentials/GetCredentials?projectId=1&sessionCode={sessionCode}");
        credentials.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCredentials_InvalidProject_ReturnsNotFoundOrEmpty()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.GetAsync($"/heappe/Credentials/GetCredentials?projectId=999999&sessionCode={sessionCode}");
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }
}
