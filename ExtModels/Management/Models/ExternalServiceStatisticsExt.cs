namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Aggregated telemetry statistics for one (service, command/path) pair over the requested time window.
/// </summary>
public class ExternalServiceStatisticsExt
{
    /// <summary>Human-readable name of the service.</summary>
    public string ServiceName { get; set; }

    /// <summary>
    /// Specific command or HTTP path these statistics apply to (lowercase).
    /// Examples: "sbatch", "squeue", "/v1/sys/health", "token-introspection".
    /// Null for generic TCP reachability checks.
    /// </summary>
    public string CommandOrPath { get; set; }

    /// <summary>Percentage of successful checks over the requested time window (0-100).</summary>
    public double AvailabilityPercentage { get; set; }

    /// <summary>Mean response time in milliseconds across all successful checks.</summary>
    public long AverageResponseTimeMs { get; set; }

    /// <summary>Minimum observed response time in milliseconds.</summary>
    public long MinResponseTimeMs { get; set; }

    /// <summary>Maximum observed response time in milliseconds.</summary>
    public long MaxResponseTimeMs { get; set; }

    /// <summary>Total number of check records in the requested time window.</summary>
    public long TotalChecks { get; set; }
}
