using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.ExtModels.ClusterInformation.Models;

namespace HEAppE.ExtModels.JobManagement.Models;

/// <summary>
/// Detailed task info for admin report
/// </summary>
[DataContract(Name = "AdminTaskInfoExt")]
[Description("Detailed task info for admin report")]
public class AdminTaskInfoExt
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id")]
    [Description("Id")]
    public long? Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    [DataMember(Name = "Name")]
    [Description("Name")]
    public string Name { get; set; }

    /// <summary>
    /// State
    /// </summary>
    [DataMember(Name = "State")]
    [Description("State")]
    public TaskStateExt? State { get; set; }

    /// <summary>
    /// Source of state update
    /// </summary>
    [DataMember(Name = "StateSource")]
    [Description("Source of state update")]
    public JobStateSourceExt? StateSource { get; set; }

    /// <summary>
    /// Timestamp of last state update
    /// </summary>
    [DataMember(Name = "StateUpdatedAt")]
    [Description("Timestamp of last state update")]
    public DateTime? StateUpdatedAt { get; set; }

    /// <summary>
    /// Priority
    /// </summary>
    [DataMember(Name = "Priority")]
    [Description("Priority")]
    public TaskPriorityExt? Priority { get; set; }

    /// <summary>
    /// Allocated time
    /// </summary>
    [DataMember(Name = "AllocatedTime")]
    [Description("Allocated time")]
    public double? AllocatedTime { get; set; }

    /// <summary>
    /// Array of allocated core ids
    /// </summary>
    [DataMember(Name = "AllocatedCoreIds")]
    [Description("Array of allocated core ids")]
    public string[] AllocatedCoreIds { get; set; }

    /// <summary>
    /// Start time
    /// </summary>
    [DataMember(Name = "StartTime")]
    [Description("Start time")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// End time
    /// </summary>
    [DataMember(Name = "EndTime")]
    [Description("End time")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Node type
    /// </summary>
    [DataMember(Name = "NodeType")]
    [Description("Node type")]
    public ClusterNodeTypeForTaskExt NodeType { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    [DataMember(Name = "ErrorMessage")]
    [Description("Error message")]
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Cpu hyper threading
    /// </summary>
    [DataMember(Name = "CpuHyperThreading")]
    [Description("Cpu hyper threading")]
    public bool? CpuHyperThreading { get; set; }

    /// <summary>
    /// Reason from scheduler
    /// </summary>
    [DataMember(Name = "Reason")]
    [Description("Reason (parsed from scheduler, e.g. SLURM)")]
    public string Reason { get; set; }

    /// <summary>
    /// Minimum CPU cores
    /// </summary>
    [DataMember(Name = "MinCores")]
    [Description("Minimum CPU cores")]
    public int? MinCores { get; set; }

    /// <summary>
    /// Maximum CPU cores
    /// </summary>
    [DataMember(Name = "MaxCores")]
    [Description("Maximum CPU cores")]
    public int? MaxCores { get; set; }

    /// <summary>
    /// GPU cores
    /// </summary>
    [DataMember(Name = "GpuCores")]
    [Description("GPU cores")]
    public int? GpuCores { get; set; }

    /// <summary>
    /// GPU nodes
    /// </summary>
    [DataMember(Name = "GpuNodes")]
    [Description("GPU nodes")]
    public int? GpuNodes { get; set; }

    /// <summary>
    /// Walltime limit
    /// </summary>
    [DataMember(Name = "WalltimeLimit")]
    [Description("Walltime limit")]
    public int? WalltimeLimit { get; set; }

    /// <summary>
    /// Memory
    /// </summary>
    [DataMember(Name = "Memory")]
    [Description("Memory")]
    public long? Memory { get; set; }

    /// <summary>
    /// Memory per CPU
    /// </summary>
    [DataMember(Name = "MemoryPerCPU")]
    [Description("Memory per CPU")]
    public long? MemoryPerCPU { get; set; }

    /// <summary>
    /// Memory per GPU
    /// </summary>
    [DataMember(Name = "MemoryPerGPU")]
    [Description("Memory per GPU")]
    public long? MemoryPerGPU { get; set; }

    /// <summary>
    /// Placement policy
    /// </summary>
    [DataMember(Name = "PlacementPolicy")]
    [Description("Placement policy")]
    public string PlacementPolicy { get; set; }

    /// <summary>
    /// Quality of service
    /// </summary>
    [DataMember(Name = "QualityOfService")]
    [Description("Quality of service")]
    public string QualityOfService { get; set; }

    /// <summary>
    /// Is exclusive
    /// </summary>
    [DataMember(Name = "IsExclusive")]
    [Description("Is exclusive")]
    public bool? IsExclusive { get; set; }

    /// <summary>
    /// Is rerunnable
    /// </summary>
    [DataMember(Name = "IsRerunnable")]
    [Description("Is rerunnable")]
    public bool? IsRerunnable { get; set; }

    /// <summary>
    /// Job arrays
    /// </summary>
    [DataMember(Name = "JobArrays")]
    [Description("Job arrays")]
    public string JobArrays { get; set; }

    /// <summary>
    /// Standard input file
    /// </summary>
    [DataMember(Name = "StandardInputFile")]
    [Description("Standard input file")]
    public string StandardInputFile { get; set; }

    /// <summary>
    /// Standard output file
    /// </summary>
    [DataMember(Name = "StandardOutputFile")]
    [Description("Standard output file")]
    public string StandardOutputFile { get; set; }

    /// <summary>
    /// Standard error file
    /// </summary>
    [DataMember(Name = "StandardErrorFile")]
    [Description("Standard error file")]
    public string StandardErrorFile { get; set; }

    /// <summary>
    /// Cluster task subdirectory
    /// </summary>
    [DataMember(Name = "ClusterTaskSubdirectory")]
    [Description("Cluster task subdirectory")]
    public string ClusterTaskSubdirectory { get; set; }

    /// <summary>
    /// Command template id
    /// </summary>
    [DataMember(Name = "CommandTemplateId")]
    [Description("Command template id")]
    public long? CommandTemplateId { get; set; }

    /// <summary>
    /// Command template parameter values zadané uživatelem
    /// </summary>
    [DataMember(Name = "TemplateParameterValues")]
    [Description("Command template parameter values submitted by user")]
    public CommandTemplateParameterValueExt[] TemplateParameterValues { get; set; }

    /// <summary>
    /// Proměnné prostředí pro task
    /// </summary>
    [DataMember(Name = "EnvironmentVariables")]
    [Description("Environment variables")]
    public EnvironmentVariableExt[] EnvironmentVariables { get; set; }

    public override string ToString()
    {
        return $"AdminTaskInfoExt(id={Id}; name={Name}; state={State}; priority={Priority}; allocatedTime={AllocatedTime}; minCores={MinCores}; maxCores={MaxCores}; walltimeLimit={WalltimeLimit}; qualityOfService={QualityOfService})";
    }
}
