using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;

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
        public override SubmittedTaskInfo ConvertTaskToTaskInfo(object responseMessage)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Read job parameters
        /// </summary>
        /// <param name="responseMessage">Server response text</param>
        /// <returns></returns>
        public override IEnumerable<string> GetJobIds(string responseMessage)
        {
            var scheduledJobIds = new List<string>();
            foreach (Match match in Regex.Matches(responseMessage, @"(?<JobId>.+)", RegexOptions.IgnoreCase | RegexOptions.Compiled))
            {
                if (match.Success && match.Groups.Count == 3)
                {
                    scheduledJobIds.Add(match.Groups.GetValueOrDefault("JobId").Value);
                }
            }

            return scheduledJobIds.Any() ? scheduledJobIds : throw new FormatException("Unable to parse response from HPC scheduler!");
        }

        public override IEnumerable<SubmittedTaskInfo> ReadParametersFromResponse(object responseMessage)
        {
            string response = (string)responseMessage;
            var jobSubmitedTasksInfo = new List<ISchedulerJobInfo>();
            foreach (Match match in Regex.Matches(response, @"(?<jobParameters>.*)\n", RegexOptions.IgnoreCase | RegexOptions.Compiled))
            {
                if (match.Success && match.Groups.Count == 2)
                {
                    string jobResponseMessage = match.Groups.GetValueOrDefault("jobParameters").Value;
                    //For each HPC scheduler job
                    var parameters = Regex.Matches(jobResponseMessage, @"\s?(?<Key>.[^=]*)=(?<Value>.[^\s]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled)
                                        .Where(w => w.Success && w.Groups.Count == 3)
                                        .Select(s => new
                                        {
                                            Key = s.Groups[1].Value.Trim(),
                                            Value = (s.Groups[2].Value is "(null)" or "N/A" or "Unknown" ? string.Empty : s.Groups[2].Value.Trim())
                                        })
                                        .Distinct();

                    var schedulerResultObj = new PbsProJobInfo(jobResponseMessage);
                    FillingSchedulerJobResultObjectFromSchedulerAttribute(schedulerResultObj, parameters.ToDictionary(i => i.Key, j => j.Value));
                    //TODO JobArrays
                    jobSubmitedTasksInfo.Add(schedulerResultObj);
                }
            }

            //return jobSubmitedTasksInfo.Any() ? jobSubmitedTasksInfo : throw new FormatException("Unable to parse response from HPC scheduler!");
            return null;
        }




        protected virtual TaskState ConvertPbsTaskStateToIndependentTaskState(string taskState, string exitStatus)
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
        }

        public static List<string> ConvertNodesUrlsToList(string result)
        {
            List<string> nodesUrls = new List<string>();
            if (!string.IsNullOrEmpty(result))
            {
                string[] lines = result.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string nodeId = lines[i].Trim();
                    if (nodeId != null && nodeId != "")
                        nodesUrls.Add(nodeId);
                }
            }
            return nodesUrls;
        }

        #endregion
    }
}