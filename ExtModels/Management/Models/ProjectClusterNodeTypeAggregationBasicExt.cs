using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Usage types
/// </summary>
[Description("Project ClusterNodeType Aggregation Basic Ext")]
public class ProjectClusterNodeTypeAggregationBasicExt
{
    /// <summary>
    /// Cluster node type aggregation id
    /// </summary>
    [DataMember(Name = "ClusterNodeTypeAggregationId")]
    [Description("Cluster node type aggregation id")]
    public long ClusterNodeTypeAggregationId { get; set; }

    /// <summary>
    /// Allocation amount
    /// </summary>
    [DataMember(Name = "AllocationAmount")]
    [Description("Allocation amount")]
    public long AllocationAmount { get; set; }
}