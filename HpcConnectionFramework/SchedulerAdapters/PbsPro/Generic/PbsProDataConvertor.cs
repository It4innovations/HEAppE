using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.DTO;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic
{
    /// <summary>
    /// PBS Professional data convertor
    /// </summary>
    public class PbsProDataConvertor : SchedulerDataConvertor
    {
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="conversionAdapterFactory">Conversion adapter factory</param>
        public PbsProDataConvertor(ConversionAdapterFactory conversionAdapterFactory) : base(conversionAdapterFactory)
        {

        }
        #endregion
        #region SchedulerDataConvertor Members
        /// <summary>
        /// Read actual queue status from scheduler
        /// </summary>
        /// <param name="nodeType">Cluster node type</param>
        /// <param name="responseMessage">Scheduler response message</param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public override ClusterNodeUsage ReadQueueActualInformation(ClusterNodeType nodeType, object responseMessage)
        {
            string response = (string)responseMessage;
            var queueInfo = new PbsProQueueInfo();

            var parameters = Regex.Matches(response, @"(?<Key>[^\s].*)( = |: )(?<Value>.*)", RegexOptions.Compiled)
                                    .Where(w => w.Success)
                                    .Select(s => new
                                        {
                                            Key = s.Groups.GetValueOrDefault("Key").Value,
                                            Value = s.Groups.GetValueOrDefault("Value").Value,
                                        });


            if(!parameters.Any())
            {
                throw new FormatException("Unable to parse response from PBS Professional HPC scheduler!");
            }

            FillingSchedulerJobResultObjectFromSchedulerAttribute(nodeType.Cluster, queueInfo, parameters.ToDictionary(i => i.Key, j => j.Value));
            return new ClusterNodeUsage
            {
                NodeType = nodeType,
                NodesUsed = queueInfo.NodesUsed,
                Priority = queueInfo.Priority,
                TotalJobs = queueInfo.TotalJobs
            };
        }

        /// <summary>
        /// Read job parameters from scheduler
        /// </summary>
        /// <param name="responseMessage">Scheduler response message</param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public override IEnumerable<string> GetJobIds(string responseMessage)
        {
            var scheduledJobIds = Regex.Matches(responseMessage, @"(?<JobId>.+)\n", RegexOptions.Compiled)
                                            .Where(w => w.Success)
                                            .Select(s => s.Groups.GetValueOrDefault("JobId").Value)
                                            .ToList();

            return scheduledJobIds.Any() ? scheduledJobIds : throw new FormatException("Unable to parse response from PBS Professional HPC scheduler!");
        }

        /// <summary>
        /// Convert HPC task information from IScheduler job information object
        /// </summary>
        /// <param name="jobInfo">Scheduler job information</param>
        /// <returns></returns>
        public override SubmittedTaskInfo ConvertTaskToTaskInfo(ISchedulerJobInfo jobInfo)
        {
            PbsProJobInfo obj = (PbsProJobInfo)jobInfo;
            return new SubmittedTaskInfo()
            {
                ScheduledJobId = obj.SchedulerJobId,
                Name = obj.Name,
                StartTime = obj.StartTime,
                EndTime = obj.StartTime.HasValue && obj.TaskState >= TaskState.Finished ? obj.StartTime.Value.AddSeconds(obj.RunTime.TotalSeconds) : null,
                AllocatedTime = Math.Round(obj.RunTime.TotalSeconds, 3),
                AllocatedCores = obj.UsedCores,
                State = obj.TaskState,
                TaskAllocationNodes = obj.AllocatedNodes?.Select(s => new SubmittedTaskAllocationNodeInfo() { AllocationNodeId = s, SubmittedTaskInfoId = long.Parse(obj.Name) })
                                                          .ToList(),
                ErrorMessage = default,
                AllParameters = obj.IsJobArrayJob ? obj.AggregateSchedulerResponseParameters : obj.SchedulerResponseParameters
            };
        }

        /// <summary>
        /// Read job parameters from scheduler
        /// </summary>
        /// <param name="cluster">Cluster</param>
        /// <param name="responseMessage">Scheduler response message</param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public override IEnumerable<SubmittedTaskInfo> ReadParametersFromResponse(Cluster cluster, object responseMessage)
        {
            string response = (string)responseMessage;
            var jobSubmitedTasksInfo = new List<SubmittedTaskInfo>();
            PbsProJobInfo aggregateResultObj = null;

            var jobResponseMessages = Regex.Split(response, @"\n\n", RegexOptions.Compiled)
                                              .Where(w=>!string.IsNullOrEmpty(w))
                                              .Select(s => s.Replace("\n\t", string.Empty))
                                              .ToList();
            foreach (string jobResponseMessage in jobResponseMessages)
            {
                //For each HPC scheduler job
                var parameters = Regex.Matches(jobResponseMessage, @"(?<Key>[^\s].*)( = |: )(?<Value>.*)", RegexOptions.Compiled)
                                        .Where(w => w.Success)
                                        .Select(s => new
                                            {
                                                Key = s.Groups.GetValueOrDefault("Key").Value,
                                                Value = (s.Groups.GetValueOrDefault("Value").Value is "(null)" or "N/A" or "Unknown" ? string.Empty : s.Groups.GetValueOrDefault("Value").Value)
                                            })
                                        .Distinct();

                var schedulerResultObj = new PbsProJobInfo(jobResponseMessage);
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

                if (aggregateResultObj.SchedulerJobIdWoJobArrayIndex == schedulerResultObj.SchedulerJobIdWoJobArrayIndex)
                {
                    aggregateResultObj.CombineJobs(schedulerResultObj);
                    continue;
                }

                jobSubmitedTasksInfo.Add(ConvertTaskToTaskInfo(aggregateResultObj));
                aggregateResultObj = schedulerResultObj;
            }

            if (aggregateResultObj is not null)
            {
                jobSubmitedTasksInfo.Add(ConvertTaskToTaskInfo(aggregateResultObj));
            }

            return jobSubmitedTasksInfo.Any() ? jobSubmitedTasksInfo : throw new FormatException("Unable to parse response from PBS Professional HPC scheduler!");
        }
        #endregion
    }
}