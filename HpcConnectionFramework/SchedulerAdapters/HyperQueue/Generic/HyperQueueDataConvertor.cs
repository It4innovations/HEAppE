using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.DTO.HyperQueueDTO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.Generic
{
    /// <summary>
    /// HyperQueue data convertor
    /// </summary>
    public class HyperQueueDataConvertor : SchedulerDataConvertor
    {
        public HyperQueueDataConvertor(ConversionAdapterFactory conversionAdapterFactory) : base(conversionAdapterFactory)
        {
        }

        public override ClusterNodeUsage ReadQueueActualInformation(ClusterNodeType nodeType, object responseMessage)
        {
            //not allowed
            throw new NotImplementedException("ReadQueueActualInformation is not implemented for HyperQueue");
        }
        
        /// <summary>
        /// This method return the job id from the response message (FOR HQ ONLY ONE JOB ID IS RETURNED)
        /// </summary>
        /// <param name="responseMessage"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public override IEnumerable<string> GetJobIds(string responseMessage)
        {
            List<string> jobIds = new List<string>();

            try
            {
                JObject jsonObject = JObject.Parse(responseMessage);
                if (jsonObject.TryGetValue("id", out JToken idToken) && idToken.Type == JTokenType.String)
                {
                    jobIds.Add(idToken.Value<string>());
                }
            }
            catch (Exception ex)
            {
                throw new FormatException("Unable to parse job id from HQ server!", ex);
            }

            return jobIds;
        }

        public override SubmittedTaskInfo ConvertTaskToTaskInfo(ISchedulerJobInfo jobInfo)
        {
            var hyperQueueJobInfo = jobInfo as HyperQueueJobInfo;
            if (hyperQueueJobInfo == null)
            {
                throw new ArgumentException("JobInfo is not HyperQueueJobInfo");
            }
            return new SubmittedTaskInfo()
            {
                ScheduledJobId = jobInfo.SchedulerJobId,
                Name = jobInfo.Name,
                StartTime = jobInfo.StartTime,
                EndTime = jobInfo.StartTime.HasValue && jobInfo.TaskState >= TaskState.Finished ? jobInfo.EndTime : null,
                AllocatedTime = Math.Round(jobInfo.RunTime.TotalSeconds, 3),
                AllocatedCores = jobInfo.UsedCores,
                State = jobInfo.TaskState,
                TaskAllocationNodes = jobInfo.AllocatedNodes?.Select(s => new SubmittedTaskAllocationNodeInfo() { AllocationNodeId = s, SubmittedTaskInfoId = long.Parse(jobInfo.Name) })
                    .ToList(),
                ErrorMessage = default,
                AllParameters = jobInfo.SchedulerResponseParameters
            };
        }

        public override IEnumerable<SubmittedTaskInfo> ReadParametersFromResponse(Cluster cluster,
            object responseMessage)
        {
            HyperQueueJobInfo hyperQueueJobWrapper;
            if (responseMessage is not string jsonString) return null;
            try
            {
                if (jsonString == "[]\n")
                {
                    return new List<SubmittedTaskInfo>()
                    {
                        new (){
                            ScheduledJobId = string.Empty,
                            Name = string.Empty,
                            State = TaskState.Failed,
                            ErrorMessage = jsonString
                        }
                    };
                }
                var hyperQueueJobInfo = JsonConvert.DeserializeObject<List<Job>>(jsonString);

                hyperQueueJobWrapper = new HyperQueueJobInfo(hyperQueueJobInfo.First());
                return new List<SubmittedTaskInfo> { ConvertTaskToTaskInfo(hyperQueueJobWrapper) };
            }
            catch(Exception ex)
            {
                throw new FormatException("Unable to parse job id from HQ server!", ex);
            }
        }
        
        public override object ConvertJobSpecificationToJob(JobSpecification jobSpecification, object schedulerAllocationCmd)
        {
            ISchedulerJobAdapter jobAdapter = _conversionAdapterFactory.CreateJobAdapter();

            //jobAdapter.SetNotifications(jobSpecification.NotificationEmail, jobSpecification.NotifyOnStart, jobSpecification.NotifyOnFinish, jobSpecification.NotifyOnAbort);
            // Setting global parameters for all tasks
            var tasks = new List<object>();
            if (jobSpecification.Tasks is not null && jobSpecification.Tasks.Any())
            {
                foreach (var task in jobSpecification.Tasks)
                {
                    var taskAllocationCmd = ConvertTaskSpecificationToTask(jobSpecification, task, schedulerAllocationCmd);
                    
                    tasks.Add(taskAllocationCmd);
                }
            }

            jobAdapter.SetTasks(tasks);
            return jobAdapter.AllocationCmd;
        }
    }
}
