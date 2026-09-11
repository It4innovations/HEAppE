using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.Monitoring;

/// <summary>
/// Persisted record of a single health check or passive telemetry observation for an external service.
/// Rows older than the configured retention window are purged automatically by the background monitor.
/// </summary>
[Table("ExternalServiceHealthLog")]
public class ExternalServiceHealthLog : IdentifiableDbEntity
{
    /// <summary>
    /// Human-readable name of the monitored service (e.g. cluster name, "HashiCorp Vault", "Keycloak").
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ServiceName { get; set; }

    /// <summary>
    /// Category of the service (lowercase). Examples: "cluster", "keymanagement", "identity", "cleanupservice", "database".
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ServiceType { get; set; }

    /// <summary>
    /// Protocol used to reach this service (lowercase). Examples: "ssh", "http", "https", "sql".
    /// </summary>
    [StringLength(20)]
    public string Protocol { get; set; }

    /// <summary>
    /// The host, URL, or IP address that was probed or used.
    /// </summary>
    [StringLength(500)]
    public string EndpointOrHost { get; set; }

    /// <summary>
    /// Port used for the connection. Null for HTTP(S) services where port is embedded in the URL.
    /// </summary>
    public int? Port { get; set; }

    /// <summary>
    /// The specific scheduler command executed (e.g. "sbatch", "squeue"), HTTP path called
    /// (e.g. "/v1/sys/health", "token-introspection"), or operation name (e.g. "GetSecret", "ExecuteQuery").
    /// </summary>
    [StringLength(250)]
    public string CommandOrPath { get; set; }

    /// <summary>
    /// UTC timestamp when the check or operation was recorded.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Whether the service responded successfully within the allowed timeout.
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Round-trip or execution time in milliseconds.
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// Error message captured when IsAvailable is false. Null on success.
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Associated Job ID if this operation was triggered during processing of a specific job.
    /// </summary>
    public long? JobId { get; set; }

    /// <summary>
    /// Associated Task ID if this operation was triggered during processing of a specific task.
    /// </summary>
    public long? TaskId { get; set; }

    /// <summary>
    /// Associated Cluster ID if this operation was targeted at a specific cluster.
    /// </summary>
    public long? ClusterId { get; set; }

    /// <summary>
    /// HTTP status code (e.g. "200", "502"), SSH exit code (e.g. "127"), or SQL error number.
    /// </summary>
    [StringLength(50)]
    public string StatusCode { get; set; }

    /// <summary>
    /// Correlation ID / Request ID matching log4net requestId and Log table for cross-referencing.
    /// </summary>
    [StringLength(100)]
    public string RequestId { get; set; }

    /// <summary>
    /// Source of the telemetry entry: "Execution" (inline in-process telemetry) or "HealthCheck" (proactive polling).
    /// </summary>
    [StringLength(30)]
    public string Source { get; set; }
}
