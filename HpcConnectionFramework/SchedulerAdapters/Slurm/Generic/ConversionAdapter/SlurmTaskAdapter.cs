using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic.ConversionAdapter;

/// <summary>
///     Slurm task adapter
/// </summary>
public class SlurmTaskAdapter : ISchedulerTaskAdapter
{
    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="taskSource"></param>
    public SlurmTaskAdapter(string taskSource)
    {
        _taskBuilder = new StringBuilder(taskSource);
    }

    #endregion

    #region Instances

    /// <summary>
    ///     Task (HPC job) allocation command builder
    /// </summary>
    protected StringBuilder _taskBuilder;

    /// <summary>
    ///     Job priority multiplier for setting priority from range [0-max(int)]
    /// </summary>
    protected static readonly int _priorityMultiplier = 25000;

    #endregion

    #region ISchedulerTaskAdapter Members

    /// <summary>
    ///     Task allocation command
    /// </summary>
    public object AllocationCmd => _taskBuilder.ToString();

    /// <summary>
    ///     Task priority
    /// </summary>
    public TaskPriority Priority
    {
        set => _taskBuilder.Append($" --priority {_priorityMultiplier * (int)value}");
    }

    /// <summary>
    ///     Task queue
    /// </summary>
    public string Queue
    {
        set => _taskBuilder.Append(!string.IsNullOrEmpty(value) ? $" --partition={value}" : string.Empty);
    }

    /// <summary>
    ///     Task quality of service
    /// </summary>
    public string QualityOfService
    {
        set => _taskBuilder.Append(!string.IsNullOrEmpty(value) ? $" --qos={value}" : string.Empty);
    }

    /// <summary>
    ///     Task cluster allocation name
    /// </summary>
    public string ClusterAllocationName
    {
        set => _taskBuilder.Append(!string.IsNullOrEmpty(value) ? $" --clusters={value}" : string.Empty);
    }


    /// <summary>
    ///     Task CPU Hyper Threading
    /// </summary>
    public bool CpuHyperThreading
    {
        set => _taskBuilder.Append(value ? " --hint=multithread" : string.Empty);
    }

    /// <summary>
    ///     JobArrays
    /// </summary>
    public string JobArrays
    {
        set => _taskBuilder.Append(!string.IsNullOrEmpty(value) ? $" --array={value}" : string.Empty);
    }

    /// <summary>
    ///     Task name
    /// </summary>
    public string Name
    {
        set => _taskBuilder.Append($" -J {value}");
    }

    /// <summary>
    ///     Project (Accounting string)
    /// </summary>
    public string Project
    {
        set => _taskBuilder.Append(!string.IsNullOrEmpty(value) ? $" -A {value}" : string.Empty);
    }

    /// <summary>
    ///     Task depend on
    /// </summary>
    public IEnumerable<TaskDependency> DependsOn
    {
        set
        {
            if (value != null && value.Any())
            {
                var builder = new StringBuilder(" --dependency=afterok");
                value.ToList().ForEach(f => builder.Append($":$_{f.ParentTaskSpecification.Id}_parsed"));
                _taskBuilder.Append(builder);
            }
        }
    }

    /// <summary>
    ///     Task exclusivity
    /// </summary>
    public bool IsExclusive
    {
        set => _taskBuilder.Append(value ? " -exclusive=mcs" : string.Empty);
    }

    /// <summary>
    ///     Task rerunable
    /// </summary>
    public bool IsRerunnable
    {
        set => _taskBuilder.Append(value ? " --requeue" : " --no-requeue");
    }

    /// <summary>
    ///     Task runtime
    /// </summary>
    public int Runtime
    {
        set
        {
            var wallTime = TimeSpan.FromSeconds(value);
            _taskBuilder.Append($" -t {wallTime:dd\\-hh\\:mm\\:ss}");
        }
    }

    /// <summary>
    ///     Task standard error file path
    /// </summary>
    public string StdErrFilePath
    {
        set => _taskBuilder.Append(!string.IsNullOrEmpty(value) ? $" -e {value}" : string.Empty);
    }

    /// <summary>
    ///     Task standard input file path
    /// </summary>
    public string StdInFilePath
    {
        set => _taskBuilder.Append(!string.IsNullOrEmpty(value) ? $" -i {value}" : string.Empty);
    }

    /// <summary>
    ///     Task standard output file path
    /// </summary>
    public string StdOutFilePath
    {
        set => _taskBuilder.Append(!string.IsNullOrEmpty(value) ? $" -o {value}" : string.Empty);
    }

