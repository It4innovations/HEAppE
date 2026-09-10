using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.Management.Models;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using HEAppE.RestApiModels.Management;
using Xunit;

namespace HEAppE.RestApi.IntegrationTests.Management;

[Trait("Category", "Integration")]
public class CommandTemplateManagementCrudTests : ManagementTestBase
{
    public CommandTemplateManagementCrudTests(HEAppEWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CommandTemplate_And_Parameters_FullCrudLifecycle_Succeeds()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var templateName = $"Tmpl-{uniqueSuffix}";

        // Project 1 and ClusterNodeType 1 exist in seed.ci.njson
        const long projectId = 1;
        const long clusterNodeTypeId = 1;

        // 1. CREATE CommandTemplate
        var createModel = new CreateCommandTemplateModel
        {
            Name = templateName,
            Description = "Integration test template",
            ExtendedAllocationCommand = "",
            ExecutableFile = "/bin/bash",
            PreparationScript = "echo 'prep'",
            ClusterNodeTypeId = clusterNodeTypeId,
            ProjectId = projectId,
            TemplateParameters = new(),
            SessionCode = sessionCode
        };

        var createdTemplate = await _client.PostJsonAsync<CreateCommandTemplateModel, ExtendedCommandTemplateExt>(
            "/heappe/Management/CommandTemplate", createModel);
        createdTemplate.Should().NotBeNull();
        createdTemplate.Id.Should().NotBeNull();
        createdTemplate.Id.Value.Should().BeGreaterThan(0);
        createdTemplate.Name.Should().Be(templateName);

        var templateId = createdTemplate.Id.Value;

        try
        {
            // 2. READ CommandTemplate by Id
            var fetchedTemplate = await _client.GetJsonAsync<CommandTemplateExt>(
                $"/heappe/Management/CommandTemplate?id={templateId}&sessionCode={sessionCode}");
            fetchedTemplate.Should().NotBeNull();
            fetchedTemplate.Id.Should().Be(templateId);
            fetchedTemplate.Name.Should().Be(templateName);

            // 3. LIST CommandTemplates for Project
            var projectTemplates = await _client.GetJsonAsync<List<CommandTemplateExt>>(
                $"/heappe/Management/CommandTemplates?projectId={projectId}&sessionCode={sessionCode}");
            projectTemplates.Should().NotBeNull();
            projectTemplates.Should().Contain(t => t.Id == templateId);

            // 4. UPDATE CommandTemplate
            var updatedName = $"Updated-{templateName}";
            var modifyModel = new ModifyCommandTemplateModel
            {
                Id = templateId,
                Name = updatedName,
                Description = "Updated template description",
                ExtendedAllocationCommand = "",
                ExecutableFile = "/bin/sh",
                PreparationScript = "echo 'updated prep'",
                ClusterNodeTypeId = clusterNodeTypeId,
                IsEnabled = true,
                SessionCode = sessionCode
            };

            var modifyResp = await _client.PutJsonAsync("/heappe/Management/CommandTemplate", modifyModel);
            modifyResp.StatusCode.Should().Be(HttpStatusCode.OK);

            // 5. PARAMETER CRUD
            var paramIdentifier = $"p_{uniqueSuffix}";
            var createParamModel = new CreateCommandTemplateParameterModel
            {
                Identifier = paramIdentifier,
                Query = "param_val",
                Description = "Test parameter",
                CommandTemplateId = templateId,
                SessionCode = sessionCode
            };

            var createdParam = await _client.PostJsonAsync<CreateCommandTemplateParameterModel, ExtendedCommandTemplateParameterExt>(
                "/heappe/Management/CommandTemplateParameter", createParamModel);
            createdParam.Should().NotBeNull();
            createdParam.Id.Should().BeGreaterThan(0);
            createdParam.Identifier.Should().Be(paramIdentifier);

            var paramId = createdParam.Id;

            // Read Parameter
            var fetchedParam = await _client.GetJsonAsync<ExtendedCommandTemplateParameterExt>(
                $"/heappe/Management/CommandTemplateParameter?id={paramId}&sessionCode={sessionCode}");
            fetchedParam.Should().NotBeNull();
            fetchedParam.Id.Should().Be(paramId);

            // Modify Parameter
            var updatedParamQuery = "updated_query";
            var modifyParamModel = new ModifyCommandTemplateParameterModel
            {
                Id = paramId,
                Identifier = paramIdentifier,
                Query = updatedParamQuery,
                Description = "Updated description",
                SessionCode = sessionCode
            };
            var modifyParamResp = await _client.PutJsonAsync(
                "/heappe/Management/CommandTemplateParameter", modifyParamModel);
            modifyParamResp.StatusCode.Should().Be(HttpStatusCode.OK);

            // Delete Parameter
            var removeParamModel = new RemoveCommandTemplateParameterModel
            {
                Id = paramId,
                SessionCode = sessionCode
            };
            var deleteParamResp = await _client.DeleteJsonAsync(
                "/heappe/Management/CommandTemplateParameter", removeParamModel);
            deleteParamResp.StatusCode.Should().Be(HttpStatusCode.OK);

            // 6. GENERIC COMMAND TEMPLATE CRUD
            var genericName = $"Gen-{uniqueSuffix}";
            var createGenericModel = new CreateGenericCommandTemplateModel
            {
                Name = genericName,
                Description = "Test generic template",
                ExtendedAllocationCommand = "",
                PreparationScript = "echo 'gen prep'",
                ClusterNodeTypeId = clusterNodeTypeId,
                ProjectId = projectId,
                SessionCode = sessionCode
            };

            var createdGeneric = await _client.PostJsonAsync<CreateGenericCommandTemplateModel, CommandTemplateExt>(
                "/heappe/Management/GenericCommandTemplate", createGenericModel);
            createdGeneric.Should().NotBeNull();
            createdGeneric.Id.Should().NotBeNull();
            createdGeneric.Id.Value.Should().BeGreaterThan(0);

            // Modify Generic Template
            var modifyGenericModel = new ModifyGenericCommandTemplateModel
            {
                Id = createdGeneric.Id.Value,
                Name = $"Updated-{genericName}",
                Description = "Updated generic description",
                ExtendedAllocationCommand = "",
                PreparationScript = "",
                ClusterNodeTypeId = clusterNodeTypeId,
                IsEnabled = true,
                SessionCode = sessionCode
            };
            var modifyGenResp = await _client.PutJsonAsync(
                "/heappe/Management/GenericCommandTemplate", modifyGenericModel);
            modifyGenResp.StatusCode.Should().Be(HttpStatusCode.OK);

            // Clean up generic template
            await _client.DeleteJsonAsync(
                "/heappe/Management/RemoveCommandTemplate",
                new RemoveCommandTemplateModel { CommandTemplateId = createdGeneric.Id.Value, SessionCode = sessionCode });
        }
        finally
        {
            // 7. DELETE CommandTemplate
            var removeModel = new RemoveCommandTemplateModel
            {
                CommandTemplateId = templateId,
                SessionCode = sessionCode
            };
            var deleteResp = await _client.DeleteJsonAsync(
                "/heappe/Management/RemoveCommandTemplate", removeModel);
            deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task ModifyCommandTemplate_GlobalTemplate_ReturnsBadRequest()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
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
            SessionCode = sessionCode
        };

        var response = await _client.PutJsonAsync("/heappe/Management/CommandTemplate", modifyModel);
        response.StatusCode.Should().Match(sc => sc == HttpStatusCode.BadRequest || sc == HttpStatusCode.Forbidden || sc == HttpStatusCode.NotFound);
    }
}
