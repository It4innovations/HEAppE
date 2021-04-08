using System;
using System.Collections.Generic;
using System.Text;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.LinuxPbs.v10.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.LinuxPbs.v12.ConversionAdapter {
	public class LinuxPbsV12JobAdapter : LinuxPbsV10JobAdapter {
		#region Constructors
		public LinuxPbsV12JobAdapter(object jobSource) : base(jobSource) {}
		#endregion

		#region ISchedulerJobAdapter Members
		public override string Project {
			get {
				string result;
				if (qstatInfo.TryGetValue(LinuxPbsJobInfoAttributes.PROJECT, out result))
					return result;
				return string.Empty;
			}
			set {
				if (!string.IsNullOrEmpty(value))
					jobSource += " -P \"" + value + "\"";
			}
		}

		public override JobState State {
			get {
				string result;
				if (qstatInfo.TryGetValue(LinuxPbsJobInfoAttributes.JOB_STATE, out result)) {
					string exitStatus;
					qstatInfo.TryGetValue(LinuxPbsJobInfoAttributes.EXIT_STATUS, out exitStatus);
					return ConvertPbsJobStateToIndependentJobState(result, exitStatus);
				}
				return JobState.Finished;
			}
		}

		public override DateTime? EndTime {
			get {
				if (State == JobState.Canceled || State == JobState.Failed || State == JobState.Finished) {
					string result;
					if (qstatInfo.TryGetValue(LinuxPbsJobInfoAttributes.MTIME, out result))
						return LinuxPbsConversionUtils.ConvertQstatDateStringToDateTime(result);
				}
				return null;
			}
		}

		public override void SetTasks(List<object> tasks)
		{
			StringBuilder builder = new StringBuilder("");
			foreach (var task in tasks)
			{
				builder.Append((string)task);
			}

			jobSource = builder.ToString();
		}
		#endregion

		#region Local Methods
		protected JobState ConvertPbsJobStateToIndependentJobState(string jobState, string exitStatus) {
			if (jobState == "H")
				return JobState.Configuring;
			if (jobState == "W")
				return JobState.Submitted;
			if (jobState == "Q" || jobState == "T")
				return JobState.Queued;
			if (jobState == "R" || jobState == "U" || jobState == "S" || jobState == "E" || jobState == "B")
				return JobState.Running;
			if (jobState == "F") {
				if (!string.IsNullOrEmpty(exitStatus)) {
					int exitStatusInt = Convert.ToInt32(exitStatus);
					if (exitStatusInt == 0)
						return JobState.Finished;
					if (exitStatusInt > 0 && exitStatusInt < 256) {
						return JobState.Failed;
					}
					if (exitStatusInt >= 256) {
						return JobState.Canceled;
					}
				}
				return JobState.Canceled;
			}
			throw new ApplicationException("Job state \"" + jobState +
			                               "\" could not be converted to any known job state.");
		}
		#endregion
	}
}