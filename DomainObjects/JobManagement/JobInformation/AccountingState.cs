using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement.JobInformation;

[Table("AccountingState")]
public class AccountingState : IdentifiableDbEntity
{
    [ForeignKey("Project")] public virtual long ProjectId { get; set; }

    public virtual Project Project { get; set; }
    public virtual AccountingStateType AccountingStateType { get; set; }
    public DateTime ComputingStartDate { get; set; }
    public DateTime? ComputingEndDate { get; set; }
    public DateTime TriggeredAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}