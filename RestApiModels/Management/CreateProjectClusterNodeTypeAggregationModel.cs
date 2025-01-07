﻿using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "CreateProjectClusterNodeTypeAggregationModel")]
public class CreateProjectClusterNodeTypeAggregationModel : SessionCodeModel
{
    [DataMember(Name = "ProjectId", IsRequired = true)]
    public long ProjectId { get; set; }

    [DataMember(Name = "ClusterNodeTypeAggregationId", IsRequired = true)]
    public long ClusterNodeTypeAggregationId { get; set; }

    [DataMember(Name = "AllocationAmount", IsRequired = true)]
    public long AllocationAmount { get; set; }
}