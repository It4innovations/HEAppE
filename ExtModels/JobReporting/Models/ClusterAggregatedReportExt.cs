using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models;

/// <summary>
/// Cluster aggregated report ext
/// </summary>
[DataContract(Name = "ClusterReportExt")]
[Description("Cluster aggregated report ext")]
public class ClusterAggregatedReportExt
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
    /// List of cluster node type aggregated reports
    /// </summary>
    [DataMember]
    [Description("List of cluster node type aggregated reports")]
    public List<ClusterNodeTypeAggregatedReportExt> ClusterNodeTypesAggregations { get; set; }
}