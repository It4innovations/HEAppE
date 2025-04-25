using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Remove cluster node type aggregation accounting model
/// </summary>
[DataContract(Name = "RemoveClusterNodeTypeAggregationAccountingModel")]
[Description("Remove cluster node type aggregation accounting model")]
public class RemoveClusterNodeTypeAggregationAccountingModel : SessionCodeModel
{
    /// <summary>
    /// Cluster node type aggregation id
    /// </summary>
    [DataMember(Name = "ClusterNodeTypeAggregationId", IsRequired = true)]
    [Description("Cluster node type aggregation id")]
    public long ClusterNodeTypeAggregationId { get; set; }

    /// <summary>
    /// Accounting id
    /// </summary>
    [DataMember(Name = "AccountingId", IsRequired = true)]
    [Description("Accounting id")]
    public long AccountingId { get; set; }
}