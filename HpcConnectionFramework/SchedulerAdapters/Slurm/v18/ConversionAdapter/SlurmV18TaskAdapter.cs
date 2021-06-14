using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.v18.ConversionAdapter
{
    /// <summary>
    /// Class: Slurm task adapter
    /// </summary>
    public class SlurmV18TaskAdapter : ISchedulerTaskAdapter
    {
        #region Properties
        /// <summary>
        /// Task command builder (create job)
        /// </summary>
        protected StringBuilder _jobTaskBuilder;

        /// <summary>
        /// Task parameters (result of job)
        /// </summary>
        protected SlurmJobDTO _taskParameters;

        /// <summary>
        /// Job priority multiplier for setting priority from range [0-max(int)]
        /// </summary>
        protected static int _priorityMultiplier = 25000;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="taskSource"></param>
        public SlurmV18TaskAdapter(string taskSource)
        {
            //TODO divide create and read task parameters
            _jobTaskBuilder = new StringBuilder(taskSource);
            _taskParameters = SlurmConversionUtils.ReadParametersFromSqueueResponse(taskSource);
        }
        #endregion
        #region ISchedulerTaskAdapter Members
        /// <summary>
        /// Task command
        /// </summary>
        public object Source
        {
            get { return _jobTaskBuilder.ToString(); }
        }

        /// <summary>
        /// Task Id
        /// </summary>
        public string Id
        {
            get { return _taskParameters.Id.ToString(); }
        }

        /// <summary>
        /// Job priority
        /// </summary>
        public TaskPriority Priority
        {
            get { return (TaskPriority)(_taskParameters.Priority / _priorityMultiplier); }
            set { _jobTaskBuilder.Append($" --priority {_priorityMultiplier * (int)value}"); }
        }

        /// <summary>
        /// Task Queue
        /// </summary>
        public string Queue
        {
            get { return _taskParameters.QueueName; }
            set
            {
                _jobTaskBuilder.Append(!string.IsNullOrEmpty(value)
                    ? $" --partition={value}"
                    : string.Empty);
            }
        }

        /// <summary>
        /// CpuHyperThreading
        /// </summary>
        public bool CpuHyperThreading
        {
            set
            {
                if (value)
                {
                    _jobTaskBuilder.Append(" --hint=multithread");
                }
                else
                {
                    _jobTaskBuilder.Append(" --hint=nomultithread");
                }

            }
        }

        /// <summary>
        /// JobArrays
        /// </summary>
        public string JobArrays
        {
            set
            {
                _jobTaskBuilder.Append(!string.IsNullOrEmpty(value)
                    ? $" --array={value}"
                    : string.Empty);
            }
        }

        /// <summary>
        /// Task allocated core ids
        /// Notes: Slurm does not support cores for node
        /// </summary>
        public ICollection<string> AllocatedCoreIds
        {
            get { return _taskParameters.AllocatedNodes; }
        }

        /// <summary>
        /// Task name
        /// </summary>
        public string Name
        {
            get { return _taskParameters.Name; }
            set { _jobTaskBuilder.Append($" -J {value}"); }
        }

        /// <summary>
        /// Task state
        /// </summary>
        public virtual TaskState State
        {
            get
            {
                return _taskParameters.TaskState;
            }
        }

        /// <summary>
        /// Task start time
        /// </summary>
        public DateTime? StartTime
        {
            get { return _taskParameters.StartTime; }
        }

        /// <summary>
        /// Task end time
        /// </summary>
        public virtual DateTime? EndTime
        {
            get { return _taskParameters.EndTime; }
        }

        /// <summary>
        /// Task error message
        /// Note: Slurm does not have error message
        /// </summary>
        public string ErrorMessage
        {
            get { return null; }
        }

        /// <summary>
        /// Task depend on
        /// </summary>
        public ICollection<TaskDependency> DependsOn
        {
            set
            {
                if (value != null && value.Count > 0)
                {
                    StringBuilder builder = new StringBuilder(" --dependency=afterok");
                    foreach (TaskDependency taskDependency in value)
                    {
                        builder.Append(":$_");
                        builder.Append(taskDependency.ParentTaskSpecification.Id);
                    }
                    _jobTaskBuilder.Append(builder);
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
                if (value)
                {
                    _jobTaskBuilder.Append(" -exclusive=mcs");
                }
            }
        }

        /// <summary>
        /// Task rerunable
        /// </summary>
        public bool IsRerunnable
        {
            get { return _taskParameters.Requeue > 0; }
            set { _jobTaskBuilder.Append(value ? " --requeue" : " --no-requeue"); }
        }

        /// <summary>
        /// Task run time
        /// Note: Used Allocated time for job
        /// </summary>
        public int Runtime
        {
            get { return (int)_taskParameters.AllocatedTime.TotalSeconds; }
            set
            {
                TimeSpan wallTime = TimeSpan.FromSeconds(value);
                _jobTaskBuilder.Append($" -t { wallTime:dd\\-hh\\:mm\\:ss}");
            }
        }

        /// <summary>
        /// Task standard error file path
        /// </summary>
        public string StdErrFilePath
        {
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _jobTaskBuilder.Append($" -e {value}");
                }
            }
        }

        /// <summary>
        /// Task standard input file path
        /// </summary>
        public string StdInFilePath
        {
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _jobTaskBuilder.Append($" -i {value}");
                }
            }
        }

        /// <summary>
        /// Task standard output file path
        /// </summary>
        public string StdOutFilePath
        {
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _jobTaskBuilder.Append($" -o {value}");
                }
            }
        }

        /// <summary>
        /// Task work directory
        /// </summary>
        public string WorkDirectory
        {
            get { return _taskParameters.WorkDirectory; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _jobTaskBuilder.Append($" -D {value}");
                }
            }
        }

        /// <summary>
        /// Task allocated time
        /// Note: Using used resources for calculation (Running time)
        /// </summary>
        public double AllocatedTime
        {
            get { return _taskParameters.RunTime.TotalSeconds; }
        }

        /// <summary>
        /// Task row paramters
        /// </summary>
        public Dictionary<string, string> AllParameters
        {
            get { return _taskParameters.SchedulerResponseParameters; }
        }

        /// <summary>
        /// Method: Set requested resources for task
        /// </summary>
        /// <param name="requestedNodeGroups">Node group names</param>
        /// <param name="requiredNodes">Node names</param>
        /// <param name="placementPolicy">Specify placement policy (on same rack, etc.)</param>
        /// <param name="paralizationSpecs">Task paralization specifications</param>
        /// <param name="minCores">Task min cores</param>
        /// <param name="maxCores">Task max cores</param>
        /// <param name="coresPerNode">Cores per node</param>
        public void SetRequestedResourceNumber(ICollection<string> requestedNodeGroups, ICollection<string> requiredNodes, string placementPolicy, ICollection<TaskParalizationSpecification> paralizationSpecs, int minCores, int maxCores, int coresPerNode)
        {
            var allocationCmdBuilder = new StringBuilder();
            string reqNodeGroupsCmd = PrepareNameOfNodesGroup(requestedNodeGroups);

            int nodeCount = maxCores / coresPerNode;
            nodeCount += maxCores % coresPerNode > 0 ? 1 : 0;

            TaskParalizationSpecification parSpec = paralizationSpecs.FirstOrDefault();
            allocationCmdBuilder.Append($" --nodes={nodeCount}{PrepareNameOfNodes(requiredNodes, nodeCount)}{reqNodeGroupsCmd}");

            if (parSpec is not null)
            {
                allocationCmdBuilder.Append(parSpec.MPIProcesses.HasValue  ? $" --ntasks-per-node={parSpec.MPIProcesses.Value}" : string.Empty);
                allocationCmdBuilder.Append(parSpec.OpenMPThreads.HasValue ? $" --cpus-per-task={parSpec.OpenMPThreads.Value}" : string.Empty);
            }

            allocationCmdBuilder.Append(string.IsNullOrEmpty(placementPolicy) ? string.Empty : $" --constraint={placementPolicy}");
            _jobTaskBuilder.Append(allocationCmdBuilder);
        }

        /// <summary>
        /// Method: Prepare name of node group
        /// </summary>
        /// <param name="requestedNodeGroups">Node group names</param>
        /// <returns></returns>
        private static string PrepareNameOfNodesGroup(ICollection<string> requestedNodeGroups)
        {
            if (requestedNodeGroups?.Count > 0)
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
        /// Method: Prepare name of nodes
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

        /// <summary>
        /// Method: Set enviroment variables for task
        /// </summary>
        /// <param name="variables"></param>
        public void SetEnvironmentVariablesToTask(ICollection<EnvironmentVariable> variables)
        {
            if (variables?.Count > 0)
            {
                var builder = new StringBuilder(" --export ");
                foreach (EnvironmentVariable variable in variables)
                {
                    builder.Append($"{variable.Name} = {variable.Value},");
                }
                builder.Remove(builder.Length - 1, 1);
                _jobTaskBuilder.Append(builder);
            }
        }

        /// <summary>
        /// Method: Set preparation command for task
        /// </summary>
        /// <param name="workDir">Task work dir</param>
        /// <param name="preparationScript">Task preparation script</param>
        /// <param name="commandLine">Task command</param>
        /// <param name="stdOutFile">Standard output file</param>
        /// <param name="stdErrFile">Standard error file</param>
        public void SetPreparationAndCommand(string workDir, string preparationScript, string commandLine, string stdOutFile, string stdErrFile, string recursiveSymlinkCommand)
        {
            _jobTaskBuilder.Append($" --wrap \'cd {workDir};");
            _jobTaskBuilder.Append(
                string.IsNullOrEmpty(recursiveSymlinkCommand) 
                    ? string.Empty 
                    : recursiveSymlinkCommand.Last().Equals(';') ? recursiveSymlinkCommand : $"{recursiveSymlinkCommand};");
            _jobTaskBuilder.Append($"rm {stdOutFile} {stdErrFile};");
            _jobTaskBuilder.Append(
                string.IsNullOrEmpty(preparationScript)
                    ? string.Empty
                    : preparationScript.Last().Equals(';') ? preparationScript : $"{preparationScript};");
            _jobTaskBuilder.Append(
                string.IsNullOrEmpty(commandLine)
                    ? string.Empty
                    : commandLine.Last().Equals(';') ? commandLine : $"{commandLine};");
            _jobTaskBuilder.Append('\'');
        }
        #endregion
    }
}
