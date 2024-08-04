using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "RemoveProjectClusterNodeTypeAggregationModel")]
    public class RemoveProjectClusterNodeTypeAggregationModel : SessionCodeModel
    {
        [DataMember(Name = "ProjectId", IsRequired = true)]
        public long ProjectId { get; set; }

        [DataMember(Name = "ClusterNodeTypeAggregationId", IsRequired = true)]
        public long ClusterNodeTypeAggregationId { get; set; }
    }
}
