using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
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

        public override SubmittedTaskInfo ConvertTaskToTaskInfo(object responseMessage)
        {
            throw new System.NotImplementedException();
        }

        public override IEnumerable<string> GetJobIds(string responseMessage)
        {
            throw new System.NotImplementedException();
        }

        public override IEnumerable<SubmittedTaskInfo> ReadParametersFromResponse(object response)
        {
            throw new System.NotImplementedException();
        }
        #endregion
        #region SchedulerDataConvertor Members
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

        //protected override string ConvertJobName(JobSpecification jobSpecification)
        //{
        //    string result = Regex.Replace(jobSpecification.Name, @"\W+", "_");
        //    return result.Substring(0, (result.Length > 15) ? 15 : result.Length);
        //}

        //protected override string ConvertTaskName(string taskName, JobSpecification jobSpecification)
        //{
        //    string result = Regex.Replace(taskName, @"\W+", "_");
        //    return result.Substring(0, (result.Length > 15) ? 15 : result.Length);
        //}
        #endregion
    }
}