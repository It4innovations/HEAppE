using System;
using System.Collections.Generic;
using System.Text;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic.ConversionAdapter;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.Schedulers;

[Trait("Category", "Unit")]
public class SlurmScriptGenerationTests
{
    [Fact]
    public void SlurmTaskAdapter_BasicDirectives_GeneratesCorrectParameters()
    {
        var adapter = new SlurmTaskAdapter("");
        adapter.Name = "TestTask";
        adapter.Queue = "standard";
        adapter.QualityOfService = "high";
        adapter.Priority = TaskPriority.High;
        adapter.WorkDirectory = "/scratch/job1";
        adapter.StdOutFilePath = "/scratch/job1/stdout.txt";
        adapter.StdErrFilePath = "/scratch/job1/stderr.txt";
        adapter.StdInFilePath = "/scratch/job1/stdin.txt";
        adapter.IsExclusive = true;
        adapter.IsRerunnable = true;
        adapter.Runtime = 3665; // 1h 1m 5s -> 00-01:01:05
        adapter.JobArrays = "1-10";

        var cmd = adapter.AllocationCmd.ToString();

        cmd.Should().Contain("-J TestTask");
        cmd.Should().Contain("--partition=standard");
        cmd.Should().Contain("--qos=high");
        cmd.Should().Contain("-D /scratch/job1");
        cmd.Should().Contain("-o /scratch/job1/stdout.txt");
        cmd.Should().Contain("-e /scratch/job1/stderr.txt");
        cmd.Should().Contain("-i /scratch/job1/stdin.txt");
        cmd.Should().Contain("-exclusive=mcs");
        cmd.Should().Contain("--requeue");
        cmd.Should().Contain("-t 00-01:01:05");
        cmd.Should().Contain("--array=1-10");
    }

    [Fact]
    public void SlurmTaskAdapter_MemoryDirectives_GeneratesCorrectFlags()
    {
        var adapter = new SlurmTaskAdapter("");
        adapter.Memory = 8192;
        adapter.MemoryPerCPU = 2048;
        adapter.MemoryPerGPU = 4096;

        var cmd = adapter.AllocationCmd.ToString();

        cmd.Should().Contain("--mem=8192");
        cmd.Should().Contain("--mem-per-cpu=2048");
        cmd.Should().Contain("--mem-per-gpu=4096");
    }

    [Fact]
    public void SlurmTaskAdapter_SbatchMode_PrependsSbatchDirectives()
    {
        var taskSource = "#!/bin/bash\necho 'hello world'";
        var adapter = new SlurmTaskAdapter(taskSource);
        adapter.Name = "SbatchJob";
        adapter.Queue = "debug";
        adapter.CpuHyperThreading = true;

        var cmd = adapter.AllocationCmd.ToString();

        cmd.Should().Contain("#SBATCH");
        cmd.Should().Contain("-J SbatchJob");
        cmd.Should().Contain("--partition=debug");
        cmd.Should().Contain("--hint=multithread");
    }

    [Fact]
    public void SlurmTaskAdapter_Dependencies_GeneratesAfterokDirectives()
    {
        var adapter = new SlurmTaskAdapter("");
        var parent1 = new TaskSpecification { Id = 101 };
        var parent2 = new TaskSpecification { Id = 102 };

        var dependencies = new List<TaskDependency>
        {
            new TaskDependency { ParentTaskSpecification = parent1 },
            new TaskDependency { ParentTaskSpecification = parent2 }
        };

        adapter.DependsOn = dependencies;

        var cmd = adapter.AllocationCmd.ToString();
        cmd.Should().Contain("--dependency=afterok:$_101_parsed:$_102_parsed");
    }

    [Fact]
    public void SlurmTaskAdapter_GpuRequestStyles_GeneratesCorrectGpuFlags()
    {
        var gpuAggregation = new ClusterNodeTypeAggregation { AllocationType = "GPU" };

        // Style: "Gres" -> --gres=gpu:N
        var adapterGres = new SlurmTaskAdapter("");
        adapterGres.SlurmGpuRequestStyle = "Gres";
        adapterGres.SetRequestedResourceNumber(
            requestedNodeGroups: Array.Empty<string>(),
            requiredNodes: new List<string>(),
            placementPolicy: null,
            paralizationSpecs: Array.Empty<TaskParalizationSpecification>(),
            minCores: 8,
            maxCores: 8,
            gpuCores: 2,
            gpuNodes: 1,
            coresPerNode: 8,
            aggregation: gpuAggregation
        );
        adapterGres.AllocationCmd.ToString().Should().Contain("--gres=gpu:2");

        // Style: "Both" -> --gres=gpu:N and --gpus=N
        var adapterBoth = new SlurmTaskAdapter("");
        adapterBoth.SlurmGpuRequestStyle = "Both";
        adapterBoth.SetRequestedResourceNumber(
            requestedNodeGroups: Array.Empty<string>(),
            requiredNodes: new List<string>(),
            placementPolicy: null,
            paralizationSpecs: Array.Empty<TaskParalizationSpecification>(),
            minCores: 8,
            maxCores: 8,
            gpuCores: 4,
            gpuNodes: 1,
            coresPerNode: 8,
            aggregation: gpuAggregation
        );
        adapterBoth.AllocationCmd.ToString().Should().Contain("--gres=gpu:4");
        adapterBoth.AllocationCmd.ToString().Should().Contain("--gpus=4");
    }

