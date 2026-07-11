using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;

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
        _sbatch = taskSource.StartsWith("#!");
        _taskAppender = new StringBuilder(taskSource);
        if (_sbatch)
            _taskAppender.AppendLine();
    }

    #endregion

    #region Instances

    /// <summary>
    ///     Build script instead of command line parameters
    /// </summary>
    protected bool _sbatch;

    /// <summary>
    ///     Task (HPC job) allocation command builder
    /// </summary>
    protected StringBuilder _taskAppender;

    /// <summary>
    ///     Task runtime in seconds
    /// </summary>
    protected int _runtime;

    /// <summary>
    ///     Append parameter to builder
    /// </summary>
    protected void DoAppend(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;
        if (_sbatch)
            _taskAppender.AppendLine("#SBATCH " + value);
        else
            _taskAppender.Append(value);
    }

    /// <summary>
    ///     Job priority multiplier for setting priority from range [0-max(int)]
    /// </summary>
    protected static readonly int _priorityMultiplier = 25000;

    #endregion

    #region ISchedulerTaskAdapter Members

    /// <summary>
    ///     Task allocation command
    /// </summary>
    public object AllocationCmd => _taskAppender.ToString();

    /// <summary>
    ///     Task priority
    /// </summary>
    public TaskPriority Priority
    {
        set => DoAppend($" --priority {_priorityMultiplier * (int)value}");
    }

    /// <summary>
    ///     Task queue
    /// </summary>
    public string Queue
    {
        set => DoAppend(!string.IsNullOrEmpty(value) ? $" --partition={value}" : string.Empty);
    }

    /// <summary>
    ///     Task quality of service
    /// </summary>
    public string QualityOfService
    {
        set => DoAppend(!string.IsNullOrEmpty(value) ? $" --qos={value}" : string.Empty);
    }

    /// <summary>
    ///     Task cluster allocation name
    /// </summary>
    public string ClusterAllocationName
    {
        set => DoAppend(!string.IsNullOrEmpty(value) ? $" --clusters={value}" : string.Empty);
    }

    /// <summary>
    ///     Reservation
    /// </summary>
    public string Reservation
    {
        set => DoAppend(!string.IsNullOrEmpty(value) ? $" --reservation={value}" : string.Empty);
    }


    /// <summary>
    ///     Task CPU Hyper Threading
    /// </summary>
    public bool CpuHyperThreading
    {
        set => DoAppend(value ? " --hint=multithread" : string.Empty);
    }

    /// <summary>
    ///     JobArrays
    /// </summary>
    public string JobArrays
    {
        set => DoAppend(!string.IsNullOrEmpty(value) ? $" --array={value}" : string.Empty);
    }

    /// <summary>
    ///     Task name
    /// </summary>
    public string Name
    {
        set => DoAppend($" -J {value}");
    }

    /// <summary>
    ///     Project (Accounting string)
    /// </summary>
    public string Project
    {
        set => DoAppend(!string.IsNullOrEmpty(value) ? $" -A {value}" : string.Empty);
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
                if (_sbatch)
                    _taskAppender.Append("#SBATCH");

                var builder = new StringBuilder(" --dependency=afterok");
                value.ToList().ForEach(f => builder.Append($":$_{f.ParentTaskSpecification.Id}_parsed"));
                _taskAppender.Append(builder);

                if (_sbatch)
                    _taskAppender.AppendLine();
            }
        }
    }

    /// <summary>
    ///     Task exclusivity
    /// </summary>
    public bool IsExclusive
    {
        set => DoAppend(value ? " -exclusive=mcs" : string.Empty);
    }

    /// <summary>
    ///     Task rerunable
    /// </summary>
    public bool IsRerunnable
    {
        set => DoAppend(value ? " --requeue" : " --no-requeue");
    }

    /// <summary>
    ///     Task runtime
    /// </summary>
    public int Runtime
    {
        set
        {
            _runtime = value;
            var wallTime = TimeSpan.FromSeconds(value);
            DoAppend($" -t {wallTime:dd\\-hh\\:mm\\:ss}");
        }
    }

    /// <summary>
    ///     Task standard error file path
    /// </summary>
    public string StdErrFilePath
    {
        set => DoAppend(!string.IsNullOrEmpty(value) ? $" -e {value}" : string.Empty);
    }

    /// <summary>
    ///     Task standard input file path
    /// </summary>
    public string StdInFilePath
    {
        set => DoAppend(!string.IsNullOrEmpty(value) ? $" -i {value}" : string.Empty);
    }

    /// <summary>
    ///     Task standard output file path
    /// </summary>
    public string StdOutFilePath
    {
        set => DoAppend(!string.IsNullOrEmpty(value) ? $" -o {value}" : string.Empty);
    }

    /// <summary>
    ///     Task work directory
    /// </summary>
    public string WorkDirectory
    {
        set => DoAppend(!string.IsNullOrEmpty(value) ? $" -D {value}" : string.Empty);
    }

    /// <summary>
    ///     Extended allocation parameters from command template
    /// </summary>
    public string ExtendedAllocationCommand
    {
        set => DoAppend(!string.IsNullOrEmpty(value) ? $" {value}" : string.Empty);
    }

    public long? Memory
    {
        set => DoAppend(value != null ? $" --mem={value}" : string.Empty);
    }

    public long? MemoryPerCPU
    {
        set => DoAppend(value != null ? $" --mem-per-cpu={value}" : string.Empty);
    }

    public long? MemoryPerGPU
    {
        set => DoAppend(value != null ? $" --mem-per-gpu={value}" : string.Empty);
    }

    public bool UseCallback { get; set; }
    public string CallbackSecret { get; set; }
    public string CallbackUrl { get; set; }
    public string WrapperScriptPath { get; set; }

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
        string placementPolicy, IEnumerable<TaskParalizationSpecification> paralizationSpecs, int? minCores,
        int? maxCores, int? gpuCores, int? gpuNodes, int coresPerNode, ClusterNodeTypeAggregation aggregation)
    {
        var allocationCmdBuilder = new StringBuilder();
        void doAppend(string value) {
            if (string.IsNullOrEmpty(value))
                return;
            if (_sbatch)
                allocationCmdBuilder.Append("#SBATCH");
            allocationCmdBuilder.Append(value);
            if (_sbatch)
                allocationCmdBuilder.AppendLine();
        }

        var reqNodeGroupsCmd = PrepareNameOfNodesGroup(requestedNodeGroups);
        var parSpec = paralizationSpecs.FirstOrDefault();

        bool isPartialAllocation = aggregation != null && aggregation.AllocationType.Contains("partial-allocation", StringComparison.OrdinalIgnoreCase);
        bool isGpuAllocation = aggregation != null && (aggregation.AllocationType.Contains("ACN", StringComparison.OrdinalIgnoreCase) || aggregation.AllocationType.Contains("GPU", StringComparison.OrdinalIgnoreCase));
        
        // GPU allocation
        if (isGpuAllocation)
        {
            int? gpuCount = null;
            if (maxCores.HasValue)
            {
                gpuCount = maxCores.Value;
            }
            else if (gpuCores.HasValue && gpuCores.Value > 0)
            {
                gpuCount = gpuCores.Value;
            }

            if (gpuCount.HasValue)
            {
                doAppend($" --gpus={gpuCount}");
            }

            // Append nodes if explicitly requested
            if (gpuNodes.HasValue && gpuNodes.Value > 0)
            {
                doAppend($" --nodes={gpuNodes.Value}{PrepareNameOfNodes(requiredNodes.ToArray(), gpuNodes.Value)}{reqNodeGroupsCmd}");
            }
            // Or calculate if maxCores is specified (legacy logic)
            else if (maxCores.HasValue)
            {
                int nodeCount = maxCores.Value / coresPerNode;
                nodeCount += maxCores.Value % coresPerNode > 0 ? 1 : 0;
                doAppend($" --nodes={nodeCount}{PrepareNameOfNodes(requiredNodes.ToArray(), nodeCount)}{reqNodeGroupsCmd}");
            }
        }
        // CPU only allocation
        else
        {
            if (gpuCores.HasValue || gpuNodes.HasValue)
            {
                throw new HEAppE.Exceptions.External.InputValidationException("GpuAllocationNotSupportedForCpuNodeType");
            }

            if (!maxCores.HasValue || maxCores <= 0)
                throw new ArgumentException($"Invalid number of cores: {maxCores} for Slurm task CPU allocation.");

            // TODO implement partial allocation?
            if (isPartialAllocation) { }

            // Calculate node count based on parallelization specifications
            int totalNodes = 0;
            int totalSpecCores = 0;
            if (paralizationSpecs != null && paralizationSpecs.Any())
            {
                foreach (var spec in paralizationSpecs)
                {
                    int specNodes = spec.MaxCores / coresPerNode;
                    specNodes += spec.MaxCores % coresPerNode > 0 ? 1 : 0;
                    totalNodes += specNodes;
                    totalSpecCores += spec.MaxCores;
                }
            }

            // Handle remaining cores (if maxCores is greater than spec sum, fill the rest)
            int effectiveMaxCores = maxCores ?? 0;
            int remainingCores = effectiveMaxCores - totalSpecCores;
            if (remainingCores > 0)
            {
                int remainingNodes = remainingCores / coresPerNode;
                remainingNodes += remainingCores % coresPerNode > 0 ? 1 : 0;
                totalNodes += remainingNodes;
            }

            if (totalNodes == 0 && effectiveMaxCores > 0)
            {
                totalNodes = effectiveMaxCores / coresPerNode;
                totalNodes += effectiveMaxCores % coresPerNode > 0 ? 1 : 0;
            }

            doAppend(
                $" --nodes={totalNodes}{PrepareNameOfNodes(requiredNodes.ToArray(), totalNodes)}{reqNodeGroupsCmd}");
        }

        if (parSpec is not null)
        {
            if (parSpec.MPIProcesses.HasValue)
                doAppend($" --ntasks-per-node={parSpec.MPIProcesses.Value}");

            if (parSpec.OpenMPThreads.HasValue)
                doAppend($" --cpus-per-task={parSpec.OpenMPThreads.Value}");
        }

        if (!string.IsNullOrEmpty(placementPolicy))
            doAppend($" --constraint={placementPolicy}");

        _taskAppender.Append(allocationCmdBuilder);
    }


    /// <summary>
    ///     Set environment variables for task
    /// </summary>
    /// <param name="variables"></param>
    public void SetEnvironmentVariablesToTask(IEnumerable<EnvironmentVariable> variables)
    {
        if (variables != null && variables.Any())
        {
            if (_sbatch)
                _taskAppender.Append("#SBATCH");

            _taskAppender.Append(" --export ");
            foreach (var variable in variables)
                _taskAppender.Append($"{variable.Name}={variable.Value},");
            _taskAppender.Remove(_taskAppender.Length - 1, 1);

            if (_sbatch)
                _taskAppender.AppendLine();
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
        if (UseCallback)
        {
            if (_runtime > 30)
            {
                DoAppend(" --signal=B:TERM@30");
            }

            if (_sbatch)
                _taskAppender.Append("#SBATCH");

            _taskAppender.Append($" --wrap \'cd {workDir};");
            
            _taskAppender.Append("mkdir -p .heappe; ");
            // 1. Write callback token to file with 600 permissions
            _taskAppender.Append($"echo \"{CallbackSecret}\" > .heappe/callback_token; chmod 600 .heappe/callback_token;");

            // 2. Symlink command
            if (!string.IsNullOrEmpty(recursiveSymlinkCommand))
            {
                _taskAppender.Append(recursiveSymlinkCommand.Last().Equals(';')
                    ? recursiveSymlinkCommand
                    : $"{recursiveSymlinkCommand};");
            }

            // 3. Write user task script
            _taskAppender.Append("cat << \"EOF\" > .heappe/heappe_user_task.sh\n");
            if (!string.IsNullOrEmpty(preparationScript))
            {
                var escapedPrep = preparationScript.Replace("'", "'\\''");
                _taskAppender.Append(escapedPrep.Last().Equals('\n') ? escapedPrep : $"{escapedPrep}\n");
            }
            if (!string.IsNullOrEmpty(commandLine))
            {
                var escapedCmd = commandLine.Replace("'", "'\\''");
                _taskAppender.Append(escapedCmd.Last().Equals('\n') ? escapedCmd : $"{escapedCmd}\n");
            }
            _taskAppender.Append("EOF\n");
            _taskAppender.Append("chmod +x .heappe/heappe_user_task.sh;");

            // 4. Run the wrapper script and redirect output
            _taskAppender.Append($"rm -f {stdOutFile} {stdErrFile}; touch {stdOutFile} {stdErrFile};");
            _taskAppender.Append($"exec bash {WrapperScriptPath} \"{CallbackUrl}\" \"slurm\" 1>> {stdOutFile} 2>> {stdErrFile};\'");

            if (_sbatch)
                _taskAppender.AppendLine();

            return;
        }

        if (_sbatch)
            _taskAppender.Append("#SBATCH");

        _taskAppender.Append($" --wrap \'cd {workDir};");
        _taskAppender.Append(
            string.IsNullOrEmpty(recursiveSymlinkCommand)
                ? string.Empty
                : recursiveSymlinkCommand.Last().Equals(';')
                    ? recursiveSymlinkCommand
                    : $"{recursiveSymlinkCommand};rm {stdOutFile} {stdErrFile};touch {stdOutFile} {stdErrFile};");

        _taskAppender.Append($"1>> {stdOutFile} 2>> {stdErrFile} ");
        _taskAppender.Append(
            string.IsNullOrEmpty(preparationScript)
                ? string.Empty
                : preparationScript.Last().Equals(';')
                    ? preparationScript
                    : $"{preparationScript};");
        _taskAppender.Append(
            string.IsNullOrEmpty(commandLine)
                ? string.Empty
                : commandLine.Last().Equals(';')
                    ? commandLine
                    : $"{commandLine};");

        _taskAppender.Append('\'');

        if (_sbatch)
            _taskAppender.AppendLine();
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
            foreach (var nodeGroup in requestedNodeGroups.Skip(1))
                builder.Append($",{nodeGroup}");
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