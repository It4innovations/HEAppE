using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models;

[DataContract(Name = "ClusterNodeTypeAggregationAccountingExt")]
public class ClusterNodeTypeAggregationAccountingExt
{
    [DataMember(Name = "ClusterNodeTypeAggregationId")]
    public long ClusterNodeTypeAggregationId { get; set; }

    [DataMember(Name = "AccountingId")] public long AccountingId { get; set; }

    public override string ToString()
    {
        return
            $"""ClusterNodeTypeAggregationAccountingExt: ClusterNodeTypeAggregationId={ClusterNodeTypeAggregationId}, AccountingId={AccountingId}""";
    }
}