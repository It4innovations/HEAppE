using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.DomainObjects.Monitoring;

namespace HEAppE.Services.Monitoring;

/// <summary>
/// Service interface for recording in-process telemetry of external services and database operations.
/// </summary>
public interface IExternalServiceTelemetryService
{
    /// <summary>
    /// Configuration settings for telemetry.
    /// </summary>
    ExternalServiceTelemetrySettings Settings { get; }

    /// <summary>
    /// Records a pre-built telemetry log item into the non-blocking channel.
    /// </summary>
    void Record(ExternalServiceHealthLog log);

    /// <summary>
    /// Records a telemetry observation with explicit parameters. Ambient JobContext is used for missing JobId/TaskId/RequestId.
    /// </summary>
    void Record(
        string serviceName,
        string serviceType,
        string operation,
        long durationMs,
        bool isSuccess,
        string statusCode = null,
        string errorMessage = null,
        long? jobId = null,
        long? taskId = null,
        long? clusterId = null,
        string endpointOrHost = null,
        string protocol = null,
        string source = "Execution");

    /// <summary>
    /// Measures the execution time of an asynchronous function, captures exceptions, and records telemetry automatically.
    /// </summary>
    Task<T> MeasureAsync<T>(
        string serviceName,
        string serviceType,
        string operation,
        Func<Task<T>> action,
        long? clusterId = null,
        string endpointOrHost = null,
        string protocol = null);

    /// <summary>
    /// Measures the execution time of an asynchronous action, captures exceptions, and records telemetry automatically.
    /// </summary>
    Task MeasureAsync(
        string serviceName,
        string serviceType,
        string operation,
        Func<Task> action,
        long? clusterId = null,
        string endpointOrHost = null,
        string protocol = null);

    /// <summary>
    /// Streams all queued telemetry logs for background persistence.
    /// </summary>
    IAsyncEnumerable<ExternalServiceHealthLog> ReadAllAsync(CancellationToken cancellationToken);
}
