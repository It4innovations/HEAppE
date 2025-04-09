using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models;

/// <summary>
/// Cluster node type aggregation for accounting ext
/// </summary>
[DataContract(Name = "ClusterNodeTypeAggregationAccountingExt")]
[Description("Cluster node type aggregation for accounting ext")]
public class ClusterNodeTypeAggregationAccountingExt
{
    /// <summary>
    /// Cluster node type aggregation id
    /// </summary>
    [DataMember(Name = "ClusterNodeTypeAggregationId")]
    [Description("Cluster node type aggregation id")]
    public long ClusterNodeTypeAggregationId { get; set; }

    /// <summary>
    /// Accounting id
    /// </summary>
    [DataMember(Name = "AccountingId")]
    [Description("Accounting id")]
    public long AccountingId { get; set; }

    public override string ToString()
    {
        return $"""ClusterNodeTypeAggregationAccountingExt: ClusterNodeTypeAggregationId={ClusterNodeTypeAggregationId}, AccountingId={AccountingId}""";
    }
}