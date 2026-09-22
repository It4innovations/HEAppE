using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DataAccessTier;
using HEAppE.DataAccessTier.Repository.JobManagement;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;

namespace HEAppE.DataAccessTier.Tests;

[Trait("Category", "Unit")]
public class ProjectRepositoryTests
{
    private DbContextOptions<MiddlewareContext> CreateNewContextOptions()
    {
        return new DbContextOptionsBuilder<MiddlewareContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetByIdWithAggregationsAsync_IncludesClusterProjectsAndAggregations()
    {
        var options = CreateNewContextOptions();

        using (var context = new MiddlewareContext(options))
        {
            var cluster = new Cluster
            {
                Id = 1,
                Name = "TestCluster",
                MasterNodeName = "node1",
                Description = "Test Cluster Description"
            };
            var credentials = new ClusterAuthenticationCredentials
            {
                Id = 100,
                Username = "hpcuser"
            };
            var clusterProject = new ClusterProject
            {
                ClusterId = 1,
                Cluster = cluster,
                ScratchStoragePath = "/scratch/path",
                ClusterProjectCredentials = new List<ClusterProjectCredential>
                {
                    new()
                    {
                        ClusterAuthenticationCredentials = credentials,
                        AdaptorUserId = 123
                    }
                }
            };
            var aggregation = new ClusterNodeTypeAggregation
            {
                Id = 1,
                Name = "Aggregation1",
                Description = "Desc"
            };
            var projectAggregation = new ProjectClusterNodeTypeAggregation
            {
                ClusterNodeTypeAggregationId = 1,
                ClusterNodeTypeAggregation = aggregation,
                AllocationAmount = 100
            };

            var project = new Project
            {
                Id = 2,
                Name = "TestProject",
                AccountingString = "acc-proj",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(10),
                ClusterProjects = new List<ClusterProject> { clusterProject },
                ProjectClusterNodeTypeAggregations = new List<ProjectClusterNodeTypeAggregation> { projectAggregation }
            };

            context.Clusters.Add(cluster);
            context.ClusterAuthenticationCredentials.Add(credentials);
            context.Projects.Add(project);
            await context.SaveChangesAsync();
        }

        using (var context = new MiddlewareContext(options))
        {
            var repo = new ProjectRepository(context);
            var result = await repo.GetByIdWithAggregationsAsync(2);

            result.Should().NotBeNull();
            result.Id.Should().Be(2);
            result.ClusterProjects.Should().NotBeNull().And.HaveCount(1);
            result.ClusterProjects[0].ClusterId.Should().Be(1);
            result.ClusterProjects[0].Cluster.Should().NotBeNull();
            result.ClusterProjects[0].Cluster.Name.Should().Be("TestCluster");
            result.ClusterProjects[0].ClusterProjectCredentials.Should().NotBeNull().And.HaveCount(1);
            result.ClusterProjects[0].ClusterProjectCredentials[0].AdaptorUserId.Should().Be(123);
            result.ProjectClusterNodeTypeAggregations.Should().NotBeNull().And.HaveCount(1);
            result.ProjectClusterNodeTypeAggregations[0].ClusterNodeTypeAggregation.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task GetByIdAsync_IncludesClusterProjectsAndContacts()
    {
        var options = CreateNewContextOptions();

        using (var context = new MiddlewareContext(options))
        {
            var cluster = new Cluster
            {
                Id = 1,
                Name = "TestCluster",
                MasterNodeName = "node1",
                Description = "Test Cluster Description"
            };
            var clusterProject = new ClusterProject
            {
                ClusterId = 1,
                Cluster = cluster,
                ScratchStoragePath = "/scratch/path"
            };
            var project = new Project
            {
                Id = 3,
                Name = "AsyncProject",
                AccountingString = "acc-proj-async",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(10),
                ClusterProjects = new List<ClusterProject> { clusterProject }
            };

            context.Clusters.Add(cluster);
            context.Projects.Add(project);
            await context.SaveChangesAsync();
        }

        using (var context = new MiddlewareContext(options))
        {
            var repo = new ProjectRepository(context);
            var result = await repo.GetByIdAsync(3);

            result.Should().NotBeNull();
            result.Id.Should().Be(3);
            result.ClusterProjects.Should().NotBeNull().And.HaveCount(1);
            result.ClusterProjects[0].ClusterId.Should().Be(1);
            result.ClusterProjects[0].Cluster.Should().NotBeNull();
            result.ClusterProjects[0].Cluster.Name.Should().Be("TestCluster");
        }
    }
}
