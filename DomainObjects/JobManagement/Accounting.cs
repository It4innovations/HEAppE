using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.DomainObjects.JobManagement;

[Table("Accounting")]
public class Accounting : IdentifiableDbEntity, ISoftDeletableEntity
{
    [Required] [StringLength(200)] public string Formula { get; set; }

    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; set; }

    [Required] public DateTime ValidityFrom { get; set; }

    public DateTime? ValidityTo { get; set; }

    public virtual List<ResourceConsumed> ConsumedResources { get; set; } = new();

    public virtual List<ClusterNodeTypeAggregationAccounting> ClusterNodeTypeAggregationAccountings { get; set; } =
        new();

    [Required] public bool IsDeleted { get; set; } = false;

    public bool IsValid(DateTime? startTime, DateTime? endTime)
    {
        return startTime.HasValue && ValidityFrom <= startTime && (!ValidityTo.HasValue || ValidityTo >= startTime);
    }
}