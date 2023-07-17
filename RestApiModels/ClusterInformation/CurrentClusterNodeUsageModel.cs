using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.ClusterInformation
{
    [DataContract(Name = "CurrentClusterNodeUsageModel")]
    public class CurrentClusterNodeUsageModel : SessionCodeModel
    {
        [DataMember(Name = "ClusterNodeId")]
        public long ClusterNodeId { get; set; }
        public override string ToString()
        {
            return $"CurrentClusterNodeUsageModel({base.ToString()}; ClusterNodeId: {ClusterNodeId})";
        }

    }
}
