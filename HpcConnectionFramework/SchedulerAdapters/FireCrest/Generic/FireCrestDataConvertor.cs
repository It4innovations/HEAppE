using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.Exceptions.Internal;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.FireCrest.DTO;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.FireCrest.Generic;

/// <summary>
///     FireCrest data convertor
/// </summary>
public class FireCrestDataConvertor : SchedulerDataConvertor
{
    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="conversionAdapterFactory">Conversion adapter factory</param>
    public FireCrestDataConvertor(ConversionAdapterFactory conversionAdapterFactory) : base(conversionAdapterFactory)
    {
    }

    #endregion

    #region ISchedulerAdapter Members

    /// <summary>
    ///     Read actual queue status from scheduler
    /// </summary>
    /// <param name="nodeType">Cluster node type</param>
    /// <param name="responseMessage">Scheduler response message</param>
    /// <returns></returns>
    /// <exception cref="FireCrestException"></exception>
    public override ClusterNodeUsage ReadQueueActualInformation(ClusterNodeType nodeType, object responseMessage)
    {
        var nodesUsed = 0;
        var response = (string)responseMessage;

        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);

        if (string.IsNullOrEmpty(nodeType.ClusterAllocationName))
        {
            if (jsonResponse.TryGetProperty("total_allocated_nodes", out var totalNodes))
            {
                nodesUsed = totalNodes.GetInt32();
            }
        }
        else
        {
            if (jsonResponse.TryGetProperty("partitions", out var partitions) &&
                partitions.ValueKind == JsonValueKind.Array)
            {
                foreach (var partition in partitions.EnumerateArray())
                {
                    if (partition.TryGetProperty("name", out var name) &&
                        name.GetString() == nodeType.ClusterAllocationName &&
                        partition.TryGetProperty("allocated_nodes", out var allocatedNodes))
                    {
                        nodesUsed = allocatedNodes.GetInt32();
                        break;
                    }
                }
            }
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
    /// Reads job parameters from scheduler using a robust text search.
    /// </summary>
    public override IEnumerable<string> GetJobIds(string responseMessage)
    {
        if (string.IsNullOrWhiteSpace(responseMessage))
        {
            throw new FireCrestException("UnableToParseResponse: Response from server was null or empty.")
            {
                CommandError = "The response from the server was empty."
            };
        }

        var match = System.Text.RegularExpressions.Regex.Match(responseMessage, "\"jobId\":\\s*(\\d+)");

        if (match.Success && match.Groups.Count > 1)
        {
            var jobId = match.Groups[1].Value;

            return new List<string> { jobId };
        }

        throw new FireCrestException("UnableToParseResponse")
        {
            CommandError = $"The new Regex-based code is not running. The response was: {responseMessage}"
        };
    }

    /// <summary>
    ///     Convert HPC task information from IScheduler job information object
    /// </summary>
    /// <param name="jobInfo">Scheduler job information</param>
    /// <returns></returns>
    public override SubmittedTaskInfo ConvertTaskToTaskInfo(ISchedulerJobInfo jobInfo)
    {
        var obj = (FireCrestJobInfo)jobInfo;
        return new SubmittedTaskInfo
        {
            ScheduledJobId = obj.SchedulerJobId,
            Name = obj.Name,
            StartTime = obj.StartTime,
            EndTime = obj.StartTime.HasValue && obj.TaskState >= TaskState.Finished ? obj.EndTime : null,
            AllocatedTime = Math.Round(obj.RunTime.TotalSeconds, 3),
            AllocatedCores = obj.UsedCores,
            State = obj.IsDeadLock ? TaskState.Failed : obj.TaskState,
            TaskAllocationNodes = obj.AllocatedNodes?.Select(s => new SubmittedTaskAllocationNodeInfo
                    { AllocationNodeId = s, SubmittedTaskInfoId = long.Parse(obj.Name) })
                .ToList(),
            ErrorMessage = default,
            Reason = obj.Reason,
            AllParameters = obj.IsJobArrayJob
                ? obj.AggregateSchedulerResponseParameters
                : obj.SchedulerResponseParameters,
            ParsedParameters = obj.ParsedParameters
        };
    }

    /// <summary>
    ///     Read job parameters from scheduler
    /// </summary>
    /// <param name="jobSpecification">JobSpecification</param>
    /// <param name="responseMessage">Scheduler response message</param>
    /// <returns></returns>
    /// <exception cref="FireCrestException"></exception>
    public override IEnumerable<SubmittedTaskInfo> ReadParametersFromResponse(Cluster cluster, object responseMessage)
    {
        var response = (string)responseMessage;
        var jobSubmitedTasksInfo = new List<SubmittedTaskInfo>();
        FireCrestJobInfo aggregateResultObj = null;

        // Parse the JSON response from FireCrest
        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);
        var jobsArray = jsonResponse.GetProperty("jobs");

        // Process each job in the response
        foreach (var jobElement in jobsArray.EnumerateArray())
        {
            // Extract job data as string for compatibility with FireCrestJobInfo constructor
            var jobResponseMessage = JsonSerializer.Serialize(jobElement);

            // Convert JSON properties to dictionary of parameters
            var parsedParameters = new Dictionary<string, string>();
            foreach (var property in jobElement.EnumerateObject())
            {
                var value = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.ToString(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => string.Empty,
                    _ => JsonSerializer.Serialize(property.Value)
                };

                // Skip null/N/A/Unknown values
                if (value is "(null)" or "N/A" or "Unknown")
                    value = string.Empty;

                parsedParameters.Add(property.Name, value);
            }

            // Create job info object and fill in details
            var schedulerResultObj = new FireCrestJobInfo(jobResponseMessage, parsedParameters);
            FillingSchedulerJobResultObjectFromSchedulerAttribute(cluster, schedulerResultObj, parsedParameters);

            // Handle job arrays the same way as in the original code
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

        if (aggregateResultObj is not null)
            jobSubmitedTasksInfo.Add(ConvertTaskToTaskInfo(aggregateResultObj));

        return jobSubmitedTasksInfo.Any()
            ? jobSubmitedTasksInfo
            : throw new FireCrestException("UnableToParseResponse")
            {
                CommandError = null
            };
    }

    /// <summary>
    ///     Convert job specification to job
    /// </summary>
    /// <param name="jobSpecification">Job specification</param>
    /// <param name="schedulerAllocationCmd">Scheduler allocation command</param>
    /// <returns></returns>
    public override object ConvertJobSpecificationToJob(JobSpecification jobSpecification,
        object schedulerAllocationCmd)
    {
        // Create job adapter as in original
        var jobAdapter = _conversionAdapterFactory.CreateJobAdapter();

        // Set notifications same as original
        jobAdapter.SetNotifications(jobSpecification.NotificationEmail, jobSpecification.NotifyOnStart,
            jobSpecification.NotifyOnFinish, jobSpecification.NotifyOnAbort);

        // Build the FireCrest API request instead of shell commands
        var jobTasks = new List<object>();

        // Process tasks if they exist
        if (jobSpecification.Tasks != null && jobSpecification.Tasks.Any())
        {
            foreach (var task in jobSpecification.Tasks)
            {
                // Convert each task to FireCrest format
                var taskData = ConvertTaskSpecificationToTask(jobSpecification, task, schedulerAllocationCmd);

                // Add task to collection
                jobTasks.Add(taskData);
            }
        }

        // Set the task data in the adapter
        jobAdapter.SetTasks(jobTasks);

        // Return the final API request payload
        return jobAdapter.AllocationCmd;
    }

    #endregion
}