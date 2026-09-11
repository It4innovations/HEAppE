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

    // Job specification details
    public int? WaitingLimit { get; set; }
    public string NotificationEmail { get; set; }
    public string PhoneNumber { get; set; }
    public bool? NotifyOnAbort { get; set; }
    public bool? NotifyOnFinish { get; set; }
    public bool? NotifyOnStart { get; set; }
    public string Reservation { get; set; }

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
    
    public List<CommandParameterValueInfo> CommandParameterValues { get; set; } = new();
    public List<EnvironmentVariableInfo> EnvironmentVariables { get; set; } = new();
}

public class CommandParameterValueInfo
{
    public string Identifier { get; set; }
    public string Value { get; set; }
}

public class EnvironmentVariableInfo
{
    public string Name { get; set; }
    public string Value { get; set; }
}

public class JobMonitoringPage
{
    public List<JobMonitoringInfo> Jobs { get; set; } = new();
    public long? NextCursorId { get; set; }
    public int Count { get; set; }
}
