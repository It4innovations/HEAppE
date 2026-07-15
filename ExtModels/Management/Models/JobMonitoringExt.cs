using System;
using System.Collections.Generic;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// A single job entry returned by the admin job monitoring endpoint.
/// Contains all key identifiers and timing fields needed for operational oversight.
/// Tasks are included as a flat list to avoid N+1 queries.
/// </summary>
public class JobMonitoringExt
{
    /// <summary>Database identity of the submitted job.</summary>
    public long Id { get; set; }

    /// <summary>User-defined name of the job.</summary>
    public string Name { get; set; }

    /// <summary>Current state of the job (lowercase string representation).</summary>
    public string State { get; set; }

    /// <summary>Username of the HEAppE user who submitted the job.</summary>
    public string SubmittedBy { get; set; }

    /// <summary>Name of the project this job belongs to.</summary>
    public string Project { get; set; }

    /// <summary>Name of the cluster where the job runs.</summary>
    public string Cluster { get; set; }

    /// <summary>UTC timestamp when the job was created in HEAppE.</summary>
    public DateTime CreationTime { get; set; }

    /// <summary>UTC timestamp when the job was submitted to the scheduler. Null until submitted.</summary>
    public DateTime? SubmitTime { get; set; }

    /// <summary>UTC timestamp when the job started executing. Null until started.</summary>
    public DateTime? StartTime { get; set; }

    /// <summary>UTC timestamp when the job finished. Null until finished.</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>Total wall-clock seconds allocated by the scheduler. Null until scheduler assigns resources.</summary>
    public double? TotalAllocatedTime { get; set; }

    // Job specification details
    public int? WaitingLimit { get; set; }
    public string NotificationEmail { get; set; }
    public string PhoneNumber { get; set; }
    public bool? NotifyOnAbort { get; set; }
    public bool? NotifyOnFinish { get; set; }
    public bool? NotifyOnStart { get; set; }
    public string Reservation { get; set; }

    /// <summary>All tasks belonging to this job.</summary>
    public List<JobMonitoringTaskExt> Tasks { get; set; } = new();
}

/// <summary>
/// Summary of a single task within a monitored job.
/// </summary>
public class JobMonitoringTaskExt
{
    /// <summary>Database identity of the task.</summary>
    public long Id { get; set; }

    /// <summary>User-defined name of the task.</summary>
    public string Name { get; set; }

    /// <summary>Current state of the task (lowercase).</summary>
    public string State { get; set; }

    /// <summary>Scheduler-assigned job ID (e.g. Slurm job ID).</summary>
    public string ScheduledJobId { get; set; }

    /// <summary>Number of CPU cores allocated to this task. Null until scheduled.</summary>
    public int? AllocatedCores { get; set; }

    /// <summary>Number of GPUs allocated to this task. Null until scheduled.</summary>
    public int? AllocatedGpus { get; set; }

    /// <summary>Wall-clock seconds allocated by the scheduler. Null until scheduled.</summary>
    public double? AllocatedTime { get; set; }

    /// <summary>UTC timestamp when the task started. Null until started.</summary>
    public DateTime? StartTime { get; set; }

    /// <summary>UTC timestamp when the task finished. Null until finished.</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>Most recent error or reason message from the scheduler. Null when healthy.</summary>
    public string ErrorMessage { get; set; }

    // Task details, settings, parameters, and environment variables
    public string Priority { get; set; }
    public string Reason { get; set; }
    public string AllParameters { get; set; }
    public int? MinCores { get; set; }
    public int? MaxCores { get; set; }
    public int? WalltimeLimit { get; set; }
    public long? Memory { get; set; }
    public long? MemoryPerCPU { get; set; }
    public long? MemoryPerGPU { get; set; }
    public bool IsExclusive { get; set; }
    public bool IsRerunnable { get; set; }
    public string StandardInputFile { get; set; }
    public string StandardOutputFile { get; set; }
    public string StandardErrorFile { get; set; }
    public string LocalDirectory { get; set; }
    public string ClusterTaskSubdirectory { get; set; }
    public bool? CpuHyperThreading { get; set; }
    public long? CommandTemplateId { get; set; }
    public string CommandTemplateName { get; set; }
    public List<HEAppE.ExtModels.ClusterInformation.Models.CommandTemplateParameterValueExt> CommandParameterValues { get; set; } = new();
    public List<HEAppE.ExtModels.JobManagement.Models.EnvironmentVariableExt> EnvironmentVariables { get; set; } = new();
}

/// <summary>
/// Paginated response wrapper returned by GET /heappe/Management/GetJobsMonitoring.
/// Use NextCursorId with the next request to retrieve the following page.
/// </summary>
public class JobMonitoringPageExt
{
    /// <summary>Jobs on this page, ordered by descending Id.</summary>
    public List<JobMonitoringExt> Jobs { get; set; } = new();

    /// <summary>
    /// The Id to pass as <c>lastJobId</c> on the next request to continue pagination.
    /// Null when this is the last page.
    /// </summary>
    public long? NextCursorId { get; set; }

    /// <summary>Number of jobs returned on this page.</summary>
    public int Count { get; set; }
}
