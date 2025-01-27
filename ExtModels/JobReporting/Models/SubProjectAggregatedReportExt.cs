using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models;

/// <summary>
/// Sub project aggregated report ext
/// </summary>
[DataContract(Name = "SubProjectReportExt")]
[Description("Sub project aggregated report ext")]
public class SubProjectAggregatedReportExt
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
    /// Total usage
    /// </summary>
    [Description("Total usage")]
    public double? TotalUsage { get; set; }

    /// <summary>
    /// List of cluster aggregated reports
    /// </summary>
    [DataMember]
    [Description("List of cluster aggregated reports")]
    public List<ClusterAggregatedReportExt> Clusters { get; set; }
}