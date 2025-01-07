using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "CreateClusterNodeTypeAggregationModel")]
public class CreateClusterNodeTypeAggregationAccountingModel : SessionCodeModel
{
    [DataMember(Name = "ClusterNodeTypeAggregationId", IsRequired = true)]
    public long ClusterNodeTypeAggregationId { get; set; }

    [DataMember(Name = "AccountingId", IsRequired = true)]
    public long AccountingId { get; set; }
}