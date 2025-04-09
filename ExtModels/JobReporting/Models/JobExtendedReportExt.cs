using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.ExtModels.JobManagement.Models;

namespace HEAppE.ExtModels.JobReporting.Models;

/// <summary>
/// Job extended report ext
/// </summary>
[DataContract(Name = "JobExtendedReportExt")]
[Description("Job extended report ext")]
public class JobExtendedReportExt
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
    /// List of task extended reports
    /// </summary>
    [DataMember]
    [Description("List of task extended reports")]
    public List<TaskExtendedReportExt> Tasks { get; set; }
}