using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.ExtModels.ClusterInformation.Models;

namespace HEAppE.ExtModels.JobManagement.Models;

/// <summary>
/// Submitted task info ext
/// </summary>
[DataContract(Name = "SubmittedTaskInfoExt")]
[Description("Submitted task info ext")]
public class SubmittedTaskInfoExt
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

    public override string ToString()
    {
        return $"SubmittedTaskInfoExt(id={Id}; name={Name}; state={State}; priority={Priority}; allocatedTime={AllocatedTime}; allocatedCoreIds={AllocatedCoreIds}; startTime={StartTime}; endTime={EndTime}; nodeType={NodeType}; errorMessage={ErrorMessage})";
    }
}