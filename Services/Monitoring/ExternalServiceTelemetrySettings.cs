namespace HEAppE.Services.Monitoring;

/// <summary>
/// Configuration settings for external service and database telemetry monitoring.
/// </summary>
public class ExternalServiceTelemetrySettings
{
    /// <summary>
    /// Whether telemetry logging of external service calls and database queries is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Capacity of the in-memory telemetry buffer channel before dropping or throttling.
    /// </summary>
    public int ChannelCapacity { get; set; } = 50000;

    /// <summary>
    /// Minimum database command duration in milliseconds to be recorded as telemetry (default 5ms).
    /// Failed database queries are always recorded regardless of duration.
    /// </summary>
    public int MinDbDurationThresholdMs { get; set; } = 5;

    /// <summary>
    /// Batch write interval in milliseconds for background database flushes (default 2000ms).
    /// </summary>
    public int FlushIntervalMs { get; set; } = 2000;

    /// <summary>
    /// Maximum batch size for a single database flush (default 100).
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>
    /// Data retention period in days before old logs are automatically purged by background worker (default 30 days).
    /// </summary>
    public int RetentionDays { get; set; } = 30;
}
