using System;
using System.Text;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.LinuxPbs;
using HEAppE.HpcConnectionFramework.LinuxPbs.v10.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.LinuxLocal.ConversionAdapter {
	public class LinuxLocalTaskAdapter : LinuxPbsV10TaskAdapter {
		#region Constructors
		public LinuxLocalTaskAdapter(object taskSource) : base(taskSource) {}
		#endregion

		#region ISchedulerTaskAdapter Members
		public override string Id
		{
			get
			{
				string result;
				if (qstatInfo.TryGetValue(LinuxPbsJobInfoAttributes.JOB_ID, out result))
					return result;

				return "0";
			}
		}

		public override TaskState State {
			get {
				string result;
				if (qstatInfo.TryGetValue(LinuxPbsJobInfoAttributes.JOB_STATE, out result)) {
					string exitStatus;
					qstatInfo.TryGetValue(LinuxPbsJobInfoAttributes.EXIT_STATUS, out exitStatus);
					return ConvertPbsTaskStateToIndependentTaskState(result, exitStatus);
				}
				return TaskState.Finished;
			}
		}

		public override DateTime? EndTime {
			get {
				if (State == TaskState.Canceled || State == TaskState.Failed || State == TaskState.Finished) {
					string result;
					if (qstatInfo.TryGetValue(LinuxPbsJobInfoAttributes.MTIME, out result))
						return LinuxPbsConversionUtils.ConvertQstatDateStringToDateTime(result);
				}
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