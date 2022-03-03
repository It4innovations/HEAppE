using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic.ConversionAdapter
{
    /// <summary>
    /// Slurm task adapter
    /// </summary>
    public class SlurmTaskAdapter : ISchedulerTaskAdapter
    {
        #region Instances
        /// <summary>
        /// Task (HPC job) allocation command builder
        /// </summary>
        protected StringBuilder _taskBuilder;

        /// <summary>
        /// Job priority multiplier for setting priority from range [0-max(int)]
        /// </summary>
        protected static readonly int _priorityMultiplier = 25000;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="taskSource"></param>
        public SlurmTaskAdapter(string taskSource)
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
                _taskBuilder.Append($" --priority {_priorityMultiplier * (int)value}");
            }
        }

        /// <summary>
        /// Task queue
        /// </summary>
        public string Queue
        {
            set
            {
                _taskBuilder.Append(!string.IsNullOrEmpty(value) ? $" --partition={value}" : string.Empty);
            }
        }

        /// <summary>
        /// Task cluster allocation name
        /// </summary>
        public string ClusterAllocationName
        {
            set
            {
                _taskBuilder.Append(!string.IsNullOrEmpty(value) ? $" --clusters={value}" : string.Empty);
            }
        }


        /// <summary>
        /// Task CPU Hyper Threading
        /// </summary>
        public bool CpuHyperThreading
        {
            set
            {
                _taskBuilder.Append(value ? " --hint=multithread" : string.Empty);
            }
        }

        /// <summary>
        /// JobArrays
        /// </summary>
        public string JobArrays
        {
            set
            {
                _taskBuilder.Append(!string.IsNullOrEmpty(value) ? $" --array={value}" : string.Empty);
            }
        }

        /// <summary>
        /// Task name
        /// </summary>
        public string Name
        {
            set
            {
                _taskBuilder.Append($" -J {value}");
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
                    var builder = new StringBuilder(" --dependency=afterok");
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
                _taskBuilder.Append(value ? " -exclusive=mcs" : string.Empty);
            }
        }

        /// <summary>
        /// Task rerunable
        /// </summary>
        public bool IsRerunnable
        {
            set
            {
                _taskBuilder.Append(value ? " --requeue" : " --no-requeue");
            }
        }

        /// <summary>
        /// Task runtime
        /// </summary>
        public int Runtime
        {
            set
            {
                TimeSpan wallTime = TimeSpan.FromSeconds(value);
                _taskBuilder.Append($" -t { wallTime:dd\\-hh\\:mm\\:ss}");
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
        /// </summary>
        public string StdInFilePath
        {
            set
            {
                _taskBuilder.Append(!string.IsNullOrEmpty(value) ? $" -i {value}" : string.Empty);
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
        /// </summary>
        public string WorkDirectory
        {
            set
            {
                _taskBuilder.Append(!string.IsNullOrEmpty(value) ? $" -D {value}" : string.Empty);
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
            var allocationCmdBuilder = new StringBuilder();
            string reqNodeGroupsCmd = PrepareNameOfNodesGroup(requestedNodeGroups);

            int nodeCount = maxCores / coresPerNode;
            nodeCount += maxCores % coresPerNode > 0 ? 1 : 0;

            TaskParalizationSpecification parSpec = paralizationSpecs.FirstOrDefault();
            allocationCmdBuilder.Append($" --nodes={nodeCount}{PrepareNameOfNodes(requiredNodes.ToArray(), nodeCount)}{reqNodeGroupsCmd}");

            if (parSpec is not null)
            {
                allocationCmdBuilder.Append(parSpec.MPIProcesses.HasValue ? $" --ntasks-per-node={parSpec.MPIProcesses.Value}" : string.Empty);
                allocationCmdBuilder.Append(parSpec.OpenMPThreads.HasValue ? $" --cpus-per-task={parSpec.OpenMPThreads.Value}" : string.Empty);
            }

            allocationCmdBuilder.Append(string.IsNullOrEmpty(placementPolicy) ? string.Empty : $" --constraint={placementPolicy}");
            _taskBuilder.Append(allocationCmdBuilder);
        }

        /// <summary>
        /// Set enviroment variables for task
        /// </summary>
        /// <param name="variables"></param>
        public void SetEnvironmentVariablesToTask(IEnumerable<EnvironmentVariable> variables)
        {
            if (variables != null && variables.Any())
            {
                _taskBuilder.Append(" --export ");
                foreach (EnvironmentVariable variable in variables)
                {
                    _taskBuilder.Append($"{variable.Name} = {variable.Value},");
                }
                _taskBuilder.Remove(_taskBuilder.Length - 1, 1);
            }
        }

        /// <summary>
        /// Set preparation command for task
        /// </summary>
        /// <param name="workDir">Task work dir</param>
        /// <param name="preparationScript">Task preparation script</param>
        /// <param name="commandLine">Task command</param>
        /// <param name="stdOutFile">Standard output file</param>
        /// <param name="stdErrFile">Standard error file</param>
        public void SetPreparationAndCommand(string workDir, string preparationScript, string commandLine, string stdOutFile, string stdErrFile, string recursiveSymlinkCommand)
        {
            _taskBuilder.Append($" --wrap \'cd {workDir};");
            _taskBuilder.Append(
                string.IsNullOrEmpty(recursiveSymlinkCommand)
                    ? string.Empty
                    : recursiveSymlinkCommand.Last().Equals(';') ? recursiveSymlinkCommand : $"{recursiveSymlinkCommand};rm {stdOutFile} {stdErrFile};");

            _taskBuilder.Append($"1>> {stdOutFile} 2>> {stdErrFile} ");
            _taskBuilder.Append(
                string.IsNullOrEmpty(preparationScript)
                    ? string.Empty
                    : preparationScript.Last().Equals(';') ? preparationScript : $"{preparationScript};");
            _taskBuilder.Append(
                string.IsNullOrEmpty(commandLine)
                    ? string.Empty
                    : commandLine.Last().Equals(';') ? commandLine : $"{commandLine};");

            _taskBuilder.Append('\'');
        }
        #endregion
        #region Local Members
        /// <summary>
        /// Prepare name of node group
        /// </summary>
        /// <param name="requestedNodeGroups">Node group names</param>
        /// <returns></returns>
        private static string PrepareNameOfNodesGroup(IEnumerable<string> requestedNodeGroups)
        {
            if (requestedNodeGroups.Any())
            {
                var builder = new StringBuilder($" --partition={requestedNodeGroups.First()}");
                foreach (string nodeGroup in requestedNodeGroups.Skip(1))
                {
                    builder.Append($",{nodeGroup}");
                }
                return builder.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Prepare name of nodes
        /// </summary>
        /// <param name="requestedNodeGroups">Node names</param>
        /// <param name="nodeCount">Node count</param>
        /// <returns></returns>
        private static string PrepareNameOfNodes(ICollection<string> requestedNodeGroups, int nodeCount)
        {
            if (requestedNodeGroups?.Count == nodeCount)
            {
                var builder = new StringBuilder($" --nodelist={requestedNodeGroups.First()}");
                foreach (string nodeGroup in requestedNodeGroups.Skip(1))
                {
                    builder.Append($",{nodeGroup}");
                }
                return builder.ToString();
            }
            else
            {
                return string.Empty;
            }
        }
        #endregion
    }
}