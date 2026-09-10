using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.ExtModels.Management.Models;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using HEAppE.RestApiModels.Management;
using Xunit;

namespace HEAppE.RestApi.IntegrationTests.Management;

[Trait("Category", "Integration")]
public class ProjectManagementCrudTests : ManagementTestBase
{
    public ProjectManagementCrudTests(HEAppEWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Project_And_SubProject_FullCrudLifecycle_Succeeds()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var accountingString = $"acc-{uniqueSuffix}";
        var projectName = $"Proj-{uniqueSuffix}";

        // 1. CREATE Project
        var createModel = new CreateProjectModel
        {
            Name = projectName,
            Description = "Integration test project description",
            AccountingString = accountingString,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            UsageType = UsageTypeExt.NodeHours,
            UseAccountingStringForScheduler = true,
            PIEmail = "pi@test.ci",
            IsOneToOneMapping = false,
            SessionCode = sessionCode
        };

        var createdProject = await _client.PostJsonAsync<CreateProjectModel, ProjectExt>(
            "/heappe/Management/Project", createModel);

        createdProject.Should().NotBeNull();
        createdProject.Id.Should().NotBeNull();
        createdProject.Id.Value.Should().BeGreaterThan(0);
        createdProject.Name.Should().Be(projectName);
        createdProject.AccountingString.Should().Be(accountingString);

        var projectId = createdProject.Id.Value;

        try
        {
            // 2. READ Project by Id
            var getResponse = await _client.GetAsync($"/heappe/Management/Project?id={projectId}&sessionCode={sessionCode}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var fetchedProject = await _client.GetJsonAsync<ProjectExt>($"/heappe/Management/Project?id={projectId}&sessionCode={sessionCode}");
            fetchedProject.Should().NotBeNull();
            fetchedProject.Id.Should().Be(projectId);
            fetchedProject.Name.Should().Be(projectName);

            // 3. LIST Projects
            var allProjects = await _client.GetJsonAsync<List<ProjectExt>>($"/heappe/Management/Projects?sessionCode={sessionCode}");
            allProjects.Should().NotBeNull();
            allProjects.Should().Contain(p => p.Id == projectId);

            // 4. GET ProjectsByAccountingStrings
            var byAccStrings = await _client.GetJsonAsync<List<ProjectExt>>(
                $"/heappe/Management/ProjectsByAccountingStrings?accountingString={accountingString}&sessionCode={sessionCode}");
            byAccStrings.Should().NotBeNull();
            byAccStrings.Should().Contain(p => p.Id == projectId);

            // 5. UPDATE Project
            var updatedName = $"Updated-{projectName}";
            var modifyModel = new ModifyProjectModel
            {
                Id = projectId,
                Name = updatedName,
                Description = "Updated project description",
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddYears(2),
                UsageType = UsageTypeExt.NodeHours,
                UseAccountingStringForScheduler = true,
                IsOneToOneMapping = false,
                SessionCode = sessionCode
            };

            var modifiedProject = await _client.PutJsonAsync<ModifyProjectModel, ProjectExt>(
                "/heappe/Management/Project", modifyModel);
            modifiedProject.Should().NotBeNull();
            modifiedProject.Name.Should().Be(updatedName);

            // 6. SUBPROJECT CRUD
            var subIdentifier = $"sub-{uniqueSuffix}";
            var createSubModel = new CreateSubProjectModel
            {
                Identifier = subIdentifier,
                Description = "Test subproject",
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddMonths(6),
                ProjectId = projectId,
                SessionCode = sessionCode
            };

            var createdSub = await _client.PostJsonAsync<CreateSubProjectModel, SubProjectExt>(
                "/heappe/Management/SubProject", createSubModel);
            createdSub.Should().NotBeNull();
            createdSub.Id.Should().BeGreaterThan(0);
            createdSub.Identifier.Should().Be(subIdentifier);

            var subId = createdSub.Id;

            // Read SubProject
            var fetchedSub = await _client.GetJsonAsync<SubProjectExt>(
                $"/heappe/Management/SubProject?subProjectId={subId}&sessionCode={sessionCode}");
            fetchedSub.Should().NotBeNull();
            fetchedSub.Id.Should().Be(subId);

            // List SubProjects in Project
            var projectSubs = await _client.GetJsonAsync<List<SubProjectExt>>(
                $"/heappe/Management/SubProjects?projectId={projectId}&sessionCode={sessionCode}");
            projectSubs.Should().NotBeNull();
            projectSubs.Should().Contain(s => s.Id == subId);

            // Modify SubProject
            var updatedSubIdentifier = $"updated-{subIdentifier}";
            var modifySubModel = new ModifySubProjectModel
            {
                Id = subId,
                Identifier = updatedSubIdentifier,
                Description = "Updated subproject",
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddMonths(12),
                SessionCode = sessionCode
            };

            var modifiedSub = await _client.PutJsonAsync<ModifySubProjectModel, SubProjectExt>(
                "/heappe/Management/SubProject", modifySubModel);
            modifiedSub.Should().NotBeNull();
            modifiedSub.Identifier.Should().Be(updatedSubIdentifier);

            // Delete SubProject
            var removeSubModel = new RemoveSubProjectModel
            {
                Id = subId,
                SessionCode = sessionCode
            };
            var deleteSubResp = await _client.DeleteJsonAsync("/heappe/Management/SubProject", removeSubModel);
            deleteSubResp.StatusCode.Should().Be(HttpStatusCode.OK);

            // 7. COMPUTE ACCOUNTING
            var accountingModel = new ComputeAccountingModel
            {
                ProjectId = projectId,
                StartTime = DateTime.UtcNow.AddDays(-1),
                EndTime = DateTime.UtcNow,
                SessionCode = sessionCode
            };
            var computeResp = await _client.PostJsonAsync("/heappe/Management/ComputeAccounting", accountingModel);
            computeResp.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            // 8. DELETE Project
            var removeProjectModel = new RemoveProjectModel
            {
                Id = projectId,
                SessionCode = sessionCode
            };
            var deleteResp = await _client.DeleteJsonAsync("/heappe/Management/Project", removeProjectModel);
            deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

            // 9. VERIFY DELETED
            var getDeletedResp = await _client.GetAsync($"/heappe/Management/Project?id={projectId}&sessionCode={sessionCode}");
            getDeletedResp.StatusCode.Should().Match(sc => sc == HttpStatusCode.NotFound || sc == HttpStatusCode.BadRequest);
        }
    }
}
