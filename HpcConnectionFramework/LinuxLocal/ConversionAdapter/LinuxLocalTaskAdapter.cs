using System;
using System.Collections.Generic;
using System.Text;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.ConversionAdapter;
using HEAppE.HpcConnectionFramework.LinuxPbs;
using HEAppE.HpcConnectionFramework.LinuxPbs.v10.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.LinuxLocal.ConversionAdapter {
	public class LinuxLocalTaskAdapter : ISchedulerTaskAdapter {
		#region Constructors

        public LinuxLocalTaskAdapter(object taskSource)
        {

        }
		#endregion

		#region ISchedulerTaskAdapter Members

        public string ErrorMessage { get; }

        public string Id
		{
			get
			{
				string result;
				/*if (qstatInfo.TryGetValue(LinuxPbsJobInfoAttributes.JOB_ID, out result))
					return result;*/

				return "0";
			}
		}

        public TaskPriority Priority { get; set; }
        public string Queue { get; set; }
        public string JobArrays { get; set; }
        public ICollection<TaskDependency> DependsOn { get; set; }
        public bool IsExclusive { get; set; }
        public bool IsRerunnable { get; set; }
        public int Runtime { get; set; }
        public string StdErrFilePath { get; set; }
        public string StdInFilePath { get; set; }
        public string StdOutFilePath { get; set; }
        public string WorkDirectory { get; set; }
        public double AllocatedTime { get; }
        public bool CpuHyperThreading { get; set; }
        public Dictionary<string, string> AllParameters { get; }

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
        public string Name { get; set; }

        public TaskState State {
			get {
				/*string result;
				if (qstatInfo.TryGetValue(LinuxPbsJobInfoAttributes.JOB_STATE, out result)) {
					string exitStatus;
					qstatInfo.TryGetValue(LinuxPbsJobInfoAttributes.EXIT_STATUS, out exitStatus);
					return ConvertPbsTaskStateToIndependentTaskState(result, exitStatus);
				}*/
				return TaskState.Finished;
			}
		}

        public DateTime? StartTime { get; }

        public DateTime? EndTime {
			get {
				/*if (State == TaskState.Canceled || State == TaskState.Failed || State == TaskState.Finished) {
					string result;
					if (qstatInfo.TryGetValue(LinuxPbsJobInfoAttributes.MTIME, out result))
						return LinuxPbsConversionUtils.ConvertQstatDateStringToDateTime(result);
				}*/
				return null;
			}
		}
		#endregion

		#region Local Methods
		protected virtual TaskState ConvertPbsTaskStateToIndependentTaskState(string taskState, string exitStatus) {

		//#error Merge: Zeptat se 
			//if (taskState == "W" || taskState == "H")
			//return TaskState.Submitted;
			if (taskState == "W")
				return TaskState.Submitted;
			if (taskState == "Q" || taskState == "T" || taskState == "H")
				return TaskState.Queued;
			if (taskState == "R" || taskState == "U" || taskState == "S" || taskState == "E" || taskState == "B")
				return TaskState.Running;
			if (taskState == "F" || taskState == "X") {
				if (!string.IsNullOrEmpty(exitStatus)) {
					int exitStatusInt = Convert.ToInt32(exitStatus);
					if (exitStatusInt == 0)
						return TaskState.Finished;
					if (exitStatusInt > 0 && exitStatusInt < 256) {
						return TaskState.Failed;
					}
					if (exitStatusInt >= 256) {
						return TaskState.Canceled;
					}
				}
				return TaskState.Canceled;
			}
			throw new ApplicationException("Task state \"" + taskState +
			                               "\" could not be converted to any known task state.");
		}
		#endregion
	}
}