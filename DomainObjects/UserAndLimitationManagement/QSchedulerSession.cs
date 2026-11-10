using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.UserAndLimitationManagement;

public enum QSchedulerSessionState
{
    Open = 0,
    Closed = 1,
    Waiting = 2
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

    /// <summary>
    ///     When QScheduler opened the session (reported by QScheduler).
    /// </summary>
    public DateTime? OpenedAt { get; set; }

    /// <summary>
    ///     Time the session has been open, in milliseconds, as reported by QScheduler (session exectime_ms).
    ///     This is the time charged to the project: the whole open time, not the run time of the tasks in the session.
    /// </summary>
    public long? ConsumedMs { get; set; }

    /// <summary>
    ///     When ConsumedMs was last refreshed from QScheduler.
    /// </summary>
    public DateTime? ConsumptionUpdatedAt { get; set; }

    /// <summary>
    ///     True once the session is closed and its final consumption has been read from QScheduler.
    /// </summary>
    [Required]
    public bool IsConsumptionFinal { get; set; } = false;

    [Required]
    [ForeignKey("AdaptorUser")]
    public long UserId { get; set; }

    public virtual AdaptorUser User { get; set; }
}
