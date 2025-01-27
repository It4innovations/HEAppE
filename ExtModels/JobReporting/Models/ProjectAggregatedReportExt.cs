using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models;

/// <summary>
/// Project aggregated report ext
/// </summary>
[DataContract(Name = "ProjectReportExt")]
[Description("Project aggregated report ext")]
public class ProjectAggregatedReportExt
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
    /// Usage type
    /// </summary>
    [DataMember]
    [Description("Usage type")]
    public UsageTypeExt UsageType { get; set; }

    /// <summary>
    /// List of sub project aggregated reports
    /// </summary>
    [DataMember]
    [Description("List of sub project aggregated reports")]
    public List<SubProjectAggregatedReportExt> SubProjects { get; set; }

    /// <summary>
    /// List of cluster aggregated reports
    /// </summary>
    [Description("List of cluster aggregated reports")]
    public List<ClusterAggregatedReportExt> Clusters { get; set; }
}