using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DomainObjects.JobReporting;

public class ProjectReport
{
    public Project Project { get; set; }
    public List<ClusterReport> Clusters { get; set; }

    /// <summary>
    ///     QScheduler sessions of the project in the report period (billed for their whole open time).
    /// </summary>
    public List<QSchedulerSessionReport> QSchedulerSessions { get; set; } = new();

    public double QSchedulerSessionsUsage => Math.Round(QSchedulerSessions?.Sum(x => x.Usage) ?? 0, 3);

    public double? TotalUsage => Math.Round((Clusters?.Sum(x => x.TotalUsage) ?? 0) + QSchedulerSessionsUsage, 3);
}