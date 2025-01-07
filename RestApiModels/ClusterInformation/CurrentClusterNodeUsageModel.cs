using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.ClusterInformation;

[DataContract(Name = "CurrentClusterNodeUsageModel")]
public class CurrentClusterNodeUsageModel : SessionCodeModel
{
    [DataMember(Name = "ClusterNodeId")] public long ClusterNodeId { get; set; }

    [DataMember(Name = "ProjectId")] public long ProjectId { get; set; }

    public override string ToString()
    {
        return $"CurrentClusterNodeUsageModel({base.ToString()}; ClusterNodeId: {ClusterNodeId})";
    }
}