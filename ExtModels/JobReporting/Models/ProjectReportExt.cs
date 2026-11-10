using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models;

/// <summary>
/// Project report ext
/// </summary>
[DataContract(Name = "ProjectReportExt")]
[Description("Project report ext")]
public class ProjectReportExt
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
    /// Accounting string
    /// </summary>
    [DataMember]
    [Description("Accounting string")]
    public string AccountingString { get; set; }

    /// <summary>
    /// Total usage
    /// </summary>
    [DataMember]
    [Description("Total usage")]
    public double? TotalUsage { get; set; }

    /// <summary>
    /// Usage of QScheduler sessions (included in the total usage)
    /// </summary>
    [DataMember]
    [Description("Usage of QScheduler sessions (included in the total usage)")]
    public double QSchedulerSessionsUsage { get; set; }

    /// <summary>
    /// QScheduler sessions in the report period
    /// </summary>
    [DataMember]
    [Description("QScheduler sessions in the report period")]
    public List<QSchedulerSessionReportExt> QSchedulerSessions { get; set; } = new();

    /// <summary>
    /// Usage type
    /// </summary>
    [DataMember]
    [Description("Usage type")]
    public UsageTypeExt UsageType { get; set; }

    /// <summary>
    /// List of cluster reports
    /// </summary>
    [DataMember]
    [Description("List of cluster reports")]
    public List<ClusterReportExt> Clusters { get; set; }
}