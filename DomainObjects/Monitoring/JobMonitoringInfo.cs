using System;
using System.Collections.Generic;

namespace HEAppE.DomainObjects.Monitoring;

public class JobMonitoringInfo
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string State { get; set; }
    public string SubmittedBy { get; set; }
    public string Project { get; set; }
    public string Cluster { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? SubmitTime { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double? TotalAllocatedTime { get; set; }
    public List<JobMonitoringTaskInfo> Tasks { get; set; } = new();
}

public class JobMonitoringTaskInfo
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string State { get; set; }
    public string ScheduledJobId { get; set; }
    public int? AllocatedCores { get; set; }
    public int? AllocatedGpus { get; set; }
    public double? AllocatedTime { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string ErrorMessage { get; set; }
}

public class JobMonitoringPage
{
    public List<JobMonitoringInfo> Jobs { get; set; } = new();
    public long? NextCursorId { get; set; }
    public int Count { get; set; }
}
