using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Enums;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.DTO;

/// <summary>
///     Slurm job info
/// </summary>
public class SlurmJobInfo : ISchedulerJobInfo
{
    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="schedulerResponseParameters"></param>
    public SlurmJobInfo(string schedulerResponseParameters, Dictionary<string, string> parsedParameters)
    {
        SchedulerResponseParameters = schedulerResponseParameters;
        ParsedParameters = parsedParameters;
    }

    #endregion

    #region Instances

    /// <summary>
    ///     Job array id
    /// </summary>
    private string _arrayJobId;
    
    /// <summary>
    /// Job execution Reason
    /// </summary>
    private string _reason;    

    #endregion

    #region Properties

    public Dictionary<string, string> ParsedParameters { get; set; }

    /// <summary>
    ///     Job scheduled id
    /// </summary>
    [Scheduler("JobId")]
    public string SchedulerJobId { get; set; }

    /// <summary>
    ///     Job Name
    /// </summary>
    [Scheduler("JobName")]
    public string Name { get; set; }

    /// <summary>
    ///     Job priority
    /// </summary>
    [Scheduler("Priority")]
    public long Priority { get; set; }

    /// <summary>
    ///     Job requeue
    /// </summary>
    [Scheduler("Requeue")]
    public bool Requeue { get; set; }

    /// <summary>
    ///     Job queue name
    /// </summary>
    [Scheduler("Partition")]
    public string QueueName { get; set; }

    /// <summary>
    ///     Task state name
    /// </summary>
    [Scheduler("JobState")]
    public string StateName
    {
        set => TaskState = MappingTaskState(value).Map();
    }

    /// <summary>
    ///     Job task state
    /// </summary>
    public TaskState TaskState { get; private set; }

    /// <summary>
    ///     Job creation time
    /// </summary>
    [Scheduler("SubmitTime")]
    public DateTime CreationTime { get; set; }

    /// <summary>
    ///     Job submission time
    /// </summary>
    [Scheduler("SubmitTime")]
    public DateTime SubmitTime { get; set; }

    /// <summary>
    ///     Job start time
    /// </summary>
    [Scheduler("StartTime")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    ///     Job end time
    /// </summary>
    [Scheduler("EndTime")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    ///     Job allocated time (requirement)
    /// </summary>
    [Scheduler("TimeLimit")]
    public TimeSpan AllocatedTime { get; set; }

    /// <summary>
    ///     Job run time
    /// </summary>
    [Scheduler("RunTime")]
    public TimeSpan RunTime { get; set; }

    /// <summary>
    ///     Job run number of cores
    /// </summary>
    /// Note: Not supported yet
    public int? UsedCores { get; set; }

    /// <summary>
    ///     Job allocated nodes
    /// </summary>
    [Scheduler("NodeList")]
    public string AllocatedNodesSplit
    {
        set => AllocatedNodes = Mapper.GetAllocatedNodes(value);
    }

    /// <summary>
    ///     Job allocated nodes
    /// </summary>
    public IEnumerable<string> AllocatedNodes { get; private set; }

    /// <summary>
    ///     Job scheduler response raw data
    /// </summary>
    public string SchedulerResponseParameters { get; }

    /// <summary>
    ///     Determinates if job is deadlocked
    /// </summary>
    public bool IsDeadLock { get; private set; }
    
    /// <summary>
    ///     Job execution Reason
    /// </summary>
    [Scheduler("Reason")]
    public string Reason
    {
        get => _reason;
        set
        {
            _reason = value;
            if (!string.IsNullOrEmpty(value) && value.Contains("DependencyNeverSatisfied"))
                IsDeadLock = true;
        }
    }

    #region Job Arrays Properties

    /// <summary>
    ///     Is job with job arrays
    /// </summary>
    public bool IsJobArrayJob { get; private set; }

    /// <summary>
    ///     Array job Id (only for job arrray)
    /// </summary>
    [Scheduler("ArrayJobId")]
    public string ArrayJobId
    {
        get => _arrayJobId;
        set
        {
            if (value is not null)
            {
                _arrayJobId = value;

                IsJobArrayJob = true;
                AggregateSchedulerResponseParameters =
                    $"{HPCConnectionFrameworkConfiguration.JobArrayDbDelimiter}\n{SchedulerResponseParameters}";
            }
        }
    }

    /// <summary>
    ///     Aggregate job scheduler raw response for data
    /// </summary>
    public string AggregateSchedulerResponseParameters { get; private set; }

    #endregion

    #endregion

    #region Local Methods

    /// <summary>
    ///     Combine two jobs with job arrays parameter
    /// </summary>
    /// <param name="jobInfo">Job info</param>
    public void CombineJobs(SlurmJobInfo jobInfo)
    {
        StartTime = StartTime.HasValue && jobInfo.StartTime.HasValue && StartTime > jobInfo.StartTime
            ? jobInfo.StartTime
            : StartTime;
        EndTime = EndTime.HasValue && jobInfo.EndTime.HasValue && EndTime < jobInfo.EndTime ? jobInfo.EndTime : EndTime;

        if (UsedCores.HasValue && jobInfo.UsedCores.HasValue && UsedCores != jobInfo.UsedCores)
        {
            var normalizedRunTime = jobInfo.UsedCores.Value * jobInfo.RunTime.TotalSeconds / UsedCores.Value;
            RunTime += TimeSpan.FromSeconds(Math.Round(normalizedRunTime, 3));
        }
        else
        {
            RunTime += jobInfo.RunTime;
        }

        if (TaskState != jobInfo.TaskState && TaskState <= TaskState.Finished
                                           && ((jobInfo.TaskState > TaskState.Queued &&
                                                jobInfo.TaskState != TaskState.Finished) ||
                                               (TaskState == TaskState.Finished &&
                                                jobInfo.TaskState == TaskState.Queued)))
            TaskState = jobInfo.TaskState;

        AllocatedNodes = jobInfo.AllocatedNodes.Union(AllocatedNodes);
        AggregateSchedulerResponseParameters +=
            $"\n{HPCConnectionFrameworkConfiguration.JobArrayDbDelimiter}\n{jobInfo.SchedulerResponseParameters}";
    }

    /// <summary>
    ///     Method: Mapping task state from text representation of state
    /// </summary>
    /// <param name="state">Task state</param>
    /// <returns></returns>
    private static SlurmTaskState MappingTaskState(string state)
    {
        state = state.Replace("_", string.Empty)
            .Trim()
            .ToLower();
        return Enum.TryParse(state, true, out SlurmTaskState taskState)
            ? taskState
            : SlurmTaskState.Failed;
    }

    #endregion
}