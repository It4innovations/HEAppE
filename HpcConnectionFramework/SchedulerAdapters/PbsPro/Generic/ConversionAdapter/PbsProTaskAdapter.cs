using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic.ConversionAdapter;

/// <summary>
///     PBS Professional task adapter (HPC Job)
/// </summary>
public class PbsProTaskAdapter : ISchedulerTaskAdapter
{
    #region Instances

    /// <summary>
    ///     Build script instead of command line parameters
    /// </summary>
    protected bool _pbs;

    /// <summary>
    ///     Task allocation command builder
    /// </summary>
    protected StringBuilder _taskAppender;

    /// <summary>
    ///     Append parameter to builder
    /// </summary>
    protected void DoAppend(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;
        if (_pbs)
            _taskAppender.AppendLine("#PBS " + value);
        else
            _taskAppender.Append(value);
    }

    #endregion

    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="taskSource">Basic allocation command</param>
    public PbsProTaskAdapter(string taskSource)
    {
        _pbs = taskSource.StartsWith("#!");
        _taskAppender = new StringBuilder(taskSource);
        if (_pbs)
            _taskAppender.AppendLine();
    }

    #endregion

    #region Local Members

    /// <summary>
    ///     Prepare name of node group
    /// </summary>
    /// <param name="requestedNodeGroups">Node group names</param>
    /// <param name="placementPolicy">Placement policy</param>
    /// <param name="paralizationSpecs">Paralization specifications</param>
    /// <param name="coreCount">Core count</param>
    /// <param name="coresPerNode">Cores per node</param>
    /// <returns></returns>
    private static string GenerateSelectPartForRequestedGroups(IEnumerable<string> requestedNodeGroups,
        string placementPolicy, IEnumerable<TaskParalizationSpecification> paralizationSpecs, int coreCount,
        int coresPerNode)
    {
        var builder = new StringBuilder();
        var reqNodeGroupsCmd = string.Empty;
        if (requestedNodeGroups.Any())
        {
            foreach (var nodeGroup in requestedNodeGroups) builder.Append($":{nodeGroup}=true");
            reqNodeGroupsCmd = builder.ToString();
            builder.Clear();
        }

        if (paralizationSpecs.Any())
        {
            var first = true;
            foreach (var pSpec in paralizationSpecs)
            {
                var nodeCount = pSpec.MaxCores / coresPerNode;
                nodeCount = pSpec.MaxCores % coresPerNode > 0 ? nodeCount + 1 : nodeCount;

                builder.Append($"{(first ? string.Empty : "+")}{nodeCount}{reqNodeGroupsCmd}:ncpus={coresPerNode}");
                builder.Append(pSpec.MPIProcesses.HasValue ? $":mpiprocs={pSpec.MPIProcesses.Value}" : string.Empty);
                builder.Append(pSpec.OpenMPThreads.HasValue
                    ? $":ompthreads={pSpec.OpenMPThreads.Value}"
                    : string.Empty);
                builder.Append(string.IsNullOrEmpty(placementPolicy) ? string.Empty : $" -l place={placementPolicy}");

                if (first) first = false;
            }

            var remainingCores = coreCount - paralizationSpecs.Sum(s => s.MaxCores);
            if (remainingCores > 0)
            {
                var nodeCount = remainingCores / coresPerNode;
                nodeCount = remainingCores % coresPerNode > 0 ? nodeCount + 1 : nodeCount;
                builder.Append($"+{nodeCount}{reqNodeGroupsCmd}:ncpus={coresPerNode}");
                builder.Append(string.IsNullOrEmpty(placementPolicy) ? string.Empty : $" -l place={placementPolicy}");
            }
        }
        else
        {
            var nodeCount = coreCount / coresPerNode;
            if (nodeCount >= 0)
            {
                nodeCount = coreCount % coresPerNode > 0 ? nodeCount + 1 : nodeCount;
                builder.Append($"{nodeCount}{reqNodeGroupsCmd}:ncpus={coresPerNode}");
                builder.Append(string.IsNullOrEmpty(placementPolicy) ? string.Empty : $" -l place={placementPolicy}");
            }
        }

        return builder.ToString();
    }

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
        set => DoAppend($" -p {(int)Math.Round(2047 / 8f * (int)value) - 1024}");
    }

    /// <summary>
    ///     Task Queue
    /// </summary>
    public string Queue
    {
        set => DoAppend(!string.IsNullOrEmpty(value) ? $" -q {value}" : string.Empty);
    }

    /// <summary>
    ///     Task quality of service
    ///     Note: Not supported
    /// </summary>
    public string QualityOfService
    {
        set { }
    }

    /// <summary>
    ///     Task cluster allocation name
    ///     Note: Not supported
    /// </summary>
    public string ClusterAllocationName
    {
        set { }
    }

    /// <summary>
    ///     Reservation
    /// </summary>
    public string Reservation
    {
        set => DoAppend(!string.IsNullOrEmpty(value) ? $" -U {value}" : string.Empty);
    }

    /// <summary>
    ///     Task CPU Hyper Threading
    /// </summary>
    public bool CpuHyperThreading
    {
        set => DoAppend(value ? " -l cpu_hyper_threading=true" : string.Empty);
    }

    /// <summary>
    ///     JobArrays
    /// </summary>
    public string JobArrays
    {
        set => DoAppend(!string.IsNullOrEmpty(value) ? $" -J {value}" : string.Empty);
    }

    /// <summary>
    ///     Task name
    /// </summary>
    public string Name
    {
        set => DoAppend($" -N {value}");
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
                if (_pbs)
                    _taskAppender.Append("#SBATCH");

                var builder = new StringBuilder(" -W depend=afterok");
                value.ToList().ForEach(f => builder.Append($":$_{f.ParentTaskSpecification.Id}"));
                _taskAppender.Append(builder);

                if (_pbs)
                    _taskAppender.AppendLine();
            }
        }
    }

    /// <summary>
    ///     Task exclusivity
    /// </summary>
    public bool IsExclusive
    {
        set => DoAppend(value ? " -l place=free:excl" : string.Empty);
    }

    /// <summary>
    ///     Task rerunable
    /// </summary>
    public bool IsRerunnable
    {
        set => DoAppend(value ? " -r y" : " -r n");
    }

    /// <summary>
    ///     Task run time
    /// </summary>
    public int Runtime
    {
        set
        {
            var wallTime = TimeSpan.FromSeconds(value);
            DoAppend($" -l walltime={wallTime:hh\\:mm\\:ss}");
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
    ///     Note: Not supported
    /// </summary>
    public string StdInFilePath
    {
        set { }
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
    ///     Note: Work directory in the PBS scheduler is set to the actual directory from which the qsub command is ran. This
    ///     means that the working directory has to be changed before calling qsub
    /// </summary>
    public string WorkDirectory
    {
        set
        {
            //DoAppend(!string.IsNullOrEmpty(value) ? $" -d {value}" : string.Empty);
        }
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
        set => DoAppend(value != null ? $" -l mem={value}mb" : string.Empty);
    }

    public long? MemoryPerCPU
    {
        set => DoAppend(value != null ? $" -l mem={value}mb" : string.Empty);
    }

    public long? MemoryPerGPU
    {
        set => DoAppend(value != null ? $" -l gpu_mem={value}mb" : string.Empty);
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
        bool isGpuAllocation = aggregation != null && (aggregation.AllocationType.Contains("ACN", StringComparison.OrdinalIgnoreCase) || aggregation.AllocationType.Contains("GPU", StringComparison.OrdinalIgnoreCase));

        var allocationCmdBuilder = new StringBuilder(" -l select=");

        if (isGpuAllocation)
        {
            int nodes = gpuNodes ?? 1;
            int gpuCount = gpuCores ?? 1;
            int gpusPerNode = gpuCount / nodes;
            if (gpusPerNode == 0) gpusPerNode = 1;

            if (requiredNodes?.Count > 0)
            {
                nodes = requiredNodes.Count;
                gpusPerNode = gpuCount / nodes;
                if (gpusPerNode == 0) gpusPerNode = 1;

                var first = true;
                foreach (var hostname in requiredNodes)
                {
                    allocationCmdBuilder.Append($"{(first ? string.Empty : "+")}1:host={hostname}:ncpus={coresPerNode}:ngpus={gpusPerNode}");
                    first = false;
                }
            }
            else
            {
                var reqNodeGroupsCmd = string.Empty;
                if (requestedNodeGroups.Any())
                {
                    var builder = new StringBuilder();
                    foreach (var nodeGroup in requestedNodeGroups) builder.Append($":{nodeGroup}=true");
                    reqNodeGroupsCmd = builder.ToString();
                }

                int cpusPerChunk = gpuNodes.HasValue && gpuNodes.Value > 0 ? coresPerNode : 1;
                allocationCmdBuilder.Append($"{nodes}{reqNodeGroupsCmd}:ncpus={cpusPerChunk}:ngpus={gpusPerNode}");
            }
        }
        else
        {
            if (gpuCores.HasValue || gpuNodes.HasValue)
            {
                throw new HEAppE.Exceptions.External.InputValidationException("GpuAllocationNotSupportedForCpuNodeType");
            }

            if (!maxCores.HasValue || maxCores <= 0)
                throw new ArgumentException("Argument 'maxCores' have to be specified for PbsPro CPU task.");

            //For specific node names
            if (requiredNodes?.Count > 0)
            {
                var requiredNodesMaxCores = coresPerNode * requiredNodes.Count;
                var remainingCores = maxCores - requiredNodesMaxCores;
                var cpusPerHost = maxCores > requiredNodesMaxCores ? coresPerNode : maxCores / requiredNodes.Count;

                var parSpecsForReqNodes = paralizationSpecs
                    .Where(w => w.MaxCores % coresPerNode == 0 && w.MaxCores / coresPerNode == 1)
                    .ToList();

                var i = 0;
                var first = true;
                foreach (var hostname in requiredNodes)
                {
                    var parSpec = parSpecsForReqNodes?.ElementAtOrDefault(i);
                    allocationCmdBuilder.Append($"{(first ? string.Empty : "+")}1:host={hostname}:ncpus={coresPerNode}");
                    if (parSpec != null)
                    {
                        allocationCmdBuilder.Append(parSpec.MPIProcesses.HasValue
                            ? $":mpiprocs={parSpec.MPIProcesses.Value}"
                            : string.Empty);
                        allocationCmdBuilder.Append(parSpec.OpenMPThreads.HasValue
                            ? $":ompthreads={parSpec.OpenMPThreads.Value}"
                            : string.Empty);
                    }

                    i++;
                    if (first)
                        first = false;
                }

                if (remainingCores > 0)
                {
                    allocationCmdBuilder.Append('+');
                    allocationCmdBuilder.Append(GenerateSelectPartForRequestedGroups(requestedNodeGroups, placementPolicy,
                        paralizationSpecs.Except(parSpecsForReqNodes).ToList(), (int)remainingCores, coresPerNode));
                }
            }
            else
            {
                allocationCmdBuilder.Append(GenerateSelectPartForRequestedGroups(requestedNodeGroups, placementPolicy,
                    paralizationSpecs, (int)maxCores, coresPerNode));
            }
        }

        if (!string.IsNullOrEmpty(placementPolicy))
        {
            allocationCmdBuilder.Append($" -l place={placementPolicy}");
        }

        DoAppend(allocationCmdBuilder.ToString());
    }

    /// <summary>
    ///     Set variables for task
    /// </summary>
    /// <param name="variables">Task variables</param>
    public void SetEnvironmentVariablesToTask(IEnumerable<EnvironmentVariable> variables)
    {
        if (variables != null && variables.Any())
        {
            if (_pbs)
                _taskAppender.Append("#PBS ");
            
            _taskAppender.Append(" -v ");
            foreach (var variable in variables)
                _taskAppender.Append($"{variable.Name}={variable.Value},");
            _taskAppender.Remove(_taskAppender.Length - 1, 1);

            if (_pbs)
                _taskAppender.AppendLine();
        }
    }

    /// <summary>
    ///     Set preparation command for task
    /// </summary>
    /// <param name="workDir">Task work directory</param>
    /// <param name="preparationScript">Task preparation script</param>
    /// <param name="commandLine">Task command</param>
    /// <param name="stdOutFile">Standard output file</param>
    /// <param name="stdErrFile">Standard error file</param>
    public void SetPreparationAndCommand(string workDir, string preparationScript, string commandLine,
        string stdOutFile, string stdErrFile, string recursiveSymlinkCommand)
    {
        var nodefileDir = workDir.Substring(0, workDir.LastIndexOf('/'));
        var taskSourceSb = new StringBuilder();

        if (UseCallback)
        {
            if (!_pbs)
                taskSourceSb.Append("echo '");

            taskSourceSb.Append($"cd {nodefileDir};cd {workDir};");
            
            taskSourceSb.Append("mkdir -p .heappe; ");
            // 1. Write callback token to file with 600 permissions
            taskSourceSb.Append($"echo \"{CallbackSecret}\" > .heappe/callback_token; chmod 600 .heappe/callback_token;");

            // 2. Symlink command
            if (!string.IsNullOrEmpty(recursiveSymlinkCommand))
            {
                taskSourceSb.Append(recursiveSymlinkCommand.Last().Equals(';')
                    ? recursiveSymlinkCommand
                    : $"{recursiveSymlinkCommand};");
            }

            // 3. Write user task script using heredoc
            taskSourceSb.Append("cat << \"EOF\" > .heappe/heappe_user_task.sh\n");
            if (!string.IsNullOrEmpty(preparationScript))
            {
                var escapedPrep = preparationScript.Replace("'", "'\\''");
                taskSourceSb.Append(escapedPrep.Last().Equals('\n') ? escapedPrep : $"{escapedPrep}\n");
            }
            if (!string.IsNullOrEmpty(commandLine))
            {
                var escapedCmd = commandLine.Replace("'", "'\\''");
                taskSourceSb.Append(escapedCmd.Last().Equals('\n') ? escapedCmd : $"{escapedCmd}\n");
            }
            taskSourceSb.Append("EOF\n");
            taskSourceSb.Append("chmod +x .heappe/heappe_user_task.sh;");

            // 4. Run the wrapper script and redirect output
            taskSourceSb.Append($"rm -f {stdOutFile} {stdErrFile}; touch {stdOutFile} {stdErrFile};");
            taskSourceSb.Append($"bash {WrapperScriptPath} \"{CallbackUrl}\" \"pbs\" 1>> {stdOutFile} 2>> {stdErrFile};");

            if (!_pbs)
            {
                taskSourceSb.Append("'");
                taskSourceSb.Append($" | {_taskAppender}");
                _taskAppender = taskSourceSb;
            }
            else
            {
                _taskAppender.AppendLine();
                _taskAppender.Append(taskSourceSb.ToString());
            }

            return;
        }

        if (!_pbs)
            taskSourceSb.Append($"echo '");
        taskSourceSb.Append($"cd {nodefileDir};cd {workDir};");
        taskSourceSb.Append(
            string.IsNullOrEmpty(recursiveSymlinkCommand)
                ? string.Empty
                : recursiveSymlinkCommand.Last().Equals(';')
                    ? recursiveSymlinkCommand
                    : $"{recursiveSymlinkCommand};rm {stdOutFile} {stdErrFile};touch {stdOutFile} {stdErrFile};");

        taskSourceSb.Append(
            string.IsNullOrEmpty(preparationScript)
                ? string.Empty
                : preparationScript.Last().Equals(';')
                    ? preparationScript
                    : $"{preparationScript};");

        taskSourceSb.Append(
            string.IsNullOrEmpty(commandLine)
                ? string.Empty
                : commandLine.Last().Equals(';')
                    ? commandLine
                    : $"{commandLine};");
        if (!_pbs)
        {
            taskSourceSb.Append($"'");
            taskSourceSb.Append($" | {_taskAppender}");
            //taskSourceSb.Append($"1>> {stdOutFile} 2>> {stdErrFile}' | {_taskBuilder}");
            _taskAppender = taskSourceSb;
        }
        else
        {
            _taskAppender.AppendLine();
            _taskAppender.Append(taskSourceSb.ToString());
        }
    }

    #endregion
}
