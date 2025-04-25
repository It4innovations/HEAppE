using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Remove cluster node type aggregation model
/// </summary>
[DataContract(Name = "RemoveClusterNodeTypeAggregationModel")]
[Description("Remove cluster node type aggregation model")]
public class RemoveClusterNodeTypeAggregationModel : SessionCodeModel
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id", IsRequired = true)]
    [Description("Id")]
    public long Id { get; set; }
}