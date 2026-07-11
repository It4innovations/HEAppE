using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.UserAndLimitationManagement;

public enum QSchedulerSessionState
{
    Open = 0,
    Closed = 1
}

[Table("QSchedulerSession")]
public class QSchedulerSession : IdentifiableDbEntity
{
    [Required]
    public long SessionId { get; set; }

    [Required]
    public long ClusterId { get; set; }

    [Required]
    public long ProjectId { get; set; }

    [Required]
    public QSchedulerSessionState State { get; set; } = QSchedulerSessionState.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ClosedAt { get; set; }

    [Required]
    [ForeignKey("AdaptorUser")]
    public long UserId { get; set; }

    public virtual AdaptorUser User { get; set; }
}
