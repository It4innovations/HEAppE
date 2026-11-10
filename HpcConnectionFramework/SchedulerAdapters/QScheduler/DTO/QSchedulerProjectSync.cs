using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.QScheduler.Generic;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.QScheduler.DTO;

/// <summary>
///     One correction to apply to a QScheduler project. Null fields stay unchanged.
/// </summary>
public record QSchedulerProjectUpdate(string QSchedulerProjectName, long? LimitMs, bool? Active, string Reason);

/// <summary>
///     Compares this HEAppE instance's QScheduler projects with HEAppE, which is the source of truth.
/// </summary>
public static class QSchedulerProjectSync
{
    /// <summary>
    ///     This instance's QScheduler project prefix for the cluster (throws when it is not configured or invalid).
    /// </summary>
    public static string ResolvePrefix(DomainObjects.ClusterInformation.Cluster cluster)
    {
        return QSchedulerSchedulerAdapter.ResolveProjectPrefix(cluster);
    }

    /// <summary>
    ///     Returns the updates that bring QScheduler in line with HEAppE.
    ///     Only projects with this instance's prefix are considered; projects of other instances are never touched.
    ///     <list type="bullet">
    ///         <item>A project HEAppE knows: limit = sum of allocations, active = within start/end date.</item>
    ///         <item>A prefixed project HEAppE no longer has (deleted, expired out of the list, removed from the cluster): deactivated.</item>
    ///         <item>A HEAppE project missing in QScheduler: nothing; it is created on the next submit.</item>
    ///     </list>
    /// </summary>
    /// <param name="prefix">This instance's project prefix.</param>
    /// <param name="qSchedulerProjects">All projects reported by QScheduler.</param>
    /// <param name="heappeProjects">HEAppE projects (QPU seconds, not deleted) assigned to the cluster.</param>
    /// <param name="now">Current time (UTC).</param>
    public static IList<QSchedulerProjectUpdate> Plan(string prefix, IEnumerable<QSchedulerProjectInfo> qSchedulerProjects,
        IEnumerable<Project> heappeProjects, DateTime now)
    {
        var ownPrefix = prefix + QSchedulerSchedulerAdapter.ProjectNameSeparator;
        var byAccountingString = (heappeProjects ?? Enumerable.Empty<Project>())
            .Where(p => !string.IsNullOrWhiteSpace(p.AccountingString))
            .GroupBy(p => p.AccountingString.Trim(), StringComparer.Ordinal)
            // Accounting strings should be unique; if not, the project that is valid now wins.
            .ToDictionary(g => g.Key,
                g => g.OrderByDescending(p => QSchedulerSchedulerAdapter.IsProjectActiveAt(p, now)).First(),
                StringComparer.Ordinal);

        var updates = new List<QSchedulerProjectUpdate>();
        foreach (var qsProject in qSchedulerProjects ?? Enumerable.Empty<QSchedulerProjectInfo>())
        {
            if (qsProject?.Name == null || !qsProject.Name.StartsWith(ownPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var accountingString = qsProject.Name.Substring(ownPrefix.Length);
            if (!byAccountingString.TryGetValue(accountingString, out var project))
            {
                if (qsProject.Active)
                {
                    updates.Add(new QSchedulerProjectUpdate(qsProject.Name, null, false,
                        "project no longer exists in HEAppE for this cluster"));
                }
                continue;
            }

            var desiredActive = QSchedulerSchedulerAdapter.IsProjectActiveAt(project, now);
            var desiredLimit = QSchedulerSchedulerAdapter.ComputeProjectLimitMs(project);

            var limitDiffers = qsProject.LimitMs != desiredLimit;
            var activeDiffers = qsProject.Active != desiredActive;
            if (!limitDiffers && !activeDiffers)
            {
                continue;
            }

            var reasons = new List<string>();
            if (limitDiffers) reasons.Add($"limit {qsProject.LimitMs} ms -> {desiredLimit} ms");
            if (activeDiffers) reasons.Add(desiredActive ? "project is valid -> active" : "project outside its start/end date -> inactive");

            updates.Add(new QSchedulerProjectUpdate(qsProject.Name,
                limitDiffers ? desiredLimit : null,
                activeDiffers ? desiredActive : null,
                string.Join(", ", reasons)));
        }

        return updates;
    }
}
