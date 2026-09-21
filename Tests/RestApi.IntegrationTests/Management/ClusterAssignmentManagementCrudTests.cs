using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.Management.Models;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using HEAppE.RestApiModels.Management;
using Xunit;

namespace HEAppE.RestApi.IntegrationTests.Management;

[Trait("Category", "Integration")]
public class ClusterAssignmentManagementCrudTests : ManagementTestBase
{
    public ClusterAssignmentManagementCrudTests(HEAppEWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ProjectAssignmentToCluster_And_Aggregation_CrudLifecycle_Succeeds()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];

        // 1. CLUSTER NODE TYPE AGGREGATION CRUD
        var aggregationName = $"Agg-{uniqueSuffix}";
        var createAggModel = new CreateClusterNodeTypeAggregationModel
        {
            Name = aggregationName,
            Description = "Test aggregation",
            AllocationType = "CoreHours",
            SessionCode = sessionCode
        };

        var createdAgg = await _client.PostJsonAsync<CreateClusterNodeTypeAggregationModel, ClusterNodeTypeAggregationExt>(
            "/heappe/Management/ClusterNodeTypeAggregation", createAggModel);
        createdAgg.Should().NotBeNull();
        createdAgg.Id.Should().NotBeNull();
        createdAgg.Id.Value.Should().BeGreaterThan(0);
        createdAgg.Name.Should().Be(aggregationName);

        var aggId = createdAgg.Id.Value;

        try
        {
            // Read Aggregation
            var fetchedAgg = await _client.GetJsonAsync<ClusterNodeTypeAggregationExt>(
                $"/heappe/Management/ClusterNodeTypeAggregation?id={aggId}&sessionCode={sessionCode}");
            fetchedAgg.Should().NotBeNull();
            fetchedAgg.Id.Should().Be(aggId);

            // List Aggregations
            var allAggs = await _client.GetJsonAsync<List<ClusterNodeTypeAggregationExt>>(
                $"/heappe/Management/ClusterNodeTypeAggregations?sessionCode={sessionCode}");
            allAggs.Should().NotBeNull();
            allAggs.Should().Contain(a => a.Id == aggId);

            // Modify Aggregation
            var updatedAggName = $"Updated-{aggregationName}";
            var modifyAggModel = new ModifyClusterNodeTypeAggregationModel
            {
                Id = aggId,
                Name = updatedAggName,
                Description = "Updated aggregation description",
                AllocationType = "CoreHours",
                SessionCode = sessionCode
            };
            var modifiedAgg = await _client.PutJsonAsync<ModifyClusterNodeTypeAggregationModel, ClusterNodeTypeAggregationExt>(
                "/heappe/Management/ClusterNodeTypeAggregation", modifyAggModel);
            modifiedAgg.Should().NotBeNull();
            modifiedAgg.Name.Should().Be(updatedAggName);
        }
        finally
        {
            // Delete Aggregation
            var removeAggModel = new RemoveClusterNodeTypeAggregationModel
            {
                Id = aggId,
                SessionCode = sessionCode
            };
            var deleteAggResp = await _client.DeleteJsonAsync(
                "/heappe/Management/ClusterNodeTypeAggregation", removeAggModel);
            deleteAggResp.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // 2. PROJECT ASSIGNMENT TO CLUSTER CRUD
        // We will create a dedicated test project and cluster to cleanly test assignment and deletion
        var projectModel = new CreateProjectModel
        {
            Name = $"AssignProj-{uniqueSuffix}",
            AccountingString = $"assign-acc-{uniqueSuffix}",
            Description = "Project for assignment testing",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            UsageType = HEAppE.ExtModels.JobReporting.Models.UsageTypeExt.NodeHours,
            PIEmail = "assign@test.ci",
            SessionCode = sessionCode
        };
        var project = await _client.PostJsonAsync<CreateProjectModel, ProjectExt>(
            "/heappe/Management/Project", projectModel);
        project.Should().NotBeNull();
        project.Id.Should().NotBeNull();
        var projectId = project.Id.Value;

        var clusterModel = new CreateClusterModel
        {
            Name = $"AssignClust-{uniqueSuffix}",
            Description = "Cluster for assignment testing",
            MasterNodeName = $"assign-node-{uniqueSuffix}.ci",
            SchedulerType = SchedulerTypeExt.Slurm,
            ConnectionProtocol = ClusterConnectionProtocolExt.Ssh,
            TimeZone = "UTC",
            Port = 22,
            UpdateJobStateByServiceAccount = true,
            DomainName = $"{uniqueSuffix}.ci",
            SessionCode = sessionCode
        };
        var cluster = await _client.PostJsonAsync<CreateClusterModel, ClusterExt>(
            "/heappe/Management/Cluster", clusterModel);
        cluster.Should().NotBeNull();
        cluster.Id.Should().NotBeNull();
        var clusterId = cluster.Id.Value;

        try
        {
            // Create Assignment
            var assignModel = new CreateProjectAssignmentToClusterModel
            {
                ProjectId = projectId,
                ClusterId = clusterId,
                ScratchStoragePath = "/scratch/test",
                ProjectStoragePath = "/projects/test",
                PreferredAuthType = ClusterAuthenticationCredentialsAuthType.Password,
                SessionCode = sessionCode
            };
            var createAssignResp = await _client.PostJsonAsync(
                "/heappe/Management/ProjectAssignmentToCluster", assignModel);
            createAssignResp.StatusCode.Should().Be(HttpStatusCode.OK);

            // Read Assignment by ProjectId and ClusterId
            var fetchedAssignment = await _client.GetJsonAsync<ClusterProjectExt>(
                $"/heappe/Management/ProjectAssignmentToCluster?projectId={projectId}&clusterId={clusterId}&sessionCode={sessionCode}");
            fetchedAssignment.Should().NotBeNull();
            fetchedAssignment.ProjectId.Should().Be(projectId);
            fetchedAssignment.ClusterId.Should().Be(clusterId);

            // List Assignments for Project
            var projectAssignments = await _client.GetJsonAsync<List<ClusterProjectExt>>(
                $"/heappe/Management/ProjectAssignmentToClusters?projectId={projectId}&sessionCode={sessionCode}");
            projectAssignments.Should().NotBeNull();
            projectAssignments.Should().Contain(a => a.ClusterId == clusterId);

            // Modify Assignment
            var modifyAssignModel = new ModifyProjectAssignmentToClusterModel
            {
                ProjectId = projectId,
                ClusterId = clusterId,
                ScratchStoragePath = "/scratch/updated",
                ProjectStoragePath = "/projects/updated",
                PreferredAuthType = ClusterAuthenticationCredentialsAuthType.Password,
                SessionCode = sessionCode
            };
            var modifyAssignResp = await _client.PutJsonAsync(
                "/heappe/Management/ProjectAssignmentToCluster", modifyAssignModel);
            modifyAssignResp.StatusCode.Should().Be(HttpStatusCode.OK);

            // Delete Assignment
            var removeAssignModel = new RemoveProjectAssignmentToClusterModel
            {
                ProjectId = projectId,
                ClusterId = clusterId,
                SessionCode = sessionCode
            };
            var deleteAssignResp = await _client.DeleteJsonAsync(
                "/heappe/Management/ProjectAssignmentToCluster", removeAssignModel);
            deleteAssignResp.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            // Clean up cluster and project
            await _client.DeleteJsonAsync("/heappe/Management/Cluster", new RemoveClusterModel { Id = clusterId, SessionCode = sessionCode });
            await _client.DeleteJsonAsync("/heappe/Management/Project", new RemoveProjectModel { Id = projectId, SessionCode = sessionCode });
        }
    }
}
