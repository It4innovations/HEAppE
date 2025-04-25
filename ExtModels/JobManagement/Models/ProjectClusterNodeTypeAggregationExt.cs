using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models;

/// <summary>
/// Project cluster node type aggregation ext
/// </summary>
[DataContract(Name = "ProjectClusterNodeTypeAggregationExt")]
[Description("Project cluster node type aggregation ext")]
public class ProjectClusterNodeTypeAggregationExt
{
    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId")]
    [Description("Project id")]
    public long ProjectId { get; set; }

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

    /// <summary>
    /// Created at date
    /// </summary>
    [DataMember(Name = "CreatedAt")]
    [Description("Created at date")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Modified at date
    /// </summary>
    [DataMember(Name = "ModifiedAt")]
    [Description("Modified at date")]
    public DateTime? ModifiedAt { get; set; }

    public override string ToString()
    {
        return $"ProjectClusterNodeTypeAggregationExt(projectId={ProjectId}; clusterNodeTypeAggregationId={ClusterNodeTypeAggregationId}; allocationAmount={AllocationAmount}; createdAt={CreatedAt}; modifiedAt={ModifiedAt})";
    }
}