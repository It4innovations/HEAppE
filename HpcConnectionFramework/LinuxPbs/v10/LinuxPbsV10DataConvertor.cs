using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.HpcConnectionFramework.ConversionAdapter;
using HEAppE.MiddlewareUtils;

namespace HEAppE.HpcConnectionFramework.LinuxPbs.v10 {
	public class LinuxPbsV10DataConvertor : SchedulerDataConvertor {
		#region Constructors
		public LinuxPbsV10DataConvertor(ConversionAdapterFactory conversionAdapterFactory) : base(conversionAdapterFactory) {}
		#endregion

		#region SchedulerDataConvertor Members
		protected override List<object> CreateTasks(JobSpecification jobSpecification, ISchedulerJobAdapter jobAdapter) {
#warning Only jobs with one task are supported, multiple jobs should be implemented to support multiple tasks for a job (Job Arrays are not suitable for this purpose because they need to have the same parameters for all tasks)
			if (jobSpecification.Tasks != null && jobSpecification.Tasks.Count > 0) {
				//object task = jobAdapter.CreateEmptyTaskObject();
				return new List<object> {
					ConvertTaskSpecificationToTask(jobSpecification, jobSpecification.Tasks.FirstOrDefault(), jobAdapter.Source)
				};
			}
			return new List<object>();
		}

		protected override string CreateCommandLineForTask(CommandTemplate template, TaskSpecification taskSpecification,
			JobSpecification jobSpecification, Dictionary<string, string> additionalParameters) {
#warning workDir is not using
            string workDir = ".";
			string jobClusterDirectory = FileSystemUtils.GetJobClusterDirectoryPath(jobSpecification.Cluster.LocalBasepath,
				jobSpecification);
			if ((jobSpecification.Tasks != null) && (jobSpecification.Tasks.Count > 0))
				workDir = FileSystemUtils.GetTaskClusterDirectoryPath(jobClusterDirectory, taskSpecification.ClusterTaskSubdirectory);
			return base.CreateCommandLineForTask(template, taskSpecification, jobSpecification, additionalParameters);
		}

		protected override string ConvertJobName(JobSpecification jobSpecification) {
			/*int internalIdLength = jobSpecification.JobSpecification.InternalId.Length + 1;
      MessageLogger.Log("JobName = " + jobSpecification.JobSpecification.Name + ", Internal ID = " + jobSpecification.JobSpecification.InternalId + ", Length: " + internalIdLength);
			return Regex.Replace(jobSpecification.JobSpecification.Name, @"\s+", "_").Substring(0, 15 - internalIdLength) + "-" + jobSpecification.JobSpecification.InternalId;*/
			string result = Regex.Replace(jobSpecification.Name, @"\W+", "_");
			return result.Substring(0, (result.Length > 15) ? 15 : result.Length);
		}

		protected override string ConvertTaskName(string taskName, JobSpecification jobSpecification) {
			/*int internalIdLength = jobSpecification.JobSpecification.InternalId.Length + 1;
      return Regex.Replace(taskName, @"\s+", "_").Substring(0, 15 - internalIdLength) + "-" + jobSpecification.JobSpecification.InternalId;*/
			string result = Regex.Replace(taskName, @"\W+", "_");
			return result.Substring(0, (result.Length > 15) ? 15 : result.Length);
		}
		#endregion
	}
}