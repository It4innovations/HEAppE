using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement.JobInformation;

[Table("ConsumedResources")]
public class ResourceConsumed : IdentifiableDbEntity
{
    [ForeignKey("SubmittedTaskInfo")]
    public virtual long SubmittedTaskInfoId { get; set; }
    public virtual SubmittedTaskInfo SubmittedTaskInfo { get; set; }
    [ForeignKey("Accounting")]
    public virtual long AccountingId { get; set; }
    public virtual Accounting Accounting { get; set; }
    public double? Value { get; set; } = null;
    public DateTime? LastUpdatedAt { get; set; }
}