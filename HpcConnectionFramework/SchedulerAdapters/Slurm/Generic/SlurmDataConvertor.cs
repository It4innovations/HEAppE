﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.Exceptions.Internal;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.DTO;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic;

/// <summary>
///     Slurm data convertor
/// </summary>
public class SlurmDataConvertor : SchedulerDataConvertor
{
    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="conversionAdapterFactory">Conversion adapter factory</param>
    public SlurmDataConvertor(ConversionAdapterFactory conversionAdapterFactory) : base(conversionAdapterFactory)
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
    /// <exception cref="SlurmException"></exception>
    public override ClusterNodeUsage ReadQueueActualInformation(ClusterNodeType nodeType, object responseMessage)
    {
        var nodesUsed = 0;
        var response = (string)responseMessage;

        var parsedNodeUsedLine = string.IsNullOrEmpty(nodeType.ClusterAllocationName)
            ? response
            : response.Replace($"CLUSTER: {nodeType.ClusterAllocationName}\n", string.Empty);

        parsedNodeUsedLine = Regex.Replace(parsedNodeUsedLine, @"[ ]|[\n]{2}", string.Empty);
        if (!string.IsNullOrEmpty(parsedNodeUsedLine))
            if (!int.TryParse(parsedNodeUsedLine, out nodesUsed))
                throw new SlurmException("UnableToParseNodeUsage")
                {
                    CommandError = null
                };

        return new ClusterNodeUsage
        {
            NodeType = nodeType,
            NodesUsed = nodesUsed,
            Priority = default,
            TotalJobs = default
        };
    }

    /// <summary>
    ///     Read job parameters from scheduler
    /// </summary>
    /// <param name="responseMessage">Scheduler response message</param>
    /// <returns></returns>
    /// <exception cref="SlurmException"></exception>
    public override IEnumerable<string> GetJobIds(string responseMessage)
    {
        var scheduledJobIds = Regex.Matches(responseMessage, @"(Submitted batch job[\s\t]+)(?<JobId>[0-9]+)",
                RegexOptions.Compiled)
            .Where(w => w.Success)
            .Select(s => s.Groups.GetValueOrDefault("JobId").Value)
            .ToList();

        return scheduledJobIds.Any()
            ? scheduledJobIds
            : throw new SlurmException("UnableToParseResponse")
            {
                CommandError = null
            };
    }

    /// <summary>
    ///     Convert HPC task information from IScheduler job information object
    /// </summary>
    /// <param name="jobInfo">Scheduler job information</param>
    /// <returns></returns>
    public override SubmittedTaskInfo ConvertTaskToTaskInfo(ISchedulerJobInfo jobInfo)
    {
        var obj = (SlurmJobInfo)jobInfo;
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
    /// <exception cref="SlurmException"></exception>
    public override IEnumerable<SubmittedTaskInfo> ReadParametersFromResponse(Cluster cluster, object responseMessage)
    {
        var response = ((string)responseMessage).Replace("\n\t", string.Empty)
            .Replace("\n  ", string.Empty);
        var jobSubmitedTasksInfo = new List<SubmittedTaskInfo>();
        SlurmJobInfo aggregateResultObj = null;

        var jobResponseMessages = Regex.Split(response, @"(?<jobParameters>.*)\n", RegexOptions.Compiled)
            .Where(w => !string.IsNullOrEmpty(w))
            .ToList();

        foreach (var jobResponseMessage in jobResponseMessages)
        {
            //For each HPC scheduler job
            var parameters = Regex
                .Matches(jobResponseMessage, @"\s?(?<Key>.[^=]*)=(?<Value>[^\s]*)", RegexOptions.Compiled)
                .Where(w => w.Success)
                .Select(s => new
                {
                    Key = s.Groups.GetValueOrDefault("Key").Value,
                    Value = s.Groups.GetValueOrDefault("Value").Value is "(null)" or "N/A" or "Unknown"
                        ? string.Empty
                        : s.Groups.GetValueOrDefault("Value").Value
                })
                .Distinct();

            var parsedParameters = parameters.ToDictionary(i => i.Key, j => j.Value);
            var schedulerResultObj = new SlurmJobInfo(jobResponseMessage, parsedParameters);

            FillingSchedulerJobResultObjectFromSchedulerAttribute(cluster, schedulerResultObj, parsedParameters);

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

            jobSubmitedTasksInfo.Add(ConvertTaskToTaskInfo(schedulerResultObj));
            aggregateResultObj = schedulerResultObj;
        }


        if (aggregateResultObj is not null) jobSubmitedTasksInfo.Add(ConvertTaskToTaskInfo(aggregateResultObj));

        return jobSubmitedTasksInfo.Any()
            ? jobSubmitedTasksInfo
            : throw new SlurmException("UnableToParseResponse")
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
        var jobAdapter = _conversionAdapterFactory.CreateJobAdapter();
        jobAdapter.SetNotifications(jobSpecification.NotificationEmail, jobSpecification.NotifyOnStart,
            jobSpecification.NotifyOnFinish, jobSpecification.NotifyOnAbort);
        // Setting global parameters for all tasks
        var globalJobParameters = (string)jobAdapter.AllocationCmd;
        var tasks = new List<object>();
        if (jobSpecification.Tasks is not null && jobSpecification.Tasks.Any())
            foreach (var task in jobSpecification.Tasks)
                tasks.Add(
                    $"_{task.Id}=$({(string)ConvertTaskSpecificationToTask(jobSpecification, task, schedulerAllocationCmd)}{globalJobParameters} 2>&1); if [ $? -gt 0 ]; then echo $_{task.Id}; exit 0; else echo $_{task.Id};_{task.Id}_parsed=$(set -- $_{task.Id};echo $4);fi;");

        jobAdapter.SetTasks(tasks);
        return jobAdapter.AllocationCmd;
    }

    #endregion
}