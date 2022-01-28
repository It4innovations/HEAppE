﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic.ConversionAdapter
{
    public class PbsProTaskAdapter : ISchedulerTaskAdapter
    {
        #region Constructors
        public PbsProTaskAdapter(object taskSource)
        {
            this.taskSource = (string)taskSource;
            qstatInfo = PbsProConversionUtils.ReadQstatResultFromJobSource(this.taskSource);
        }
        #endregion

        #region ISchedulerTaskAdapter Members
        public object AllocationCmd
        {
            get { return taskSource; }
        }

        public virtual string Id
        {
            get
            {
                string result;
                if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.JOB_ID, out result))
                    return PbsProConversionUtils.GetJobIdFromJobCode(result).ToString();

                return "0";
            }
        }

        public TaskPriority Priority
        {
            get
            {
                string result;
                if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.PRIORITY, out result))
                    return (TaskPriority)Math.Round(((Convert.ToInt32(result) + 1024) * 8) / 2047f);
                return 0;
            }
            set { taskSource += " -p " + ((int)Math.Round((2047 / 8f) * (int)value) - 1024); }
        }

        public string Queue
        {
            get
            {
                string result;
                if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.QUEUE, out result))
                    return result;
                return string.Empty;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    taskSource += " -q " + value;
            }
        }

        public bool CpuHyperThreading
        {
            set
            {
                if (value)
                    taskSource += " -l cpu_hyper_threading=true";
            }
        }

        public string JobArrays
        {
            set
            {
                if (!string.IsNullOrEmpty(value))
                    taskSource += " -J " + value;
            }
        }

        public IEnumerable<string> AllocatedCoreIds
        {
            get
            {
                string result;
                if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.EXEC_HOST, out result))
                {
                    result = result.Replace('*', ' ');
                    string[] allocIds = result.Split('+');
                    return allocIds;
                }
                return new List<string>();
            }
        }

        public string Name
        {
            get
            {
                string result;
                if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.JOB_NAME, out result))
                    return result;
                return string.Empty;
            }
            set { taskSource += " -N " + value; }
        }

        public virtual TaskState State
        {
            get
            {
                string result;
                if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.JOB_STATE, out result))
                {
                    return ConvertPbsTaskStateToIndependentTaskState(result);
                }
                return TaskState.Finished;
            }
        }

        public DateTime? StartTime
        {
            get
            {
                string result;
                if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.STIME, out result))
                    return PbsProConversionUtils.ConvertQstatDateStringToDateTime(result);
                return null;
            }
        }

        /// <summary>EndTime is not supported in the Linux PBS Scheduler v10.</summary>
        public virtual DateTime? EndTime
        {
            get { return null; }
        }

        /// <summary>
        ///   ErrorMessage is not supported in the Linux PBS Scheduler. Error messages for the task are in the task's error
        ///   output file.
        /// </summary>
        public string ErrorMessage
        {
            get { return null; }
        }

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
                    taskSource += builder.ToString();
                }
            }
        }

        public bool IsExclusive
        {
            set
            {
                if (value)
                    taskSource += " -l place=free:excl";
            }
        }

        public bool IsRerunnable
        {
            get
            {
                string result;
                if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.RERUNNABLE, out result))
                    return result == "y";
                return false;
            }
            set
            {
                if (value)
                    taskSource += " -r y";
                else
                    taskSource += " -r n";
            }
        }

        public int Runtime
        {
            get
            {
                string result;
                if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.RESOURCE_LIST_WALLTIME, out result))
                    return Convert.ToInt32(result);
                return 0;
            }
            set
            {
                if (value > 0)
                    taskSource += " -l walltime=" + PbsProConversionUtils.ConvertSecondsToQstatTimeString(value);
            }
        }

        public string StdErrFilePath
        {
            set
            {
                if (!string.IsNullOrEmpty(value))
                    taskSource += " -e " + value;
            }
        }

        /// <summary>Standard input redirection is not supported by Linux PBS scheduler.</summary>
        public string StdInFilePath
        {
            set { }
        }

        public string StdOutFilePath
        {
            set
            {
                if (!string.IsNullOrEmpty(value))
                    taskSource += " -o " + value;
            }
        }

        public string WorkDirectory
        {
            get
            {
                string result;
                if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.JOBDIR, out result))
                    return result;
                return string.Empty;
            }
            ///<summary>Work directory in the PBS scheduler is set to the actual directory from which the qsub command is ran. This means that the working directory has to be changed before calling qsub (in the LinuxPbsSchedulerAdapter.CreateJob method).</summary>
            set
            {
                /*if (!string.IsNullOrEmpty(value))
					taskSource += " -d " + value;*/
            }
        }

        public double AllocatedTime
        {
            get
            {
                string result;
                if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.RESOURCES_USED_CPUT, out result))
                {
                    string[] split = result.Split(':');
                    return Convert.ToInt32(split[0]) * 3600 + Convert.ToInt32(split[1]) * 60 + Convert.ToInt32(split[2]);
                }
                return 0;
            }
        }

        public Dictionary<string, string> AllParameters
        {
            get { return qstatInfo; }
        }

        public void SetRequestedResourceNumber(IEnumerable<string> requestedNodeGroups, ICollection<string> requiredNodes, string placementPolicy, IEnumerable<TaskParalizationSpecification> paralizationSpecs, int minCores, int maxCores, int coresPerNode)
        {
            StringBuilder allocationCmdBuilder = new StringBuilder(" -l select=");

            //For specific node names
            if (requiredNodes?.Count > 0)
            {
                int requiredNodesMaxCores = coresPerNode * requiredNodes.Count;
                int remainingCores = maxCores - requiredNodesMaxCores;
                int cpusPerHost = maxCores > requiredNodesMaxCores
                                                    ? coresPerNode
                                                    : maxCores / requiredNodes.Count;


                List<TaskParalizationSpecification> parSpecsForReqNodes = paralizationSpecs.Where(w => w.MaxCores % coresPerNode == 0
                                                                                                  && w.MaxCores / coresPerNode == 1).ToList();

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
                    allocationCmdBuilder.Append("+");
                    allocationCmdBuilder.Append(GenerateSelectPartForRequestedGroups(requestedNodeGroups, placementPolicy,
                                                  paralizationSpecs.Except(parSpecsForReqNodes).ToList(), remainingCores, coresPerNode));
                }
            }
            else
            {
                allocationCmdBuilder.Append(GenerateSelectPartForRequestedGroups(requestedNodeGroups, placementPolicy, paralizationSpecs, maxCores, coresPerNode));
            }

            taskSource += allocationCmdBuilder.ToString();
        }

        public void SetEnvironmentVariablesToTask(IEnumerable<EnvironmentVariable> variables)
        {
            if (variables != null && variables.Any())
            {
                StringBuilder builder = new StringBuilder(" -v ");
                bool first = true;
                foreach (EnvironmentVariable variable in variables)
                {
                    if (first)
                        first = false;
                    else
                        builder.Append(",");
                    builder.Append(variable.Name);
                    builder.Append("=");
                    builder.Append(variable.Value);
                }
                taskSource += builder.ToString();
            }
        }

        public void SetPreparationAndCommand(string workDir, string preparationScript, string commandLine, string stdOutFile, string stdErrFile, string recursiveSymlinkCommand)
        {
            string nodefileDir = workDir.Substring(0, workDir.LastIndexOf('/'));
            var taskSourceSb = new StringBuilder();
            taskSourceSb.Append($"echo ' cd {nodefileDir}; ~/.key_scripts/nodefile.sh; ");
            taskSourceSb.Append($"cd {workDir}; ");
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
            taskSourceSb.Append($"1>> {stdOutFile} ");
            taskSourceSb.Append($"2>> {stdErrFile} ");
            taskSourceSb.Append($"' | {taskSource}");

            taskSource = taskSourceSb.ToString();
        }
        #endregion

        #region Local Methods
        protected virtual string GenerateSelectPartForRequestedGroups(IEnumerable<string> requestedNodeGroups, string placementPolicy, IEnumerable<TaskParalizationSpecification> paralizationSpecs, int coreCount, int coresPerNode)
        {
            StringBuilder builder = new StringBuilder();
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

        protected virtual TaskState ConvertPbsTaskStateToIndependentTaskState(string taskState)
        {
            if (taskState == "H")
                return TaskState.Configuring;
            if (taskState == "W")
                return TaskState.Submitted;
            if (taskState == "Q" || taskState == "T")
                return TaskState.Queued;
            if (taskState == "R" || taskState == "U" || taskState == "S" || taskState == "E")
                return TaskState.Running;
            throw new ApplicationException("Task state \"" + taskState +
                                           "\" could not be converted to any known task state.");
        }
        #endregion

        #region Instance Fields
        protected string taskSource;
        protected Dictionary<string, string> qstatInfo;
        #endregion
    }
}