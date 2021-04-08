using System;
using System.Collections.Generic;
using HaaSMiddleware.HpcConnectionFramework.ConversionAdapter;
using Microsoft.Hpc.Scheduler;
using Microsoft.Hpc.Scheduler.Properties;
using JobState = HaaSMiddleware.DomainObjects.JobManagement.JobInformation.JobState;

namespace HaaSMiddleware.HpcConnectionFramework.WindowsHpc.ConversionAdapter {
	public class WindowsHpcJobAdapter : ISchedulerJobAdapter {
		#region Constructors
		public WindowsHpcJobAdapter(object jobSource) {
			this.jobSource = (ISchedulerJob) jobSource;
			this.allParameters = WindowsHpcConversionUtils.GetAllPropertiesFromSource(jobSource);
		}
		#endregion

		#region ISchedulerJobAdapter Members
		public bool IsExclusive {
			get { return jobSource.IsExclusive; }
			set { jobSource.IsExclusive = value; }
		}

		public object Source {
			get { return jobSource; }
		}

		public int Id {
			get { return Convert.ToInt32(jobSource.Id); }
		}

		public string Name {
			get { return jobSource.Name; }
			set { jobSource.Name = value; }
		}

		public int Priority {
			get { return (jobSource.ExpandedPriority*8)/4000; }
			set { jobSource.ExpandedPriority = (4000/8)*value; }
		}

		public string Project {
			get { return jobSource.Project; }
			set { jobSource.Project = value; }
		}

		public JobState State {
			get { return ConvertMsJobStateToIndependentJobState(jobSource.State); }
		}

		public DateTime CreateTime {
			get { return jobSource.CreateTime; }
		}

		public DateTime? SubmitTime {
			get { return WindowsHpcConversionUtils.GetNullableDateTime(jobSource.SubmitTime); }
		}

		public DateTime? StartTime {
			get { return WindowsHpcConversionUtils.GetNullableDateTime(jobSource.StartTime); }
		}

		public DateTime? EndTime {
			get { return WindowsHpcConversionUtils.GetNullableDateTime(jobSource.EndTime); }
		}

		public string[] RequestedNodeGroups {
			get { return WindowsHpcConversionUtils.ConvertMsStringCollectionToStringNameArray(jobSource.NodeGroups); }
			set { jobSource.NodeGroups = WindowsHpcConversionUtils.ConvertStringNameArrayToMsStringCollection(value); }
		}

		public int Runtime {
			get { return jobSource.Runtime; }
			set { jobSource.Runtime = value; }
		}

		public string Queue {
			get {
				// Different queues are not supported by Windows HPC
				return string.Empty;
			}
			set {
				// Different queues are not supported by Windows HPC
			}
		}

		public string AccountingString {
			get {
				// Accounting strings are not supported by Windows HPC
				return string.Empty;
			}
			set {
				// Accounting strings are not supported by Windows HPC
			}
		}

		public Dictionary<string, string> AllParameters {
			get { return allParameters; }
		}

		public List<object> GetTaskList() {
			ISchedulerCollection taskSources = jobSource.GetTaskList(null, null, false);
			List<object> tasks = new List<object>(taskSources.Count);
			foreach (ISchedulerTask taskSource in taskSources) {
				tasks.Add(taskSource);
			}
			return tasks;
		}

		public void SetRequestedResourceNumber(int minCores, int maxCores) {
			jobSource.AutoCalculateMin = false;
			jobSource.AutoCalculateMax = false;
			jobSource.UnitType = JobUnitType.Core;
			jobSource.MinimumNumberOfCores = minCores;
			jobSource.MaximumNumberOfCores = maxCores;
		}

		public object CreateEmptyTaskObject() {
			return jobSource.CreateTask();
		}

		public void SetNotifications(string mailAddress, bool? notifyOnStart, bool? notifyOnCompletion, bool? notifyOnFailure) {
			jobSource.EmailAddress = mailAddress;
			jobSource.NotifyOnStart = notifyOnStart ?? false;
			jobSource.NotifyOnCompletion = (notifyOnCompletion ?? false) || (notifyOnFailure ?? false);
		}

		public void SetTasks(List<object> tasks) {
			foreach (object task in tasks)
				jobSource.AddTask((ISchedulerTask) task);
		}
		#endregion

		#region Local Methods
		private JobState ConvertMsJobStateToIndependentJobState(Microsoft.Hpc.Scheduler.Properties.JobState jobState) {
			switch (jobState) {
				case Microsoft.Hpc.Scheduler.Properties.JobState.Configuring:
					return JobState.Configuring;
				case Microsoft.Hpc.Scheduler.Properties.JobState.Submitted:
				case Microsoft.Hpc.Scheduler.Properties.JobState.Validating:
				case Microsoft.Hpc.Scheduler.Properties.JobState.ExternalValidation:
					return JobState.Submitted;
				case Microsoft.Hpc.Scheduler.Properties.JobState.Queued:
					return JobState.Queued;
				case Microsoft.Hpc.Scheduler.Properties.JobState.Running:
				case Microsoft.Hpc.Scheduler.Properties.JobState.Finishing:
				case Microsoft.Hpc.Scheduler.Properties.JobState.Canceling:
					return JobState.Running;
				case Microsoft.Hpc.Scheduler.Properties.JobState.Finished:
					return JobState.Finished;
				case Microsoft.Hpc.Scheduler.Properties.JobState.Failed:
					return JobState.Failed;
				case Microsoft.Hpc.Scheduler.Properties.JobState.Canceled:
					return JobState.Canceled;
				default:
					throw new ApplicationException("Job state \"" + jobState +
					                               "\" could not be converted to any known job state.");
			}
		}
		#endregion

		#region Instance Fields
		private readonly Dictionary<string, string> allParameters;
		private readonly ISchedulerJob jobSource;
		#endregion
	}
}