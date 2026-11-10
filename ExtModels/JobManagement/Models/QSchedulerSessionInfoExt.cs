using System;

namespace HEAppE.ExtModels.JobManagement.Models;

public class QSchedulerSessionInfoExt
{
    public long SessionId { get; set; }
    public long ClusterId { get; set; }
    public long ProjectId { get; set; }
    public string State { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    ///     Time the session has been open, in milliseconds. This is what QScheduler charges to the project
    ///     (the whole open time, not only the run time of the tasks). Null until QScheduler reports it.
    /// </summary>
    public long? ConsumedMs { get; set; }

    /// <summary>
    ///     Consumed QPU seconds (ConsumedMs / 1000).
    /// </summary>
    public double? ConsumedQpuSeconds { get; set; }

    /// <summary>
    ///     True once the session is closed and its final consumption is known.
    /// </summary>
    public bool IsConsumptionFinal { get; set; }

    public static QSchedulerSessionInfoExt FromDomain(HEAppE.DomainObjects.UserAndLimitationManagement.QSchedulerSession session)
    {
        return new QSchedulerSessionInfoExt
        {
            SessionId = session.SessionId,
            ClusterId = session.ClusterId,
            ProjectId = session.ProjectId,
            State = session.State.ToString(),
            CreatedAt = session.CreatedAt,
            OpenedAt = session.OpenedAt,
            ClosedAt = session.ClosedAt,
            ConsumedMs = session.ConsumedMs,
            ConsumedQpuSeconds = session.ConsumedMs.HasValue ? Math.Round(session.ConsumedMs.Value / 1000.0, 3) : null,
            IsConsumptionFinal = session.IsConsumptionFinal
        };
    }
}
