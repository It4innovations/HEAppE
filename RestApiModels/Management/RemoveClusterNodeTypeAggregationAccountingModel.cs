using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "RemoveClusterNodeTypeAggregationAccountingModel")]
    public class RemoveClusterNodeTypeAggregationAccountingModel : SessionCodeModel
    {
        [DataMember(Name = "ClusterNodeTypeAggregationId", IsRequired = true)]
        public long ClusterNodeTypeAggregationId { get; set; }

        [DataMember(Name = "AccountingId", IsRequired = true)]
        public long AccountingId { get; set; }
    }
}
