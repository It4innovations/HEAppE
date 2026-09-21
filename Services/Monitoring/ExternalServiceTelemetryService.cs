using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using HEAppE.DomainObjects.Monitoring;
using HEAppE.Utils;
using Microsoft.Extensions.Logging;

namespace HEAppE.Services.Monitoring;

/// <summary>
/// High-throughput, non-blocking telemetry collector using System.Threading.Channels.
/// </summary>
public class ExternalServiceTelemetryService : IExternalServiceTelemetryService
{
    private static IExternalServiceTelemetryService _instance;
    private readonly Channel<ExternalServiceHealthLog> _channel;
    private readonly ILogger<ExternalServiceTelemetryService> _logger;

    public ExternalServiceTelemetrySettings Settings { get; }
    public ChannelReader<ExternalServiceHealthLog> Reader => _channel.Reader;

    /// <summary>
    /// Global singleton instance accessor for non-DI callers.
    /// </summary>
    public static IExternalServiceTelemetryService Instance
    {
        get => _instance ??= new ExternalServiceTelemetryService();
        set => _instance = value;
    }

    public ExternalServiceTelemetryService(
        ExternalServiceTelemetrySettings settings = null,
        ILogger<ExternalServiceTelemetryService> logger = null)
    {
        Settings = settings ?? new ExternalServiceTelemetrySettings();
        _logger = logger;

        var channelOptions = new BoundedChannelOptions(Math.Max(1000, Settings.ChannelCapacity))
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<ExternalServiceHealthLog>(channelOptions);
        _instance = this;
    }

    public void Record(ExternalServiceHealthLog log)
    {
        if (!Settings.IsEnabled || log == null) return;

        // Enrich with ambient context if missing
        log.JobId ??= JobExecutionContext.CurrentJobId;
        log.TaskId ??= JobExecutionContext.CurrentTaskId;
        log.ClusterId ??= JobExecutionContext.CurrentClusterId;
        log.RequestId ??= JobExecutionContext.CurrentRequestId;
        log.Source ??= "Execution";

        if (log.Timestamp == default)
        {
            log.Timestamp = DateTime.UtcNow;
        }

        if (!_channel.Writer.TryWrite(log))
        {
            _logger?.LogWarning("Telemetry buffer is full. Dropped telemetry record for service {ServiceName}.", log.ServiceName);
        }
    }

    public void Record(
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
        string source = "Execution")
    {
        if (!Settings.IsEnabled) return;

        var log = new ExternalServiceHealthLog
        {
            ServiceName = serviceName ?? "unknown",
            ServiceType = serviceType ?? "unknown",
            CommandOrPath = operation,
            ResponseTimeMs = Math.Max(0, durationMs),
            IsAvailable = isSuccess,
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
            JobId = jobId ?? JobExecutionContext.CurrentJobId,
            TaskId = taskId ?? JobExecutionContext.CurrentTaskId,
            ClusterId = clusterId ?? JobExecutionContext.CurrentClusterId,
            RequestId = JobExecutionContext.CurrentRequestId,
            EndpointOrHost = endpointOrHost,
            Protocol = protocol,
            Source = source,
            Timestamp = DateTime.UtcNow
        };

        Record(log);
    }

    public async Task<T> MeasureAsync<T>(
        string serviceName,
        string serviceType,
        string operation,
        Func<Task<T>> action,
        long? clusterId = null,
        string endpointOrHost = null,
        string protocol = null)
    {
        if (!Settings.IsEnabled)
        {
            return await action();
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await action();
            sw.Stop();
            Record(serviceName, serviceType, operation, sw.ElapsedMilliseconds, true, "200", null, null, null, clusterId, endpointOrHost, protocol);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Record(serviceName, serviceType, operation, sw.ElapsedMilliseconds, false, "500", ex.Message, null, null, clusterId, endpointOrHost, protocol);
            throw;
        }
    }

    public async Task MeasureAsync(
        string serviceName,
        string serviceType,
        string operation,
        Func<Task> action,
        long? clusterId = null,
        string endpointOrHost = null,
        string protocol = null)
    {
        if (!Settings.IsEnabled)
        {
            await action();
            return;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            await action();
            sw.Stop();
            Record(serviceName, serviceType, operation, sw.ElapsedMilliseconds, true, "200", null, null, null, clusterId, endpointOrHost, protocol);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Record(serviceName, serviceType, operation, sw.ElapsedMilliseconds, false, "500", ex.Message, null, null, clusterId, endpointOrHost, protocol);
            throw;
        }
    }

    public IAsyncEnumerable<ExternalServiceHealthLog> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