    [Fact]
    public void SlurmTaskAdapter_EnvironmentVariables_GeneratesExports()
    {
        var adapter = new SlurmTaskAdapter("");
        var envVars = new List<EnvironmentVariable>
        {
            new EnvironmentVariable { Name = "MY_VAR", Value = "MY_VALUE" },
            new EnvironmentVariable { Name = "QUOTED_VAR", Value = "value with spaces" }
        };

        adapter.SetEnvironmentVariablesToTask(envVars);

        var cmd = adapter.AllocationCmd.ToString();
        cmd.Should().Contain("--export");
        cmd.Should().Contain("MY_VAR=\"MY_VALUE\"");
        cmd.Should().Contain("QUOTED_VAR=\"value with spaces\"");
    }

    [Theory]
    [InlineData("~", "$HOME")]
    [InlineData("~/", "$HOME/")]
    [InlineData("~/test-p01/HEAppE/Executions", "$HOME/test-p01/HEAppE/Executions")]
    [InlineData("/storage/scratch/user", "/storage/scratch/user")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void NormalizeShellPath_CorrectlyNormalizesTilde(string? input, string? expected)
    {
        SlurmTaskAdapter.NormalizeShellPath(input).Should().Be(expected);
    }

    [Fact]
    public void SlurmTaskAdapter_TildePath_ExpandsToHomeVariableInCallbackScript()
    {
        var adapter = new SlurmTaskAdapter("sbatch");
        adapter.UseCallback = true;
        adapter.CallbackSecret = "test-token-123";
        adapter.CallbackUrl = "https://heappe.example.com/callback";
        adapter.WrapperScriptPath = "~/.HEAppE/.ATR-26-1/test-p01/.key_scripts/task_wrapper.sh";

        adapter.SetPreparationAndCommand(
            workDir: "~/test-p01/HEAppE/Executions/kon0379/38988/38988",
            preparationScript: "echo prep",
            commandLine: "echo run",
            stdOutFile: "~/test-p01/HEAppE/Executions/kon0379/38988/38988/stdout",
            stdErrFile: "~/test-p01/HEAppE/Executions/kon0379/38988/38988/stderr",
            recursiveSymlinkCommand: null
        );

        var cmd = adapter.AllocationCmd.ToString();

        // Must NOT contain literal quotes around tildes which would break bash
        cmd.Should().NotContain("\"~/");

        // Must use $HOME inside quotes
        cmd.Should().Contain("mkdir -p \"$HOME/test-p01/HEAppE/Executions/kon0379/38988/38988/.heappe\"");
        cmd.Should().Contain("echo \"test-token-123\" > \"$HOME/test-p01/HEAppE/Executions/kon0379/38988/38988/.heappe/callback_token\"");
        cmd.Should().Contain("cat << \"EOF_HEAPPE_USER_TASK\" > \"$HOME/test-p01/HEAppE/Executions/kon0379/38988/38988/.heappe/heappe_user_task.sh\"");
        cmd.Should().Contain("chmod +x \"$HOME/test-p01/HEAppE/Executions/kon0379/38988/38988/.heappe/heappe_user_task.sh\"");
        cmd.Should().Contain("cd \"$HOME/test-p01/HEAppE/Executions/kon0379/38988/38988\"");
        cmd.Should().Contain("rm -f \"$HOME/test-p01/HEAppE/Executions/kon0379/38988/38988/stdout\" \"$HOME/test-p01/HEAppE/Executions/kon0379/38988/38988/stderr\"");
        cmd.Should().Contain("touch \"$HOME/test-p01/HEAppE/Executions/kon0379/38988/38988/stdout\" \"$HOME/test-p01/HEAppE/Executions/kon0379/38988/38988/stderr\"");
        cmd.Should().Contain("exec bash \"$HOME/.HEAppE/.ATR-26-1/test-p01/.key_scripts/task_wrapper.sh\" \"https://heappe.example.com/callback\" \"slurm\"");
        cmd.Should().Contain("1>> \"$HOME/test-p01/HEAppE/Executions/kon0379/38988/38988/stdout\" 2>> \"$HOME/test-p01/HEAppE/Executions/kon0379/38988/38988/stderr\"");
    }
}
