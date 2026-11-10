using System;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DomainObjects.JobReporting;

/// <summary>
///     Consumption of one QScheduler session. QScheduler charges the whole open time of a session to the project,
///     so sessions are reported on their own, next to jobs; tasks that ran in a session have no usage of their own.
/// </summary>
public class QSchedulerSessionReport
{
    public QSchedulerSession Session { get; set; }

    /// <summary>
    ///     Usage in QPU seconds (session open time).
    /// </summary>
    public double Usage => Math.Round((Session?.ConsumedMs ?? 0) / 1000.0, 3);
}
