using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices.JavaScript;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DomainObjects.JobManagement;

[Table("Accounting")]
public class Accounting : IdentifiableDbEntity
{
    [Required]
    [StringLength(200)]
    public string Formula { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; set; }

    [Required]
    public bool IsDeleted { get; set; } = false;
    
    [Required]
    public DateTime ValidityFrom { get; set; }

    public DateTime? ValidityTo { get; set; }
    public bool IsValid(DateTime? startTime, DateTime? endTime) => (startTime.HasValue && ValidityFrom <= startTime && (ValidityTo.HasValue && ValidityTo >= startTime));
    
    public virtual List<ClusterNodeTypeAggregationAccounting> ClusterNodeTypeAggregationAccountings { get; set; } = new List<ClusterNodeTypeAggregationAccounting>();
}