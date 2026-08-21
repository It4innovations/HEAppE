using System;
using System.Threading;

namespace HEAppE.Utils;

/// <summary>
/// Execution context data holding ambient JobId, TaskId, ClusterId, and RequestId across async call chains.
/// </summary>
public class JobContextData
{
    public long? JobId { get; set; }
    public long? TaskId { get; set; }
    public long? ClusterId { get; set; }
    public string RequestId { get; set; }
}

/// <summary>
/// Thread-safe and async-safe ambient execution context providing correlation identifiers for telemetry and logging.
/// </summary>
public static class JobExecutionContext
{
    private static readonly AsyncLocal<JobContextData> _current = new();

    /// <summary>
    /// Current ambient execution context data (or null if not set).
    /// </summary>
    public static JobContextData Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    /// <summary>
    /// Current Job ID in execution context.
    /// </summary>
    public static long? CurrentJobId => _current.Value?.JobId;

    /// <summary>
    /// Current Task ID in execution context.
    /// </summary>
    public static long? CurrentTaskId => _current.Value?.TaskId;

    /// <summary>
    /// Current Cluster ID in execution context.
    /// </summary>
    public static long? CurrentClusterId => _current.Value?.ClusterId;

    /// <summary>
    /// Current Request / Correlation ID in execution context.
    /// </summary>
    public static string CurrentRequestId => _current.Value?.RequestId;

    /// <summary>
    /// Begins a new scoped execution context. When disposed, restores the previous context.
    /// </summary>
    public static IDisposable BeginScope(long? jobId = null, long? taskId = null, long? clusterId = null, string requestId = null)
    {
        var previous = _current.Value;
        var newContext = new JobContextData
        {
            JobId = jobId ?? previous?.JobId,
            TaskId = taskId ?? previous?.TaskId,
            ClusterId = clusterId ?? previous?.ClusterId,
            RequestId = requestId ?? previous?.RequestId
        };

        _current.Value = newContext;
        return new ContextScope(previous);
    }

    /// <summary>
    /// Sets or updates the current RequestId in the active context.
    /// </summary>
    public static void SetRequestId(string requestId)
    {
        if (_current.Value == null)
        {
            _current.Value = new JobContextData { RequestId = requestId };
        }
        else
        {
            _current.Value.RequestId = requestId;
        }
    }

    /// <summary>
    /// Sets or updates the current JobId in the active context.
    /// </summary>
    public static void SetJobId(long? jobId)
    {
        if (_current.Value == null)
        {
            _current.Value = new JobContextData { JobId = jobId };
        }
        else
        {
            _current.Value.JobId = jobId;
        }
    }

    /// <summary>
    /// Sets or updates the current ClusterId in the active context.
    /// </summary>
    public static void SetClusterId(long? clusterId)
    {
        if (_current.Value == null)
        {
            _current.Value = new JobContextData { ClusterId = clusterId };
        }
        else
        {
            _current.Value.ClusterId = clusterId;
        }
    }

    private sealed class ContextScope : IDisposable
    {
        private readonly JobContextData _previous;
        private bool _disposed;

        public ContextScope(JobContextData previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _current.Value = _previous;
                _disposed = true;
            }
        }
    }
}
