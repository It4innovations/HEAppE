using System;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Detailed telemetry record of an external service or database operation linked to a job.
/// </summary>
public class JobExternalServiceLogExt
{
    public long Id { get; set; }
    public string ServiceName { get; set; }
    public string ServiceType { get; set; }
    public string Protocol { get; set; }
    public string EndpointOrHost { get; set; }
    public int? Port { get; set; }
    public string Operation { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsSuccess { get; set; }
    public long ResponseTimeMs { get; set; }
    public string StatusCode { get; set; }
    public string ErrorMessage { get; set; }
    public long? JobId { get; set; }
    public long? TaskId { get; set; }
    public long? ClusterId { get; set; }
    public string RequestId { get; set; }
    public string Source { get; set; }
}