    /// <summary>
    ///     Task work directory
    /// </summary>
    public string WorkDirectory
    {
        set => _taskBuilder.Append(!string.IsNullOrEmpty(value) ? $" -D {value}" : string.Empty);
    }

    /// <summary>
    ///     Extended allocation parameters from command template
    /// </summary>
    public string ExtendedAllocationCommand
    {
        set => _taskBuilder.Append(!string.IsNullOrEmpty(value) ? $" {value}" : string.Empty);
    }

    /// <summary>
    ///     Set requested resources for task
    /// </summary>
    /// <param name="requestedNodeGroups">Node group names</param>
    /// <param name="requiredNodes">Node names</param>
    /// <param name="placementPolicy">Specify placement policy (on same rack, etc.)</param>
    /// <param name="paralizationSpecs">Task parallel specifications</param>
    /// <param name="minCores">Task min cores</param>
    /// <param name="maxCores">Task max cores</param>
    /// <param name="coresPerNode">Cores per node</param>
    public void SetRequestedResourceNumber(IEnumerable<string> requestedNodeGroups, ICollection<string> requiredNodes,
        string placementPolicy, IEnumerable<TaskParalizationSpecification> paralizationSpecs, int minCores,
        int maxCores, int coresPerNode, ClusterNodeTypeAggregation aggregation)
    {
        if (maxCores <= 0)
            throw new ArgumentException($"Invalid number of cores: {maxCores}");

        var allocationCmdBuilder = new StringBuilder();
        var reqNodeGroupsCmd = PrepareNameOfNodesGroup(requestedNodeGroups);
        var parSpec = paralizationSpecs.FirstOrDefault();

        bool isPartialAllocation = aggregation != null && aggregation.AllocationType.Contains("partial-allocation", StringComparison.OrdinalIgnoreCase);
        bool isGpuAllocation = aggregation != null && (aggregation.AllocationType.Contains("ACN") || aggregation.AllocationType.Contains("GPU"));
        // GPU allocation
        if (isGpuAllocation)
        {
            // partial allocation
            if (isPartialAllocation)
            {
                allocationCmdBuilder.Append($" --gpus={maxCores}");
            }
            else
            {
                int gpuCount = maxCores;
                var nodeCount = maxCores / coresPerNode;
                nodeCount += maxCores % coresPerNode > 0 ? 1 : 0;
                allocationCmdBuilder.Append($" --gpus={gpuCount}");
                allocationCmdBuilder.Append($" --nodes={nodeCount}{PrepareNameOfNodes(requiredNodes.ToArray(), nodeCount)}{reqNodeGroupsCmd}");
            }
        }
        // CPU only allocation
        else
        {
            // TODO implement partial allocation?
            if (isPartialAllocation) { }

            var nodeCount = maxCores / coresPerNode;
            nodeCount += maxCores % coresPerNode > 0 ? 1 : 0;
            allocationCmdBuilder.Append(
                $" --nodes={nodeCount}{PrepareNameOfNodes(requiredNodes.ToArray(), nodeCount)}{reqNodeGroupsCmd}");
        }

        if (parSpec is not null)
        {
            if (parSpec.MPIProcesses.HasValue)
                allocationCmdBuilder.Append($" --ntasks-per-node={parSpec.MPIProcesses.Value}");

            if (parSpec.OpenMPThreads.HasValue)
                allocationCmdBuilder.Append($" --cpus-per-task={parSpec.OpenMPThreads.Value}");
        }

        if (!string.IsNullOrEmpty(placementPolicy))
            allocationCmdBuilder.Append($" --constraint={placementPolicy}");

        _taskBuilder.Append(allocationCmdBuilder);
    }


    /// <summary>
    ///     Set environment variables for task
    /// </summary>
    /// <param name="variables"></param>
    public void SetEnvironmentVariablesToTask(IEnumerable<EnvironmentVariable> variables)
    {
        if (variables != null && variables.Any())
        {
            _taskBuilder.Append(" --export ");
            foreach (var variable in variables) _taskBuilder.Append($"{variable.Name}={variable.Value},");
            _taskBuilder.Remove(_taskBuilder.Length - 1, 1);
        }
    }

