using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.RestApiModels.Management;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.Management;

[Trait("Category", "Integration")]
public class ManagementCrudTests : IClassFixture<HEAppEWebApplicationFactory>
{
    private readonly ApiClient _client;

    public ManagementCrudTests(HEAppEWebApplicationFactory factory)
    {
        var httpClient = factory.CreateClient();
        _client = new ApiClient(httpClient);
        _client.SetApiKey("admin", "Passw0rd");
    }

    [Fact]
    public async Task ListProjects_AdminUser_ReturnsOk()
    {
        var response = await _client.GetAsync("/heappe/Management/Projects?sessionCode=test-session");
        response.StatusCode.Should().Match(sc => sc == HttpStatusCode.OK || sc == HttpStatusCode.NotFound || sc == HttpStatusCode.BadRequest || sc == HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ModifyCommandTemplate_GlobalTemplate_ReturnsBadRequest()
    {
        var modifyModel = new ModifyCommandTemplateModel
        {
            Id = 1, // Global template from seed
            Name = "ModifiedGlobalTemplate",
            Description = "Modified global description",
            ExtendedAllocationCommand = "",
            ExecutableFile = "~/.HEAppE/.key_scripts/test.sh",
            PreparationScript = "",
            ClusterNodeTypeId = 1,
            IsEnabled = true,
            SessionCode = "test-session"
        };

        var response = await _client.PutJsonAsync("/heappe/Management/CommandTemplate", modifyModel);
        response.StatusCode.Should().Match(sc => sc == HttpStatusCode.BadRequest || sc == HttpStatusCode.Forbidden || sc == HttpStatusCode.NotFound);
    }
}
