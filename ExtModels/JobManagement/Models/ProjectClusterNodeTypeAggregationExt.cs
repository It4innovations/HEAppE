using System;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models;

[DataContract(Name = "ProjectClusterNodeTypeAggregationExt")]
public class ProjectClusterNodeTypeAggregationExt
{
    [DataMember(Name = "ProjectId")] public long ProjectId { get; set; }

    [DataMember(Name = "ClusterNodeTypeAggregationId")]
    public long ClusterNodeTypeAggregationId { get; set; }

    [DataMember(Name = "AllocationAmount")]
    public long AllocationAmount { get; set; }

    [DataMember(Name = "CreatedAt")] public DateTime CreatedAt { get; set; }

    [DataMember(Name = "ModifiedAt")] public DateTime? ModifiedAt { get; set; }

    public override string ToString()
    {
        return
            $"ProjectClusterNodeTypeAggregationExt(projectId={ProjectId}; clusterNodeTypeAggregationId={ClusterNodeTypeAggregationId}; allocationAmount={AllocationAmount}; createdAt={CreatedAt}; modifiedAt={ModifiedAt})";
    }
}