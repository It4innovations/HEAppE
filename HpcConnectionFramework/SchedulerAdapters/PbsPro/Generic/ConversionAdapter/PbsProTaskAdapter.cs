using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic.ConversionAdapter
{
    /// <summary>
    /// PBS Professional task adapter (HPC Job)
    /// </summary>
    public class PbsProTaskAdapter : ISchedulerTaskAdapter
    {
        #region Instances
        /// <summary>
        /// Task allocation command builder
        /// </summary>
        protected StringBuilder _taskBuilder;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="taskSource">Basic allocation command</param>
        public PbsProTaskAdapter(string taskSource)
        {
            _taskBuilder = new StringBuilder(taskSource);
        }
        #endregion
        #region ISchedulerTaskAdapter Members
        /// <summary>
        /// Task allocation command
        /// </summary>
        public object AllocationCmd
        {
            get
            {
                return _taskBuilder.ToString();
            }
        }

        /// <summary>
        /// Task priority
        /// </summary>
        public TaskPriority Priority
        {
            set
            {
                _taskBuilder.Append($" -p {((int)Math.Round((2047 / 8f) * (int)value) - 1024)}");
            }
        }

        /// <summary>
        /// Task Queue
        /// </summary>
        public string Queue
        {
            set
            {
                _taskBuilder.Append(!string.IsNullOrEmpty(value) ? $" -q {value}" : string.Empty);
            }
        }

        /// <summary>
        /// Task cluster allocation name
        /// Note: Not supported
        /// </summary>
        public string ClusterAllocationName
        {
            set
            { 

            }
        }

        /// <summary>
        /// Task CPU Hyper Threading
        /// </summary>
        public bool CpuHyperThreading
        {
            set
            {
                _taskBuilder.Append(value ? " -l cpu_hyper_threading=true" : string.Empty);
            }
        }

        /// <summary>
        /// JobArrays
        /// </summary>
        public string JobArrays
        {
            set
            {
                _taskBuilder.Append(!string.IsNullOrEmpty(value) ? $" -J {value}" : string.Empty);
            }
        }

        /// <summary>
        /// Task name
        /// </summary>
        public string Name
        {
            set
            {
                _taskBuilder.Append($" -N {value}");
            }
        }

        /// <summary>
        /// Task depend on
        /// </summary>
        public IEnumerable<TaskDependency> DependsOn
        {
            set
            {
                if (value != null && value.Any())
                {
                    StringBuilder builder = new StringBuilder(" -W depend=afterok");
                    foreach (TaskDependency taskDependency in value)
                    {
                        builder.Append(":$_");
                        builder.Append(taskDependency.ParentTaskSpecification.Id);
                    }
                    _taskBuilder.Append(builder);
                }
            }
        }

        /// <summary>
        /// Task exclusivity
        /// </summary>
        public bool IsExclusive
        {
            set
            {
                _taskBuilder.Append(" -l place=free:excl");
            }
        }

        /// <summary>
        /// Task rerunable
        /// </summary>
        public bool IsRerunnable
        {
            set
            {
                _taskBuilder.Append(value ? " -r y" : " -r n");
            }
        }

        /// <summary>
        /// Task run time
        /// </summary>
        public int Runtime
        {
            set
            {
                TimeSpan wallTime = TimeSpan.FromSeconds(value);
                _taskBuilder.Append($" -l walltime={ wallTime:hh\\:mm\\:ss}");
            }
        }

        /// <summary>
        /// Task standard error file path
        /// </summary>
        public string StdErrFilePath
        {
            set
            {
                _taskBuilder.Append(!string.IsNullOrEmpty(value) ? $" -e {value}" : string.Empty);
            }
        }

        /// <summary>
        /// Task standard input file path
        /// Note: Not supported
        /// </summary>
        public string StdInFilePath
        {
            set
            {

            }
        }

        /// <summary>
        /// Task standard output file path
        /// </summary>
        public string StdOutFilePath
        {
            set
            {
                _taskBuilder.Append(!string.IsNullOrEmpty(value) ? $" -o {value}" : string.Empty);
            }
        }

        /// <summary>
        /// Task work directory
        /// Note: Work directory in the PBS scheduler is set to the actual directory from which the qsub command is ran. This means that the working directory has to be changed before calling qsub
        /// </summary>
        public string WorkDirectory
        {
            set
            {
                //_taskBuilder.Append(!string.IsNullOrEmpty(value) ? $" -d {value}" : string.Empty);
            }
        }

        /// <summary>
        /// Set requested resources for task
        /// </summary>
        /// <param name="requestedNodeGroups">Node group names</param>
        /// <param name="requiredNodes">Node names</param>
        /// <param name="placementPolicy">Specify placement policy (on same rack, etc.)</param>
        /// <param name="paralizationSpecs">Task paralization specifications</param>
        /// <param name="minCores">Task min cores</param>
        /// <param name="maxCores">Task max cores</param>
        /// <param name="coresPerNode">Cores per node</param>
        public void SetRequestedResourceNumber(IEnumerable<string> requestedNodeGroups, ICollection<string> requiredNodes, string placementPolicy, IEnumerable<TaskParalizationSpecification> paralizationSpecs, int minCores, int maxCores, int coresPerNode)
        {
            var allocationCmdBuilder = new StringBuilder(" -l select=");

            //For specific node names
            if (requiredNodes?.Count > 0)
            {
                int requiredNodesMaxCores = coresPerNode * requiredNodes.Count;
                int remainingCores = maxCores - requiredNodesMaxCores;
                int cpusPerHost = maxCores > requiredNodesMaxCores ? coresPerNode : (maxCores / requiredNodes.Count);

                var parSpecsForReqNodes = paralizationSpecs.Where(w => w.MaxCores % coresPerNode == 0 && w.MaxCores / coresPerNode == 1)
                                                            .ToList();

                int i = 0;
                bool first = true;
                foreach (var hostname in requiredNodes)
                {
                    TaskParalizationSpecification parSpec = parSpecsForReqNodes?.ElementAtOrDefault(i);
                    allocationCmdBuilder.Append($"{(first ? string.Empty : "+")}1:host={hostname}:ncpus={coresPerNode}");
                    if (parSpec != null)
                    {
                        allocationCmdBuilder.Append(parSpec.MPIProcesses.HasValue ? $":mpiprocs={parSpec.MPIProcesses.Value}" : string.Empty);
                        allocationCmdBuilder.Append(parSpec.OpenMPThreads.HasValue ? $":ompthreads={parSpec.OpenMPThreads.Value}" : string.Empty);
                    }
                    allocationCmdBuilder.Append(string.IsNullOrEmpty(placementPolicy) ? string.Empty : $" -l place={placementPolicy}");

                    i++;
                    if (first)
                        first = false;
                }

                if (remainingCores > 0)
                {
                    allocationCmdBuilder.Append('+');
                    allocationCmdBuilder.Append(GenerateSelectPartForRequestedGroups(requestedNodeGroups, placementPolicy,
                                                  paralizationSpecs.Except(parSpecsForReqNodes).ToList(), remainingCores, coresPerNode));
                }
            }
            else
            {
                allocationCmdBuilder.Append(GenerateSelectPartForRequestedGroups(requestedNodeGroups, placementPolicy, paralizationSpecs, maxCores, coresPerNode));
            }
            _taskBuilder.Append(allocationCmdBuilder);
        }

        /// <summary>
        /// Set variables for task
        /// </summary>
        /// <param name="variables">Task varibles</param>
        public void SetEnvironmentVariablesToTask(IEnumerable<EnvironmentVariable> variables)
        {
            if (variables != null && variables.Any())
            {
                _taskBuilder.Append(" -v ");
                foreach (EnvironmentVariable variable in variables)
                {
                    _taskBuilder.Append($"{variable.Name}={variable.Value},");
                }
                _taskBuilder.Remove(_taskBuilder.Length - 1, 1);
            }
        }

        /// <summary>
        /// Set preparation command for task
        /// </summary>
        /// <param name="workDir">Task work directory</param>
        /// <param name="preparationScript">Task preparation script</param>
        /// <param name="commandLine">Task command</param>
        /// <param name="stdOutFile">Standard output file</param>
        /// <param name="stdErrFile">Standard error file</param>
        public void SetPreparationAndCommand(string workDir, string preparationScript, string commandLine, string stdOutFile, string stdErrFile, string recursiveSymlinkCommand)
        {
            string nodefileDir = workDir.Substring(0, workDir.LastIndexOf('/'));
            var taskSourceSb = new StringBuilder();
            taskSourceSb.Append($"echo 'cd {nodefileDir};cd {workDir};");
            taskSourceSb.Append(
                string.IsNullOrEmpty(recursiveSymlinkCommand)
                    ? string.Empty
                    : recursiveSymlinkCommand.Last().Equals(';') ? recursiveSymlinkCommand : $"{recursiveSymlinkCommand};rm {stdOutFile} {stdErrFile};");

            taskSourceSb.Append(
                string.IsNullOrEmpty(preparationScript)
                    ? string.Empty
                    : preparationScript.Last().Equals(';') ? preparationScript : $"{preparationScript};");

            taskSourceSb.Append(
                string.IsNullOrEmpty(commandLine)
                    ? string.Empty
                    : commandLine.Last().Equals(';') ? commandLine : $"{commandLine};");

            taskSourceSb.Append($"1>> {stdOutFile} 2>> {stdErrFile}' | {_taskBuilder}");
            _taskBuilder = taskSourceSb;
        }
        #endregion
        #region Local Members
        /// <summary>
        /// Prepare name of node group
        /// </summary>
        /// <param name="requestedNodeGroups">Node group names</param>
        /// <param name="placementPolicy">Placement policy</param>
        /// <param name="paralizationSpecs">Paralization specifications</param>
        /// <param name="coreCount">Core count</param>
        /// <param name="coresPerNode">Cores per node</param>
        /// <returns></returns>
        private static string GenerateSelectPartForRequestedGroups(IEnumerable<string> requestedNodeGroups, string placementPolicy, IEnumerable<TaskParalizationSpecification> paralizationSpecs, int coreCount, int coresPerNode)
        {
            var builder = new StringBuilder();
            string reqNodeGroupsCmd = string.Empty;
            if (requestedNodeGroups.Any())
            {
                foreach (string nodeGroup in requestedNodeGroups)
                {
                    builder.Append($":{nodeGroup}=true");
                }
                reqNodeGroupsCmd = builder.ToString();
                builder.Clear();
            }

            if (paralizationSpecs.Any())
            {
                bool first = true;
                foreach (var pSpec in paralizationSpecs)
                {
                    int nodeCount = pSpec.MaxCores / coresPerNode;
                    nodeCount = (pSpec.MaxCores % coresPerNode) > 0 ? nodeCount + 1 : nodeCount;

                    builder.Append($"{(first ? string.Empty : "+")}{nodeCount}{reqNodeGroupsCmd}:ncpus={coresPerNode}");
                    builder.Append(pSpec.MPIProcesses.HasValue ? $":mpiprocs={pSpec.MPIProcesses.Value}" : string.Empty);
                    builder.Append(pSpec.OpenMPThreads.HasValue ? $":ompthreads={pSpec.OpenMPThreads.Value}" : string.Empty);
                    builder.Append(string.IsNullOrEmpty(placementPolicy) ? string.Empty : $" -l place={placementPolicy}");

                    if (first)
                    {
                        first = false;
                    }
                }

                int remainingCores = coreCount - paralizationSpecs.Sum(s => s.MaxCores);
                if (remainingCores > 0)
                {
                    int nodeCount = remainingCores / coresPerNode;
                    nodeCount = (remainingCores % coresPerNode) > 0 ? nodeCount + 1 : nodeCount;
                    builder.Append($"+{nodeCount}{reqNodeGroupsCmd}:ncpus={coresPerNode}");
                    builder.Append(string.IsNullOrEmpty(placementPolicy) ? string.Empty : $" -l place={placementPolicy}");
                }
            }
            else
            {
                int nodeCount = coreCount / coresPerNode;
                if (nodeCount >= 0)
                {
                    nodeCount = (coreCount % coresPerNode) > 0 ? nodeCount + 1 : nodeCount;
                    builder.Append($"{nodeCount}{reqNodeGroupsCmd}:ncpus={coresPerNode}");
                    builder.Append(string.IsNullOrEmpty(placementPolicy) ? string.Empty : $" -l place={placementPolicy}");
                }
            }
            return builder.ToString();
        }
        #endregion
    }
}