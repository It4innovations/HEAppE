using System;
using System.Collections.Generic;
using System.Text;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.ConversionAdapter;
using HEAppE.HpcConnectionFramework.LinuxPbs;
using HEAppE.HpcConnectionFramework.LinuxPbs.v10.ConversionAdapter;
using Newtonsoft.Json;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.ConversionAdapter
{
    public class LinuxLocalTaskAdapter : ISchedulerTaskAdapter
    {
        private dynamic taskInfo;
        #region Constructors
        public LinuxLocalTaskAdapter(object taskSource)
        {
            taskInfo = JsonConvert.DeserializeObject(taskSource.ToString());
        }
        #endregion

        #region ISchedulerTaskAdapter Members

        public string ErrorMessage { get; }

        public string Id => taskInfo.Id?.ToString() ?? "0";

        public TaskPriority Priority { get; set; }//todo
        public string Queue { get; set; }//todo
        public string JobArrays { get; set; }//todo
        public ICollection<TaskDependency> DependsOn { get; set; }//todo
        public bool IsExclusive { get; set; }//todo
        public bool IsRerunnable { get; set; }//todo
        public int Runtime { get; set; }
        public string StdErrFilePath { get; set; }
        public string StdInFilePath { get; set; }
        public string StdOutFilePath { get; set; }
        public string WorkDirectory { get; set; }
        public double AllocatedTime => taskInfo.AllocatedTime ?? 0;
        public bool CpuHyperThreading { get; set; }
        public Dictionary<string, string> AllParameters => ConvertTaskInfoToDict();



        public void SetRequestedResourceNumber(ICollection<string> requestedNodeGroups, ICollection<string> requiredNodes, string placementPolicy,
            ICollection<TaskParalizationSpecification> paralizationSpecs, int minCores, int maxCores, int coresPerNode)
        {
            throw new NotImplementedException();
        }

        public void SetEnvironmentVariablesToTask(ICollection<EnvironmentVariable> variables)
        {
            throw new NotImplementedException();
        }

        public void SetPreparationAndCommand(string workDir, string preparationScript, string commandLine, string stdOutFile,
            string stdErrFile, string recursiveSymlinkCommand)
        {
            throw new NotImplementedException();
        }

        public object Source { get; }
        public ICollection<string> AllocatedCoreIds { get; }
        public string Name { get => taskInfo.Id?.ToString() ?? "0"; set => taskInfo.Name = value; }

        public TaskState State//todo all states
        {
            get
            {
                switch (taskInfo.State?.ToString())
                {
                    case "H":
                        return TaskState.Configuring;
                    case "R":
                        return TaskState.Running;
                    case "F":
                        return TaskState.Finished;
                    default:
                        throw new ApplicationException("Task state could not be converted to any known task state.");
                }
            }
        }

        public DateTime? StartTime
        {
            get
            {
                if (taskInfo.StartTime == null || string.IsNullOrEmpty(taskInfo.StartTime.ToString()))
                    return null;
                else
                {
                    return taskInfo.StartTime;
                }
            }
        }

        public DateTime? EndTime
        {
            get
            {
                if (taskInfo.EndTime == null || string.IsNullOrEmpty(taskInfo.EndTime.ToString()))
                    return null;
                else
                {
                    return taskInfo.EndTime;
                }
            }
        }
        #endregion

        #region Local Methods
        /*protected virtual TaskState ConvertPbsTaskStateToIndependentTaskState(string taskState, string exitStatus)
        {
            if (taskState == "W")
                return TaskState.Submitted;
            if (taskState == "Q" || taskState == "T" || taskState == "H")
                return TaskState.Queued;
            if (taskState == "R" || taskState == "U" || taskState == "S" || taskState == "E" || taskState == "B")
                return TaskState.Running;
            if (taskState == "F" || taskState == "X")
            {
                if (!string.IsNullOrEmpty(exitStatus))
                {
                    int exitStatusInt = Convert.ToInt32(exitStatus);
                    if (exitStatusInt == 0)
                        return TaskState.Finished;
                    if (exitStatusInt > 0 && exitStatusInt < 256)
                    {
                        return TaskState.Failed;
                    }
                    if (exitStatusInt >= 256)
                    {
                        return TaskState.Canceled;
                    }
                }
                return TaskState.Canceled;
            }
            throw new ApplicationException("Task state \"" + taskState +
                                           "\" could not be converted to any known task state.");
        }*/


        private Dictionary<string, string> ConvertTaskInfoToDict()
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(taskInfo.ToString());
        }
        #endregion
    }
}