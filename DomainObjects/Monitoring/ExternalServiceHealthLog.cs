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
    [Required]
    [StringLength(20)]
    public string Protocol { get; set; }

    /// <summary>
    /// The host, URL, or IP address that was probed or used.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string EndpointOrHost { get; set; }

    /// <summary>
    /// Port used for the connection. Null for HTTP(S) services where port is embedded in the URL.
    /// </summary>
    public int? Port { get; set; }

    /// <summary>
    /// The specific scheduler command executed (e.g. "sbatch", "squeue") or HTTP path called
    /// (e.g. "/v1/sys/health", "token-introspection"). Stored in lowercase.
    /// Null for generic TCP reachability checks.
    /// </summary>
    [StringLength(250)]
    public string CommandOrPath { get; set; }

    /// <summary>
    /// UTC timestamp when the check was recorded.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Whether the service responded successfully within the allowed timeout.
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Round-trip time in milliseconds. Zero when the request timed out or was not sent.
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// Error message captured when IsAvailable is false. Null on success.
    /// </summary>
    [StringLength(500)]
    public string ErrorMessage { get; set; }
}
