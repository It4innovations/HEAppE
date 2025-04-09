using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DomainObjects.JobManagement;

[Table("ClusterNodeTypeAggregation")]
public class ClusterNodeTypeAggregation : IdentifiableDbEntity, ISoftDeletableEntity
{
    [Required] [StringLength(50)] public string Name { get; set; }

    [StringLength(100)] public string Description { get; set; }

    public string AllocationType { get; set; }

    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; set; }

    [Required] public DateTime ValidityFrom { get; set; }

    public DateTime? ValidityTo { get; set; }

    public virtual List<ClusterNodeTypeAggregationAccounting> ClusterNodeTypeAggregationAccountings { get; set; } =
        new();

    public virtual List<ProjectClusterNodeTypeAggregation> ProjectClusterNodeTypeAggregations { get; set; } = new();
    public virtual List<ClusterNodeType> ClusterNodeTypes { get; set; } = new();

    [Required] public bool IsDeleted { get; set; } = false;
}