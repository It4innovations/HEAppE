using System;
using System.Collections.Generic;
using HaaSMiddleware.DomainObjects.JobManagement;
using HaaSMiddleware.DomainObjects.JobManagement.JobInformation;
using HaaSMiddleware.HpcConnectionFramework.ConversionAdapter;
using Microsoft.Hpc.Scheduler;

namespace HaaSMiddleware.HpcConnectionFramework.WindowsHpc.ConversionAdapter {
	public class WindowsHpcTaskAdapter : ISchedulerTaskAdapter {
		#region Constructors
		public WindowsHpcTaskAdapter(object taskSource) {
			this.taskSource = (ISchedulerTask) taskSource;
			this.allParameters = WindowsHpcConversionUtils.GetAllPropertiesFromSource(taskSource);
		}
		#endregion

		#region ISchedulerTaskAdapter Members
		public object Source {
			get { return taskSource; }
		}

		public string AllocatedCoreIds {
			get { return taskSource.AllocatedCoreIds; }
		}

		public string Name {
			get { return taskSource.Name; }
			set { taskSource.Name = value; }
		}

		public TaskState State {
			get { return ConvertMsTaskStateToIndependentTaskState(taskSource.State); }
		}

		public DateTime? StartTime {
			get { return WindowsHpcConversionUtils.GetNullableDateTime(taskSource.StartTime); }
		}

		public DateTime? EndTime {
			get { return WindowsHpcConversionUtils.GetNullableDateTime(taskSource.EndTime); }
		}

		/*public string Output
		{
			get { return taskSource.Output; }
		}*/

		public string ErrorMessage {
			get { return taskSource.ErrorMessage; }
		}

		public ICollection<TaskSpecification> DependsOn {
			set { taskSource.DependsOn = WindowsHpcConversionUtils.ConvertTaskDependenciesToMsStringCollection(value); }
		}

		public bool IsExclusive {
			get { return taskSource.IsExclusive; }
			set { taskSource.IsExclusive = value; }
		}

		public bool IsRerunnable {
			get { return taskSource.IsRerunnable; }
			set { taskSource.IsRerunnable = value; }
		}

		public int Runtime {
			get { return taskSource.Runtime; }
			set { taskSource.Runtime = value; }
		}

		public string StdErrFilePath {
			get { return taskSource.StdErrFilePath; }
			set { taskSource.StdErrFilePath = value; }
		}

		public string StdInFilePath {
			get { return taskSource.StdInFilePath; }
			set { taskSource.StdInFilePath = value; }
		}

		public string StdOutFilePath {
			get { return taskSource.StdOutFilePath; }
			set { taskSource.StdOutFilePath = value; }
		}

		public string WorkDirectory {
			get { return taskSource.WorkDirectory; }
			set { taskSource.WorkDirectory = value; }
		}

		public double AllocatedTime {
			get {
				if (StartTime.HasValue) {
					double elapsedTime = (EndTime.HasValue)
						? (EndTime.Value - StartTime.Value).TotalSeconds
						: (DateTime.Now - StartTime.Value).TotalSeconds;
					string[] taskSplit = AllocatedCoreIds.Split(' ');
					return elapsedTime*(taskSplit.Length/2);
				}
				return 0;
			}
		}

		public Dictionary<string, string> AllParameters {
			get { return allParameters; }
		}

		public void SetRequestedResourceNumber(string[] requestedNodeGroups,
			string[] requiredNodes, int minCores, int maxCores, int coresPerNode) {
			taskSource.RequiredNodes = WindowsHpcConversionUtils.ConvertStringNameArrayToMsStringCollection(requiredNodes);
			taskSource.MinimumNumberOfCores = Convert.ToInt32(minCores);
			taskSource.MaximumNumberOfCores = Convert.ToInt32(maxCores);
		}

		public void SetEnvironmentVariablesToTask(ICollection<EnvironmentVariable> variables) {
			if (variables != null) {
				foreach (EnvironmentVariable variable in variables) {
					taskSource.SetEnvironmentVariable(variable.Name, variable.Value);
				}
			}
		}

		public void SetPreparationAndCommand(string workDir, string preparationScript, string commandLine, string stdOutFile, string stdErrFile) {
#warning Preparation Script is not implemented in the Windows HPC Adaptor. This could be done by creating NodePrep tasks for the job, but has to be investigated further
			taskSource.CommandLine = commandLine;
		}
		#endregion

		#region Local Methods
		protected TaskState ConvertMsTaskStateToIndependentTaskState(Microsoft.Hpc.Scheduler.Properties.TaskState taskState) {
			switch (taskState) {
				case Microsoft.Hpc.Scheduler.Properties.TaskState.Configuring:
					return TaskState.Configuring;
				case Microsoft.Hpc.Scheduler.Properties.TaskState.Submitted:
				case Microsoft.Hpc.Scheduler.Properties.TaskState.Validating:
					return TaskState.Submitted;
				case Microsoft.Hpc.Scheduler.Properties.TaskState.Queued:
				case Microsoft.Hpc.Scheduler.Properties.TaskState.Dispatching:
					return TaskState.Queued;
				case Microsoft.Hpc.Scheduler.Properties.TaskState.Running:
				case Microsoft.Hpc.Scheduler.Properties.TaskState.Finishing:
				case Microsoft.Hpc.Scheduler.Properties.TaskState.Canceling:
					return TaskState.Running;
				case Microsoft.Hpc.Scheduler.Properties.TaskState.Finished:
					return TaskState.Finished;
				case Microsoft.Hpc.Scheduler.Properties.TaskState.Failed:
					return TaskState.Failed;
				case Microsoft.Hpc.Scheduler.Properties.TaskState.Canceled:
					return TaskState.Canceled;
				default:
					throw new ApplicationException("Task state \"" + taskState +
					                               "\" could not be converted to any known task state.");
			}
		}
		#endregion

		#region Instance Fields
		private readonly Dictionary<string, string> allParameters;
		private readonly ISchedulerTask taskSource;
		#endregion
	}
}