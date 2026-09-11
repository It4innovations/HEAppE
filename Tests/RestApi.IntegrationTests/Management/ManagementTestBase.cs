using System.Threading.Tasks;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using HEAppE.RestApiModels.UserAndLimitationManagement;
using Xunit;

namespace HEAppE.RestApi.IntegrationTests.Management;

public abstract class ManagementTestBase : IClassFixture<HEAppEWebApplicationFactory>
{
    protected readonly ApiClient _client;
    protected readonly HEAppEWebApplicationFactory _factory;

    protected ManagementTestBase(HEAppEWebApplicationFactory factory)
    {
        _factory = factory;
        var httpClient = factory.CreateClient();
        _client = new ApiClient(httpClient);
        _client.SetApiKey("admin");
    }

    protected async Task<string> GetAdminSessionCodeAsync()
    {
        var authModel = new AuthenticateUserPasswordModel
        {
            Credentials = new PasswordCredentialsExt
            {
                Username = "admin",
                Password = TestCredentials.DefaultPassword
            }
        };

        var response = await _client.PostJsonAsync("/heappe/UserAndLimitationManagement/AuthenticateUserPassword", authModel);
        response.EnsureSuccessStatusCode();
        var sessionCode = (await response.Content.ReadAsStringAsync()).Trim('"');
        return sessionCode;
    }
}
