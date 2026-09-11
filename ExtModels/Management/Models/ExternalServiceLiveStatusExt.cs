using System;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Current live status snapshot for a single monitored external service.
/// </summary>
public class ExternalServiceLiveStatusExt
{
    /// <summary>Human-readable name of the service (e.g. cluster name, "HashiCorp Vault").</summary>
    public string ServiceName { get; set; }

    /// <summary>Service category in lowercase (e.g. "cluster", "keymanagement", "identity").</summary>
    public string Type { get; set; }

    /// <summary>Protocol used for the health probe in lowercase (e.g. "ssh", "https", "sql").</summary>
    public string Protocol { get; set; }

    /// <summary>Host, URL, or IP address that was probed.</summary>
    public string EndpointOrHost { get; set; }

    /// <summary>Port number probed. Null for HTTP/HTTPS checks where the port is implicit.</summary>
    public int? Port { get; set; }

    /// <summary>True when the service responded within the configured timeout.</summary>
    public bool IsAvailable { get; set; }

    /// <summary>Round-trip time in milliseconds. Zero on timeout.</summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>Error description when IsAvailable is false. Null on success.</summary>
    public string ErrorMessage { get; set; }

    /// <summary>UTC timestamp of this live check.</summary>
    public DateTime LastCheck { get; set; }
}
