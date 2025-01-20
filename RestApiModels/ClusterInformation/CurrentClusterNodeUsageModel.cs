using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.ClusterInformation;

/// <summary>
/// Model for retrieving current cluster node usage
/// </summary>
[DataContract(Name = "CurrentClusterNodeUsageModel")]
[Description("Model for retrieving current cluster node usage")]
public class CurrentClusterNodeUsageModel : SessionCodeModel
{
    /// <summary>
    /// Cluster node id
    /// </summary>
    [DataMember(Name = "ClusterNodeId")]
    [Description("Cluster node id")]
    public long ClusterNodeId { get; set; }

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId")]
    [Description("Project id")]
    public long ProjectId { get; set; }

    public override string ToString()
    {
        return $"CurrentClusterNodeUsageModel({base.ToString()}; ClusterNodeId: {ClusterNodeId})";
    }
}