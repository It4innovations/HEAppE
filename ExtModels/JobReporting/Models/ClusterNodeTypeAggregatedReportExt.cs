using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models;

/// <summary>
/// Cluster node type aggregated report ext
/// </summary>
[DataContract(Name = "ClusterNodeTypeReportExt")]
[Description("Cluster node type aggregated report ext")]
public class ClusterNodeTypeAggregatedReportExt
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
    [Description("Id")]
    public string Name { get; set; }

    /// <summary>
    /// Total usage
    /// </summary>
    [Description("Total usage")] 
    public double? TotalUsage { get; set; }

    /// <summary>
    /// List of cluster node type reports
    /// </summary>
    [DataMember]
    [Description("List of cluster node type reports")]
    public List<ClusterNodeTypeReportExt> ClusterNodeTypes { get; set; }
}