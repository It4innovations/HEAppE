using System;
using System.Collections.Generic;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic.ConversionAdapter;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.Schedulers;

[Trait("Category", "Unit")]
public class PbsProScriptGenerationTests
{
    [Fact]
    public void PbsProTaskAdapter_BasicDirectives_GeneratesCorrectPbsFlags()
    {
        var adapter = new PbsProTaskAdapter("#!/bin/bash\n");
        adapter.Name = "PbsTestJob";
        adapter.Queue = "workq";
        adapter.Project = "my_acc";
        adapter.Runtime = 3600; // 01:00:00
        adapter.IsExclusive = true;
        adapter.IsRerunnable = false;
        adapter.JobArrays = "1-5";
        adapter.Reservation = "res123";

        var cmd = adapter.AllocationCmd.ToString();

        cmd.Should().Contain("#PBS");
        cmd.Should().Contain("-N PbsTestJob");
        cmd.Should().Contain("-q workq");
        cmd.Should().Contain("-A my_acc");
        cmd.Should().Contain("-l walltime=01:00:00");
        cmd.Should().Contain("-l place=free:excl");
        cmd.Should().Contain("-r n");
        cmd.Should().Contain("-J 1-5");
        cmd.Should().Contain("-U res123");
    }

    [Fact]
    public void PbsProTaskAdapter_SelectPart_GeneratesNodeAndCoreSpecs()
    {
        var adapter = new PbsProTaskAdapter("");
        adapter.SetRequestedResourceNumber(
            requestedNodeGroups: Array.Empty<string>(),
            requiredNodes: new List<string>(),
            placementPolicy: null,
            paralizationSpecs: Array.Empty<TaskParalizationSpecification>(),
            minCores: 16,
            maxCores: 16,
            gpuCores: null,
            gpuNodes: null,
            coresPerNode: 8,
            aggregation: new ClusterNodeTypeAggregation { AllocationType = "CPU" }
        );

        var cmd = adapter.AllocationCmd.ToString();
        // 16 cores with 8 coresPerNode -> 2 nodes
        cmd.Should().Contain("-l select=2:ncpus=8");
    }

    [Fact]
    public void PbsProTaskAdapter_Dependencies_GeneratesAfterokDirectives()
    {
        var adapter = new PbsProTaskAdapter("");
        var parent1 = new TaskSpecification { Id = 201 };
        var dependencies = new List<TaskDependency>
        {
            new TaskDependency { ParentTaskSpecification = parent1 }
        };

        adapter.DependsOn = dependencies;

        var cmd = adapter.AllocationCmd.ToString();
        cmd.Should().Contain("-W depend=afterok:$_201");
    }
}
