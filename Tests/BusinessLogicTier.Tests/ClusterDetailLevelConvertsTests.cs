using System;
using System.Collections.Generic;
using FluentAssertions;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.Management.Models;
using Xunit;

namespace HEAppE.BusinessLogicTier.Tests;

public class ClusterDetailLevelConvertsTests
{
    private Cluster CreateSampleCluster()
    {
        var project = new Project
        {
            Id = 100,
            Name = "TestProject",
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(10)
        };

        var commandTemplate = new CommandTemplate
        {
            Id = 200,
            Name = "Template1",
            ProjectId = 100,
            IsDeleted = false
        };

        var cluster = new Cluster
        {
            Id = 1,
            Name = "Barbora",
            Description = "HPC Cluster Barbora",
            MasterNodeName = "barbora.it4i.cz",
            SchedulerType = SchedulerType.Slurm,
            ConnectionProtocol = ClusterConnectionProtocol.Ssh,
            TimeZone = "UTC",
            Port = 22,
            FileTransferMethods = new List<DomainObjects.FileTransfer.FileTransferMethod>(),
            ClusterProjects = new List<ClusterProject>
            {
                new ClusterProject
                {
                    ClusterId = 1,
                    ProjectId = 100,
                    Project = project,
                    IsDeleted = false
                }
            }
        };

        var nodeType = new ClusterNodeType
        {
            Id = 10,
            Name = "qcpu",
            ClusterId = 1,
            Cluster = cluster,
            NumberOfNodes = 10,
            CoresPerNode = 36,
            MaxWalltime = 3600,
            PossibleCommands = new List<CommandTemplate> { commandTemplate }
        };

        cluster.NodeTypes = new List<ClusterNodeType> { nodeType };
        return cluster;
    }

    [Fact]
    public void ConvertIntToExtendedExt_WithClustersOnly_LeavesNodeTypesNull()
    {
        var cluster = CreateSampleCluster();
        var projects = new List<Project> { cluster.ClusterProjects[0].Project };

        var ext = cluster.ConvertIntToExtendedExt(projects, false, ClusterDetailLevelExt.ClustersOnly);

        ext.Should().NotBeNull();
        ext.Id.Should().Be(1);
        ext.Name.Should().Be("Barbora");
        ext.NodeTypes.Should().BeNull();
    }

    [Fact]
    public void ConvertIntToExtendedExt_WithNodeTypes_PopulatesNodeTypes_LeavesProjectsNull()
    {
        var cluster = CreateSampleCluster();
        var projects = new List<Project> { cluster.ClusterProjects[0].Project };

        var ext = cluster.ConvertIntToExtendedExt(projects, false, ClusterDetailLevelExt.NodeTypes);

        ext.Should().NotBeNull();
        ext.NodeTypes.Should().NotBeNullOrEmpty();
        ext.NodeTypes.Length.Should().Be(1);
        ext.NodeTypes[0].Name.Should().Be("qcpu");
        ext.NodeTypes[0].Projects.Should().BeNull();
    }

    [Fact]
    public void ConvertIntToExtendedExt_WithProjects_PopulatesProjects_LeavesCommandTemplatesNullOrEmpty()
    {
        var cluster = CreateSampleCluster();
        var projects = new List<Project> { cluster.ClusterProjects[0].Project };

        var ext = cluster.ConvertIntToExtendedExt(projects, false, ClusterDetailLevelExt.Projects);

        ext.Should().NotBeNull();
        ext.NodeTypes.Should().NotBeNullOrEmpty();
        ext.NodeTypes[0].Projects.Should().NotBeNullOrEmpty();
        ext.NodeTypes[0].Projects[0].Id.Should().Be(100);
        ext.NodeTypes[0].Projects[0].CommandTemplates.Should().BeNull();
    }

    [Fact]
    public void ConvertIntToExtendedExt_WithFull_PopulatesFullHierarchy()
    {
        var cluster = CreateSampleCluster();
        var projects = new List<Project> { cluster.ClusterProjects[0].Project };

        var ext = cluster.ConvertIntToExtendedExt(projects, false, ClusterDetailLevelExt.Full);

        ext.Should().NotBeNull();
        ext.NodeTypes.Should().NotBeNullOrEmpty();
        ext.NodeTypes[0].Projects.Should().NotBeNullOrEmpty();
        ext.NodeTypes[0].Projects[0].CommandTemplates.Should().NotBeNullOrEmpty();
        ext.NodeTypes[0].Projects[0].CommandTemplates[0].Id.Should().Be(200);
    }

    [Theory]
    [InlineData(ClusterDetailLevel.Full, ClusterDetailLevelExt.Full)]
    [InlineData(ClusterDetailLevel.ClustersOnly, ClusterDetailLevelExt.ClustersOnly)]
    [InlineData(ClusterDetailLevel.NodeTypes, ClusterDetailLevelExt.NodeTypes)]
    [InlineData(ClusterDetailLevel.Projects, ClusterDetailLevelExt.Projects)]
    public void ClusterDetailLevel_EnumConversions_MapCorrectly(ClusterDetailLevel domainLevel, ClusterDetailLevelExt extLevel)
    {
        domainLevel.ConvertIntToExt().Should().Be(extLevel);
        extLevel.ConvertExtToInt().Should().Be(domainLevel);
    }
}
