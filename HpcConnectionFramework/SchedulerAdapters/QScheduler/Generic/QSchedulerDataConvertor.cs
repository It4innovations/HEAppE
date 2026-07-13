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

    private DateTime? ParseDateTime(JToken token)
    {
        if (token == null || token.Type == JTokenType.Null)
            return null;

        if (token.Type == JTokenType.Date)
            return token.Value<DateTime>().ToUniversalTime();

        var str = token.ToString();
        if (string.IsNullOrWhiteSpace(str))
            return null;

        if (DateTime.TryParse(str, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
        {
            return dt.ToUniversalTime();
        }

        if (double.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var num))
        {
            try
            {
                if (num > 1e11) // Milliseconds
                {
                    return DateTimeOffset.FromUnixTimeMilliseconds((long)num).UtcDateTime;
                }
                else // Seconds
                {
                    return DateTimeOffset.FromUnixTimeSeconds((long)num).UtcDateTime;
                }
            }
            catch
            {
                // ignore and fall back
            }
        }

        return null;
    }

    private string FormatJValue(JToken token)
    {
        if (token.Type == JTokenType.Float)
        {
            return token.Value<double>().ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        if (token.Type == JTokenType.Integer)
        {
            return token.Value<long>().ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        if (token.Type == JTokenType.Date)
        {
            return token.Value<DateTime>().ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }
        return token.ToString().Replace(" ", "_");
    }

    private string GetAllParametersString(JObject obj)
    {
        var list = new List<string>();
        foreach (var prop in obj.Properties())
        {
            if (prop.Value.Type == JTokenType.Object && prop.Value is JObject subObj)
            {
                foreach (var subProp in subObj.Properties())
                {
                    if (subProp.Value.Type != JTokenType.Object && subProp.Value.Type != JTokenType.Array)
                    {
                        var val = FormatJValue(subProp.Value);
                        list.Add($"{prop.Name}.{subProp.Name}={val}");
                    }
                }
            }
            else if (prop.Value.Type != JTokenType.Object && prop.Value.Type != JTokenType.Array)
            {
                var val = FormatJValue(prop.Value);
                list.Add($"{prop.Name}={val}");
            }
        }
        return string.Join(" ", list);
    }


    public override IEnumerable<SubmittedTaskInfo> ReadParametersFromResponse(Cluster cluster, object responseMessage)
    {
        if (responseMessage is not string jsonString || string.IsNullOrWhiteSpace(jsonString))
            return Enumerable.Empty<SubmittedTaskInfo>();

        try
        {
            var json = JObject.Parse(jsonString);
            var stateStr = json.Value<string>("state");
            
            var taskState = stateStr?.ToLower() switch
            {
                "waiting" => TaskState.Queued,
                "running" => TaskState.Running,
                "started" => TaskState.Running,
                "finished" => TaskState.Finished,
                "failed" => TaskState.Failed,
                "error" => TaskState.Failed,
                "cancelled" => TaskState.Canceled,
                _ => TaskState.Unknown
            };

            var errorMsg = json.Value<string>("error");

            var startTime = ParseDateTime(json["start_time"] ?? json["started_at"] ?? json["started"] ?? json["startTime"]);
            var endTime = ParseDateTime(json["end_time"] ?? json["finished_at"] ?? json["finished"] ?? json["endTime"]);

            double? allocatedTime = null;
            var allocatedTimeToken = json["allocated_time"] ?? json["allocatedTime"] ?? json["qpu_seconds"] ?? json["billable_time_ms"];
            if (allocatedTimeToken != null && allocatedTimeToken.Type != JTokenType.Null)
            {
                try
                {
                    allocatedTime = allocatedTimeToken.Value<double>();
                }
                catch
                {
                    var strVal = allocatedTimeToken.ToString().Replace(',', '.');
                    if (double.TryParse(strVal, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var atVal))
                    {
                        allocatedTime = atVal;
                    }
                }
            }

            var allParamsStr = GetAllParametersString(json);

            var taskInfo = new SubmittedTaskInfo
            {
                State = taskState,
                ErrorMessage = errorMsg,
                StartTime = startTime,
                EndTime = endTime,
                AllocatedTime = allocatedTime,
                AllParameters = allParamsStr
            };

            return new List<SubmittedTaskInfo> { taskInfo };
        }
        catch (Exception ex)
        {
            throw new FormatException("Unable to parse task status from QScheduler response: " + jsonString, ex);
        }
    }
}