    /// <summary>
    ///     Set preparation command for task
    /// </summary>
    /// <param name="workDir">Task work dir</param>
    /// <param name="preparationScript">Task preparation script</param>
    /// <param name="commandLine">Task command</param>
    /// <param name="stdOutFile">Standard output file</param>
    /// <param name="stdErrFile">Standard error file</param>
    public void SetPreparationAndCommand(string workDir, string preparationScript, string commandLine,
        string stdOutFile, string stdErrFile, string recursiveSymlinkCommand)
    {
        bool notificationScript = true;
        bool useControlLoop = true;
        int controlLoopSleepTime = 5;
        string outputFile = "script-demo.txt";

        _taskBuilder.Append($" --wrap \'");

        // TODO: check 2x3
        if (notificationScript)
        {
            //var timeLeft = @"timeLeft() { squeue -h -j $SLURM_JOB_ID -O TimeLeft | awk -F':|-' 'if (NF == 1) print $NF; else if (NF == 2) print ($1 * 60) + ($2); else if (NF == 3) print ($1 * 3600) + ($2 * 60) + $3; else if (NF == 4) print ($1 * 86400) + ($2 * 3600) + ($3 * 60) + $4' }";
            // 
            // echo \"$(get_job_state)\" >> cleanup_handler.txt; scontrol show JobId $SLURM_JOB_ID -o >> cleanup_handler.txt; 
            // echo \"$(get_job_state)\" >> exit_handler.txt; scontrol show JobId $SLURM_JOB_ID -o >> exit_handler.txt; 
            // squeue -j $SLURM_JOB_ID -h -o "%t"
            //"exit_handler() { local X=$?; send_state \"EXIT\"; scontrol show JobId $SLURM_JOB_ID -o >> " + outputFile + "; if [ $X -eq 0 ]; then send_state \"COMPLETED\"; else send_state \"FAILED\"; fi };",
            //var sendCommand = "sleep 5; echo $(date +\"%Y-%m-%d %H:%M:%S\") \": Job $SLURM_JOB_ID entered state $SLURM_JOB_STATE\" >> " + outputFile + ";";
            var sendCommand = "curl --header \"Content-Type: application/json\" --request POST --data \"{\\\"SLURM_JOB_ID\\\":\\\"$SLURM_JOB_ID\\\", \\\"SLURM_JOB_STATE\\\":\\\"$SLURM_JOB_STATE\\\"}\" http://127.0.0.1:1880/slurm --max-time 5; echo $(date +\"%Y-%m-%d %H:%M:%S\") \": Job $SLURM_JOB_ID entered state $SLURM_JOB_STATE\" >> " + outputFile + ";";
            var wrapperScript = new[] {
                "get_job_state() { echo $(squeue -j $SLURM_JOB_ID -h -o \"%T\" 2>/dev/null); };",
                "send_state() { local SLURM_JOB_STATE=$1; " + sendCommand + " };",
                "cleanup_handler() { trap - EXIT; scontrol show JobId $SLURM_JOB_ID -o >> " + outputFile + ";send_state \"CANCELLED_OR_TIMEOUT\";  exit 1; };",
                "exit_handler() { scontrol show JobId $SLURM_JOB_ID -o >> " + outputFile + "; if [ $PID_EXIT -eq 0 ]; then send_state \"COMPLETED\"; else send_state \"FAILED\"; fi; exit $PID_EXIT; };",
                "trap \'cleanup_handler\' 15;", // 15 = SIGTERM (use number for compatibility with default shell in --wrap environment)
                "trap \'exit_handler\' EXIT;",
                "control_loop() { local PID=$1; while kill -0 $PID 2>/dev/null; do sleep " + controlLoopSleepTime + "; scontrol show JobId $SLURM_JOB_ID -o >> " + outputFile + "; done };"
            };
            foreach (var line in wrapperScript)
                _taskBuilder.Append(line);

            _taskBuilder.Append("send_state \"BEGIN\";");
            _taskBuilder.Append("send_state \"$(get_job_state)\";");
            _taskBuilder.Append("{ ");
        }

        _taskBuilder.Append($"cd {workDir};");
        _taskBuilder.Append(
            string.IsNullOrEmpty(recursiveSymlinkCommand)
                ? string.Empty
                : recursiveSymlinkCommand.Last().Equals(';')
                    ? recursiveSymlinkCommand
                    : $"{recursiveSymlinkCommand};rm {stdOutFile} {stdErrFile};touch {stdOutFile} {stdErrFile};");

        _taskBuilder.Append($"1>> {stdOutFile} 2>> {stdErrFile} ");
        _taskBuilder.Append(
            string.IsNullOrEmpty(preparationScript)
                ? string.Empty
                : preparationScript.Last().Equals(';')
                    ? preparationScript
                    : $"{preparationScript};");
        _taskBuilder.Append(
            string.IsNullOrEmpty(commandLine)
                ? string.Empty
                : commandLine.Last().Equals(';')
                    ? commandLine
                    : $"{commandLine};");

        if (notificationScript)
        {
            _taskBuilder.Append(" }& PID=$!;");
            if (useControlLoop)
            {
                _taskBuilder.Append("echo \"START_CONTROL_LOOP\" >> " + outputFile + ";");
                _taskBuilder.Append("control_loop $PID & CL_PID=$!;");
                _taskBuilder.Append("wait $PID; PID_EXIT=$?;");
                _taskBuilder.Append("kill -s 15 $CL_PID;");
                _taskBuilder.Append("echo \"FINISH with code $PID_EXIT\" >> " + outputFile + ";");
                _taskBuilder.Append("exit $PID_EXIT");
            }
            else
            {
                _taskBuilder.Append("wait $PID; PID_EXIT=$?; exit $PID_EXIT");
            }
        }

        _taskBuilder.Append('\''); // wrap
    }

