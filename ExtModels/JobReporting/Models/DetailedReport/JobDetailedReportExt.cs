using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.ExtModels.JobManagement.Models;

namespace HEAppE.ExtModels.JobReporting.Models.DetailedReport;

/// <summary>
/// Job detailed report ext
/// </summary>
[DataContract(Name = "JobDetailedReportExt")]
[Description("Job detailed report ext")]
public class JobDetailedReportExt
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
    public JobStateExt State { get; set; }

    /// <summary>
    /// Creation time
    /// </summary>
    [DataMember]
    [Description("Creation time")]
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// Submit time
    /// </summary>
    [DataMember]
    [Description("Submit time")]
    public DateTime? SubmitTime { get; set; }

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
    /// Submitter
    /// </summary>
    [DataMember]
    [Description("Submitter")]
    public string Submitter { get; set; }

    /// <summary>
    /// List of task detailed reports
    /// </summary>
    [DataMember]
    [Description("List of task detailed reports")]
    public List<TaskDetailedReportExt> Tasks { get; set; }
}