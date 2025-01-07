using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "ModifyProjectClusterNodeTypeAggregationModel")]
public class ModifyProjectClusterNodeTypeAggregationModel : SessionCodeModel
{
    [DataMember(Name = "ProjectId", IsRequired = true)]
    public long ProjectId { get; set; }

    [DataMember(Name = "ClusterNodeTypeAggregationId", IsRequired = true)]
    public long ClusterNodeTypeAggregationId { get; set; }

    [DataMember(Name = "AllocationAmount", IsRequired = true)]
    public long AllocationAmount { get; set; }
}