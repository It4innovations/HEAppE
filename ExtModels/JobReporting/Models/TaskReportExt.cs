using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models;

/// <summary>
/// Task report ext
/// </summary>
[DataContract(Name = "TaskReportExt")]
[Description("Task report ext")]
public class TaskReportExt
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
    /// Usage
    /// </summary>
    [DataMember]
    [Description("Usage")]
    public double? Usage { get; set; }
}