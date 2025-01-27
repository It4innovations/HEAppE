using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models;

/// <summary>
/// Job report ext
/// </summary>
[DataContract(Name = "JobReportExt")]
[Description("Job report ext")]
public class JobReportExt
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
    /// Sub project
    /// </summary>
    [DataMember]
    [Description("Sub project")]
    public string SubProject { get; set; }

    /// <summary>
    /// List of task reports
    /// </summary>
    [DataMember]
    [Description("List of task reports")]
    public List<TaskReportExt> Tasks { get; set; }
}