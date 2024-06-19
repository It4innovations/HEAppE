using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement;

[Table("ProjectClusterNodeTypeAggregation")]
public class ProjectClusterNodeTypeAggregation
{
    public long ProjectId { get; set; }
    public virtual Project Project { get; set; }
    
    public long ClusterNodeTypeAggregationId { get; set; }
    public virtual ClusterNodeTypeAggregation ClusterNodeTypeAggregation { get; set; }
    
    [Required]
    public long AllocationAmount { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
}