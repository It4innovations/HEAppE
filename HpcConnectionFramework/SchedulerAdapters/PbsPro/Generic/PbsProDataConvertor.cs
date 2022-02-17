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
            PbsProJobInfo obj = (PbsProJobInfo)responseMessage;
            return new SubmittedTaskInfo()
            {
                ScheduledJobId = obj.SchedulerJobId,
                Name = obj.Name,
                StartTime = obj.StartTime,
                EndTime = obj.EndTime,
                AllocatedTime = obj.RunTime.TotalSeconds,
                State = obj.TaskState,
                TaskAllocationNodes = obj.AllocatedNodes?.Select(s => new SubmittedTaskAllocationNodeInfo() { AllocationNodeId = s, SubmittedTaskInfoId = long.Parse(obj.Name) })
                                                          .ToList(),
                ErrorMessage = default,
                AllParameters = obj.IsJobArrayJob ? obj.AggregateSchedulerResponseParameters : obj.SchedulerResponseParameters
            };
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
            //TODO Regex check!
            string response = (string)responseMessage;
            var jobSubmitedTasksInfo = new List<SubmittedTaskInfo>();
            PbsProJobInfo aggregateResultObj = null;

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

                    if (!schedulerResultObj.IsJobArrayJob)
                    {
                        jobSubmitedTasksInfo.Add(ConvertTaskToTaskInfo(schedulerResultObj));
                        continue;
                    }

                    if (aggregateResultObj is null)
                    {
                        aggregateResultObj = schedulerResultObj;
                        continue;
                    }

                    if (aggregateResultObj.SchedulerJobIdWoJobArrayIndex == schedulerResultObj.SchedulerJobIdWoJobArrayIndex)
                    {
                        aggregateResultObj.CombineJobs(schedulerResultObj);
                        continue;
                    }

                    jobSubmitedTasksInfo.Add(ConvertTaskToTaskInfo(aggregateResultObj));
                    aggregateResultObj = schedulerResultObj;
                }
            }

            if (aggregateResultObj is not null)
            {
                jobSubmitedTasksInfo.Add(ConvertTaskToTaskInfo(aggregateResultObj));
            }

            return jobSubmitedTasksInfo.Any() ? jobSubmitedTasksInfo : throw new FormatException("Unable to parse response from HPC scheduler!");
        }
        #endregion
    }
}