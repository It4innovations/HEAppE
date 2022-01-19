using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.HpcConnectionFramework.ConversionAdapter;
using HEAppE.MiddlewareUtils;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic
{
    public class PbsProDataConvertor : SchedulerDataConvertor
    {
        #region Constructors
        public PbsProDataConvertor(ConversionAdapterFactory conversionAdapterFactory) : base(conversionAdapterFactory) 
        {
        }
        #endregion
        #region SchedulerDataConvertor Members
        protected override List<object> CreateTasks(JobSpecification jobSpecification, ISchedulerJobAdapter jobAdapter)
        {
#warning Only jobs with one task are supported, multiple jobs should be implemented to support multiple tasks for a job (Job Arrays are not suitable for this purpose because they need to have the same parameters for all tasks)
            if (jobSpecification.Tasks != null && jobSpecification.Tasks.Count > 0)
            {
                return new List<object> {
                    ConvertTaskSpecificationToTask(jobSpecification, jobSpecification.Tasks.FirstOrDefault(), jobAdapter.Source)
                };
            }
            return new List<object>();
        }

        protected override string CreateCommandLineForTask(CommandTemplate template, TaskSpecification taskSpecification,
            JobSpecification jobSpecification, Dictionary<string, string> additionalParameters)
        {
#warning workDir is not using
            string workDir = ".";
            string jobClusterDirectory = FileSystemUtils.GetJobClusterDirectoryPath(jobSpecification.Cluster.LocalBasepath,
                jobSpecification);
            if ((jobSpecification.Tasks != null) && (jobSpecification.Tasks.Count > 0))
                workDir = FileSystemUtils.GetTaskClusterDirectoryPath(jobClusterDirectory, taskSpecification.ClusterTaskSubdirectory);
            return base.CreateCommandLineForTask(template, taskSpecification, jobSpecification, additionalParameters);
        }

        protected override string ConvertJobName(JobSpecification jobSpecification)
        {
            string result = Regex.Replace(jobSpecification.Name, @"\W+", "_");
            return result.Substring(0, (result.Length > 15) ? 15 : result.Length);
        }

        protected override string ConvertTaskName(string taskName, JobSpecification jobSpecification)
        {
            string result = Regex.Replace(taskName, @"\W+", "_");
            return result.Substring(0, (result.Length > 15) ? 15 : result.Length);
        }
        #endregion
    }
}