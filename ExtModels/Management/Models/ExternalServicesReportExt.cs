using System.Collections.Generic;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Full response payload for GET /heappe/Management/GetExternalServicesReport.
/// Contains both the freshly probed live status of every registered service and
/// historical aggregated statistics from the telemetry database for the requested time window.
/// </summary>
public class ExternalServicesReportExt
{
    /// <summary>
    /// One entry per registered service reflecting the result of the parallel live health probes
    /// performed at the moment this endpoint was called.
    /// </summary>
    public List<ExternalServiceLiveStatusExt> LiveStatus { get; set; } = new();

    /// <summary>
    /// Aggregated statistics grouped by (service, command/path) from the
    /// ExternalServiceHealthLog table for the requested time window.
    /// </summary>
    public List<ExternalServiceStatisticsExt> Statistics { get; set; } = new();
}
