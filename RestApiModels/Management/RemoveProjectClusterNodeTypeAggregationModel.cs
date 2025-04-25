using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Remove project cluster node type aggregation model
/// </summary>
[DataContract(Name = "RemoveProjectClusterNodeTypeAggregationModel")]
[Description("Remove project cluster node type aggregation model")]
public class RemoveProjectClusterNodeTypeAggregationModel : SessionCodeModel
{
    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId", IsRequired = true)]
    [Description("Project id")]
    public long ProjectId { get; set; }

    /// <summary>
    /// Cluster node type aggregation id
    /// </summary>
    [DataMember(Name = "ClusterNodeTypeAggregationId", IsRequired = true)]
    [Description("Cluster node type aggregation id")]
    public long ClusterNodeTypeAggregationId { get; set; }
}