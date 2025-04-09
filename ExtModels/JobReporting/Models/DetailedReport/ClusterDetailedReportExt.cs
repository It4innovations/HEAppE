using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models.DetailedReport;

/// <summary>
/// Cluster detail report ext
/// </summary>
[Description("Cluster detail report ext")]
public class ClusterDetailedReportExt
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember]
    [Description("Id")]
    public long Id { get; set; }

    /// <summary>
    /// Total usage
    /// </summary>
    [DataMember]
    [Description("Total usage")]
    public double? TotalUsage { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    [DataMember]
    [Description("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [DataMember]
    [Description("Description")]
    public string Description { get; set; }

    /// <summary>
    /// List of cluster node type detailed reports
    /// </summary>
    [DataMember]
    [Description("List of cluster node type detailed reports")]
    public List<ClusterNodeTypeDetailedReportExt> ClusterNodeTypes { get; set; }
}