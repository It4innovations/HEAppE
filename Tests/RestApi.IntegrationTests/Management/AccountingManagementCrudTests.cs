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
public class AccountingManagementCrudTests : ManagementTestBase
{
    public AccountingManagementCrudTests(HEAppEWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Accounting_CrudLifecycle_Succeeds()
    {
        var sessionCode = await GetAdminSessionCodeAsync();

        // 1. Create Accounting
        var createModel = new CreateAccountingModel
        {
            Formula = "1.5 * CoreHours",
            ValidityFrom = DateTime.UtcNow.Date,
            ValidityTo = DateTime.UtcNow.Date.AddYears(1),
            SessionCode = sessionCode
        };

        var created = await _client.PostJsonAsync<CreateAccountingModel, AccountingExt>(
            "/heappe/Management/Accounting", createModel);
        created.Should().NotBeNull();
        created.Id.Should().NotBeNull();
        created.Id!.Value.Should().BeGreaterThan(0);
        created.Formula.Should().Be("1.5 * CoreHours");

        var accountingId = created.Id.Value;

        try
        {
            // 2. Get Accounting by Id
            var fetched = await _client.GetJsonAsync<AccountingExt>(
                $"/heappe/Management/Accounting?id={accountingId}&sessionCode={sessionCode}");
            fetched.Should().NotBeNull();
            fetched.Id.Should().Be(accountingId);
            fetched.Formula.Should().Be("1.5 * CoreHours");

            // 3. List Accountings
            var list = await _client.GetJsonAsync<List<AccountingExt>>(
                $"/heappe/Management/Accountings?sessionCode={sessionCode}");
            list.Should().NotBeNull();
            list.Should().Contain(a => a.Id == accountingId);

            // 4. Modify Accounting
            var modifyModel = new ModifyAccountingModel
            {
                Id = accountingId,
                Formula = "2.0 * CoreHours",
                ValidityFrom = DateTime.UtcNow.Date,
                ValidityTo = DateTime.UtcNow.Date.AddYears(2),
                SessionCode = sessionCode
            };

            var modified = await _client.PutJsonAsync<ModifyAccountingModel, AccountingExt>(
                "/heappe/Management/Accounting", modifyModel);
            modified.Should().NotBeNull();
            modified.Formula.Should().Be("2.0 * CoreHours");

            // Verify modification persisted
            var reFetched = await _client.GetJsonAsync<AccountingExt>(
                $"/heappe/Management/Accounting?id={accountingId}&sessionCode={sessionCode}");
            reFetched.Should().NotBeNull();
            reFetched.Formula.Should().Be("2.0 * CoreHours");
        }
        finally
        {
            // 5. Remove Accounting
            var removeModel = new RemoveAccountingModel
            {
                Id = accountingId,
                SessionCode = sessionCode
            };
            var deleteResp = await _client.DeleteJsonAsync(
                "/heappe/Management/Accounting", removeModel);
            deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task ClusterNodeTypeAggregationAccounting_And_ProjectClusterNodeTypeAggregation_CrudLifecycle_Succeeds()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];

        // Setup prerequisite 1: ClusterNodeTypeAggregation
        var createAggModel = new CreateClusterNodeTypeAggregationModel
        {
            Name = $"AggAcc-{uniqueSuffix}",
            Description = "Aggregation for accounting test",
            AllocationType = "CoreHours",
            SessionCode = sessionCode
        };
        var agg = await _client.PostJsonAsync<CreateClusterNodeTypeAggregationModel, ClusterNodeTypeAggregationExt>(
            "/heappe/Management/ClusterNodeTypeAggregation", createAggModel);
        agg.Should().NotBeNull();
        agg.Id.Should().NotBeNull();
        var aggId = agg.Id!.Value;

        // Setup prerequisite 2: Accounting
        var createAccModel = new CreateAccountingModel
        {
            Formula = "1.0 * CoreHours",
            ValidityFrom = DateTime.UtcNow.Date,
            ValidityTo = DateTime.UtcNow.Date.AddYears(1),
            SessionCode = sessionCode
        };
        var acc = await _client.PostJsonAsync<CreateAccountingModel, AccountingExt>(
            "/heappe/Management/Accounting", createAccModel);
        acc.Should().NotBeNull();
        acc.Id.Should().NotBeNull();
        var accId = acc.Id!.Value;

        // Setup prerequisite 3: Project
        var createProjModel = new CreateProjectModel
        {
            Name = $"AggProj-{uniqueSuffix}",
            AccountingString = $"agg-proj-{uniqueSuffix}",
            Description = "Project for aggregation test",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            UsageType = HEAppE.ExtModels.JobReporting.Models.UsageTypeExt.NodeHours,
            PIEmail = "agg@test.ci",
            SessionCode = sessionCode
        };
        var proj = await _client.PostJsonAsync<CreateProjectModel, ProjectExt>(
            "/heappe/Management/Project", createProjModel);
        proj.Should().NotBeNull();
        proj.Id.Should().NotBeNull();
        var projId = proj.Id!.Value;

        try
        {
            // === ClusterNodeTypeAggregationAccounting CRUD ===
            // Create Aggregation-Accounting link
            var createAggAccModel = new CreateClusterNodeTypeAggregationAccountingModel
            {
                ClusterNodeTypeAggregationId = aggId,
                AccountingId = accId,
                SessionCode = sessionCode
            };
            var createdAggAcc = await _client.PostJsonAsync<CreateClusterNodeTypeAggregationAccountingModel, ClusterNodeTypeAggregationAccountingExt>(
                "/heappe/Management/ClusterNodeTypeAggregationAccounting", createAggAccModel);
            createdAggAcc.Should().NotBeNull();
            createdAggAcc.ClusterNodeTypeAggregationId.Should().Be(aggId);
            createdAggAcc.AccountingId.Should().Be(accId);

            // Get Aggregation-Accounting link by IDs
            var fetchedAggAcc = await _client.GetJsonAsync<ClusterNodeTypeAggregationAccountingExt>(
                $"/heappe/Management/ClusterNodeTypeAggregationAccounting?clusterNodeTypeAggregationId={aggId}&accountingId={accId}&sessionCode={sessionCode}");
            fetchedAggAcc.Should().NotBeNull();
            fetchedAggAcc.ClusterNodeTypeAggregationId.Should().Be(aggId);
            fetchedAggAcc.AccountingId.Should().Be(accId);

            // List Aggregation-Accountings
            var aggAccList = await _client.GetJsonAsync<List<ClusterNodeTypeAggregationAccountingExt>>(
                $"/heappe/Management/ClusterNodeTypeAggregationAccountings?sessionCode={sessionCode}");
            aggAccList.Should().NotBeNull();
            aggAccList.Should().Contain(a => a.ClusterNodeTypeAggregationId == aggId && a.AccountingId == accId);

            // === ProjectClusterNodeTypeAggregation CRUD ===
            // Create Project-Aggregation allocation
            var createProjAggModel = new CreateProjectClusterNodeTypeAggregationModel
            {
                ProjectId = projId,
                ClusterNodeTypeAggregationId = aggId,
                AllocationAmount = 500,
                SessionCode = sessionCode
            };
            var createdProjAgg = await _client.PostJsonAsync<CreateProjectClusterNodeTypeAggregationModel, ProjectClusterNodeTypeAggregationExt>(
                "/heappe/Management/ProjectClusterNodeTypeAggregation", createProjAggModel);
            createdProjAgg.Should().NotBeNull();
            createdProjAgg.ProjectId.Should().Be(projId);
            createdProjAgg.ClusterNodeTypeAggregationId.Should().Be(aggId);
            createdProjAgg.AllocationAmount.Should().Be(500);

            // Get Project-Aggregation by ID
            var fetchedProjAgg = await _client.GetJsonAsync<ProjectClusterNodeTypeAggregationExt>(
                $"/heappe/Management/ProjectClusterNodeTypeAggregation?projectId={projId}&clusterNodeTypeAggregationId={aggId}&sessionCode={sessionCode}");
            fetchedProjAgg.Should().NotBeNull();
            fetchedProjAgg.AllocationAmount.Should().Be(500);

            // List Project-Aggregations by ProjectId
            var projAggListByProj = await _client.GetJsonAsync<List<ProjectClusterNodeTypeAggregationExt>>(
                $"/heappe/Management/ProjectClusterNodeTypeAggregations?projectId={projId}&sessionCode={sessionCode}");
            projAggListByProj.Should().NotBeNull();
            projAggListByProj.Should().Contain(p => p.ProjectId == projId && p.ClusterNodeTypeAggregationId == aggId);

            // List all Project-Aggregations
            var allProjAggList = await _client.GetJsonAsync<List<ProjectClusterNodeTypeAggregationExt>>(
                $"/heappe/Management/ProjectClusterNodeTypeAggregations?sessionCode={sessionCode}");
            allProjAggList.Should().NotBeNull();
            allProjAggList.Should().Contain(p => p.ProjectId == projId && p.ClusterNodeTypeAggregationId == aggId);

            // Modify Project-Aggregation
            var modifyProjAggModel = new ModifyProjectClusterNodeTypeAggregationModel
            {
                ProjectId = projId,
                ClusterNodeTypeAggregationId = aggId,
                AllocationAmount = 1000,
                SessionCode = sessionCode
            };
            var modifiedProjAgg = await _client.PutJsonAsync<ModifyProjectClusterNodeTypeAggregationModel, ProjectClusterNodeTypeAggregationExt>(
                "/heappe/Management/ProjectClusterNodeTypeAggregation", modifyProjAggModel);
            modifiedProjAgg.Should().NotBeNull();
            modifiedProjAgg.AllocationAmount.Should().Be(1000);

            // Delete Project-Aggregation
            var removeProjAggModel = new RemoveProjectClusterNodeTypeAggregationModel
            {
                ProjectId = projId,
                ClusterNodeTypeAggregationId = aggId,
                SessionCode = sessionCode
            };
            var deleteProjAggResp = await _client.DeleteJsonAsync(
                "/heappe/Management/ProjectClusterNodeTypeAggregation", removeProjAggModel);
            deleteProjAggResp.StatusCode.Should().Be(HttpStatusCode.OK);

            // Delete Aggregation-Accounting link
            var removeAggAccModel = new RemoveClusterNodeTypeAggregationAccountingModel
            {
                ClusterNodeTypeAggregationId = aggId,
                AccountingId = accId,
                SessionCode = sessionCode
            };
            var deleteAggAccResp = await _client.DeleteJsonAsync(
                "/heappe/Management/ClusterNodeTypeAggregationAccounting", removeAggAccModel);
            deleteAggAccResp.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            // Clean up prerequisites
            await _client.DeleteJsonAsync("/heappe/Management/Accounting", new RemoveAccountingModel { Id = accId, SessionCode = sessionCode });
            await _client.DeleteJsonAsync("/heappe/Management/ClusterNodeTypeAggregation", new RemoveClusterNodeTypeAggregationModel { Id = aggId, SessionCode = sessionCode });
            await _client.DeleteJsonAsync("/heappe/Management/Project", new RemoveProjectModel { Id = projId, SessionCode = sessionCode });
        }
    }
}
