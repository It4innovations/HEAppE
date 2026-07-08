using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.QScheduler.Generic;

public class QSchedulerDataConvertor : SchedulerDataConvertor
{
    public QSchedulerDataConvertor(ILogger logger) : base(null, logger)
    {
    }

    public override object ConvertJobSpecificationToJob(JobSpecification jobSpecification, object schedulerAllocationCmd)
    {
        return string.Empty;
    }

    public override object ConvertTaskSpecificationToTask(JobSpecification jobSpecification, TaskSpecification taskSpecification, object schedulerAllocationCmd)
    {
        return string.Empty;
    }

    public override ClusterNodeUsage ReadQueueActualInformation(ClusterNodeType nodeType, object responseMessage)
    {
        throw new NotImplementedException("ReadQueueActualInformation is not implemented for QScheduler");
    }

    public override IEnumerable<string> GetJobIds(string responseMessage)
    {
        var jobIds = new List<string>();
        if (string.IsNullOrWhiteSpace(responseMessage))
            return jobIds;

        try
        {
            var responseStr = responseMessage.Trim();
            if (long.TryParse(responseStr, out long jobId))
            {
                jobIds.Add(jobId.ToString());
            }
            else
            {
                var token = JToken.Parse(responseStr);
                if (token.Type == JTokenType.Integer)
                {
                    jobIds.Add(token.Value<long>().ToString());
                }
                else if (token is JObject obj && obj.TryGetValue("id", out var idVal))
                {
                    jobIds.Add(idVal.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            throw new FormatException("Unable to parse job ID from QScheduler response: " + responseMessage, ex);
        }

        return jobIds;
    }

    public override SubmittedTaskInfo ConvertTaskToTaskInfo(ISchedulerJobInfo jobInfo)
    {
        throw new NotImplementedException("ConvertTaskToTaskInfo is not implemented directly for QScheduler");
    }

    public override IEnumerable<SubmittedTaskInfo> ReadParametersFromResponse(Cluster cluster, object responseMessage)
    {
        if (responseMessage is not string jsonString || string.IsNullOrWhiteSpace(jsonString))
            return Enumerable.Empty<SubmittedTaskInfo>();

        try
        {
            var json = JObject.Parse(jsonString);
            var stateStr = json.Value<string>("state");
            
            var taskState = stateStr switch
            {
                "waiting" => TaskState.Queued,
                "running" => TaskState.Running,
                "finished" => TaskState.Finished,
                "failed" => TaskState.Failed,
                "error" => TaskState.Failed,
                "cancelled" => TaskState.Canceled,
                _ => TaskState.Unknown
            };

            var errorMsg = json.Value<string>("error");

            var taskInfo = new SubmittedTaskInfo
            {
                State = taskState,
                ErrorMessage = errorMsg
            };

            return new List<SubmittedTaskInfo> { taskInfo };
        }
        catch (Exception ex)
        {
            throw new FormatException("Unable to parse task status from QScheduler response: " + jsonString, ex);
        }
    }
}
