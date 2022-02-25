using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic
{
    /// <summary>
    /// Slurm data convertor
    /// </summary>
    public class SlurmDataConvertor : SchedulerDataConvertor
    {
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="conversionAdapterFactory">Conversion adapter factory</param>
        public SlurmDataConvertor(ConversionAdapterFactory conversionAdapterFactory) : base(conversionAdapterFactory)
        {
        }
        #endregion
        #region Local Members
        /// <summary>
        /// Read actual queue status
        /// </summary>
        /// <param name="responseMessage">Server response text</param>
        /// <param name="nodeType">NodeType</param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public override ClusterNodeUsage ReadQueueActualInformation(object responseMessage, ClusterNodeType nodeType)
        {
            string response = (string)responseMessage;

            string parsedNodeUsedLine = Regex.Replace(response, @"[ ]|[\n]{2}", string.Empty);
            if (!int.TryParse(parsedNodeUsedLine, out int nodesUsed))
            {
                throw new FormatException("Unable to parse cluster node usage from HPC scheduler!");
            }

            return new ClusterNodeUsage
            {
                NodeType = nodeType,
                NodesUsed = nodesUsed,
                Priority = default,
                TotalJobs = default
            };
        }

        /// <summary>
        /// Get job ids after submission
        /// </summary>
        /// <param name="responseMessage">Server response text</param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public override IEnumerable<string> GetJobIds(string responseMessage)
        {
            var scheduledJobIds = new List<string>();
            foreach (Match match in Regex.Matches(responseMessage, @"(Submitted batch job[\s\t]+)(?<JobId>[0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled))
            {
                if (match.Success && match.Groups.Count == 3)
                {
                    scheduledJobIds.Add(match.Groups.GetValueOrDefault("JobId").Value);
                }
            }

            return scheduledJobIds.Any() ? scheduledJobIds : throw new FormatException("Unable to parse response from HPC scheduler!");
        }

        /// <summary>
        /// Convert HPC task information from DTO object
        /// </summary>
        /// <param name="jobInfo">HPC Job Info</param>
        /// <returns></returns>
        public override SubmittedTaskInfo ConvertTaskToTaskInfo(ISchedulerJobInfo jobInfo)
        {
            SlurmJobInfo obj = (SlurmJobInfo)jobInfo;
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
        /// <param name="cluster">Cluster</param>
        /// <param name="responseMessage">Server response text</param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public override IEnumerable<SubmittedTaskInfo> ReadParametersFromResponse(Cluster cluster, object responseMessage)
        {
            string response = (string)responseMessage;
            var jobSubmitedTasksInfo = new List<SubmittedTaskInfo>();
            SlurmJobInfo aggregateResultObj = null;

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

                    var schedulerResultObj = new SlurmJobInfo(jobResponseMessage);
                    FillingSchedulerJobResultObjectFromSchedulerAttribute(cluster, schedulerResultObj, parameters.ToDictionary(i => i.Key, j => j.Value));

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

                    if (aggregateResultObj.ArrayJobId == schedulerResultObj.ArrayJobId)
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