    #endregion

    #region Local Members

    /// <summary>
    ///     Prepare name of node group
    /// </summary>
    /// <param name="requestedNodeGroups">Node group names</param>
    /// <returns></returns>
    private static string PrepareNameOfNodesGroup(IEnumerable<string> requestedNodeGroups)
    {
        if (requestedNodeGroups.Any())
        {
            var builder = new StringBuilder($" --partition={requestedNodeGroups.First()}");
            foreach (var nodeGroup in requestedNodeGroups.Skip(1)) builder.Append($",{nodeGroup}");
            return builder.ToString();
        }

        return string.Empty;
    }

    /// <summary>
    ///     Prepare name of nodes
    /// </summary>
    /// <param name="requestedNodeGroups">Node names</param>
    /// <param name="nodeCount">Node count</param>
    /// <returns></returns>
    private static string PrepareNameOfNodes(ICollection<string> requestedNodeGroups, int nodeCount)
    {
        if (nodeCount > 0 && requestedNodeGroups?.Count == nodeCount)
        {
            var builder = new StringBuilder($" --nodelist={requestedNodeGroups.First()}");
            foreach (var nodeGroup in requestedNodeGroups.Skip(1)) builder.Append($",{nodeGroup}");
            return builder.ToString();
        }

        return string.Empty;
    }

    #endregion
}

/*
 //var timeLeft = @"timeLeft() { squeue -h -j $SLURM_JOB_ID -O TimeLeft | awk -F':|-' 'if (NF == 1) print $NF; else if (NF == 2) print ($1 * 60) + ($2); else if (NF == 3) print ($1 * 3600) + ($2 * 60) + $3; else if (NF == 4) print ($1 * 86400) + ($2 * 3600) + ($3 * 60) + $4' }";
            // 
            // echo \"$(get_job_state)\" >> cleanup_handler.txt; scontrol show JobId $SLURM_JOB_ID -o >> cleanup_handler.txt; 
            // echo \"$(get_job_state)\" >> exit_handler.txt; scontrol show JobId $SLURM_JOB_ID -o >> exit_handler.txt; 
            // squeue -j $SLURM_JOB_ID -h -o "%t"
            var sendCommand = "echo $(date +\"%Y-%m-%d %H:%M:%S\") \": Job $SLURM_JOB_ID entered state $SLURM_JOB_STATE\" >> " + outputFile + ";";
            var wrapperScript = new[] {
                "get_job_state() { echo $(squeue -j $SLURM_JOB_ID -h -o \"%T\" 2>/dev/null); };",
                "send_state() { local SLURM_JOB_STATE=$1; " + sendCommand + " };",
                "cleanup_handler() { trap - EXIT; send_state \"CANCELLED_OR_TIMEOUT\"; exit 1; };", // TODO: scontrol
                //"exit_handler() { local X=$?; send_state \"EXIT\"; scontrol show JobId $SLURM_JOB_ID -o >> " + outputFile + "; if [ $X -eq 0 ]; then send_state \"COMPLETED\"; else send_state \"FAILED\"; fi };",
                "exit_handler() { if [ $? -eq 0 ]; then send_state \"COMPLETED\"; else send_state \"FAILED\"; fi };",
                "trap \'cleanup_handler\' 15;", // 15 = SIGTERM (use number for compatibility with default shell)
                "trap \'exit_handler\' EXIT;",
                "control_loop() { local PID=$1; while kill -0 $PID 2>/dev/null; do sleep " + controlLoopSleepTime + "; scontrol show JobId $SLURM_JOB_ID -o >> " + outputFile + "; done };"
            };

*/