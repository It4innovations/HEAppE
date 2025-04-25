using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.ExtModels.JobManagement.Models;

namespace HEAppE.ExtModels.JobReporting.Models;

/// <summary>
/// Task extended report ext
/// </summary>
[DataContract(Name = "TaskExtendedReportExt")]
[Description("Task extended report ext")]
public class TaskExtendedReportExt
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember]
    [Description("Id")]
    public long Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    [DataMember]
    [Description("Name")]
    public string Name { get; set; }

    /// <summary>
    /// State
    /// </summary>
    [DataMember]
    [Description("State")]
    public TaskStateExt State { get; set; }

    /// <summary>
    /// Start time
    /// </summary>
    [DataMember]
    [Description("Start time")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// End time
    /// </summary>
    [DataMember]
    [Description("End time")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Command template id
    /// </summary>
    [DataMember]
    [Description("Command template id")]
    public long CommandTemplateId { get; set; }

    /// <summary>
    /// Usage
    /// </summary>
    [DataMember]
    [Description("Usage")]
    public double? Usage { get; set; }
}