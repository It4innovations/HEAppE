using System;
using System.Collections.Concurrent;

namespace HEAppE.Utils;

/// <summary>
/// Thread-safe in-memory cache to hold QScheduler quantum circuit payloads in RAM
/// across asynchronous operations and HTTP callbacks without disk I/O.
/// </summary>
public static class QSchedulerPayloadContext
{
    private sealed class PayloadEntry
    {
        public byte[] Data { get; }
        public DateTime CreatedAt { get; }

        public PayloadEntry(byte[] data)
        {
            Data = data;
            CreatedAt = DateTime.UtcNow;
        }
    }

    private static readonly ConcurrentDictionary<long, PayloadEntry> _payloadsByTaskId = new();
    private static readonly ConcurrentDictionary<string, PayloadEntry> _payloadsByName = new();

    /// <summary>
    /// Stores the payload bytes for a specific submitted task ID.
    /// </summary>
    public static void StoreByTaskId(long taskId, byte[] payload)
    {
        if (payload != null)
        {
            _payloadsByTaskId[taskId] = new PayloadEntry(payload);
        }
    }

    /// <summary>
    /// Attempts to retrieve the payload bytes for a specific task ID.
    /// </summary>
    public static bool TryGetByTaskId(long taskId, out byte[] payload)
    {
        if (_payloadsByTaskId.TryGetValue(taskId, out var entry))
        {
            payload = entry.Data;
            return true;
        }
        payload = null;
        return false;
    }

    /// <summary>
    /// Removes the payload for a specific task ID from memory.
    /// </summary>
    public static void RemoveByTaskId(long taskId)
    {
        _payloadsByTaskId.TryRemove(taskId, out _);
    }

    /// <summary>
    /// Stores the payload bytes by task name (used during initial job submission flow).
    /// </summary>
    public static void StoreByName(string taskName, byte[] payload)
    {
        if (taskName != null && payload != null)
        {
            _payloadsByName[taskName] = new PayloadEntry(payload);
        }
    }

    /// <summary>
    /// Attempts to retrieve the payload bytes by task name.
    /// </summary>
    public static bool TryGetByName(string taskName, out byte[] payload)
    {
        if (taskName != null && _payloadsByName.TryGetValue(taskName, out var entry))
        {
            payload = entry.Data;
            return true;
        }
        payload = null;
        return false;
    }

    /// <summary>
    /// Removes the payload by task name from memory.
    /// </summary>
    public static void RemoveByName(string taskName)
    {
        if (taskName != null)
        {
            _payloadsByName.TryRemove(taskName, out _);
        }
    }

    /// <summary>
    /// Cleans up any payloads that have exceeded the specified age (default: 24 hours).
    /// </summary>
    public static void CleanupExpired(TimeSpan? maxAge = null)
    {
        var limit = DateTime.UtcNow - (maxAge ?? TimeSpan.FromHours(24));
        foreach (var kvp in _payloadsByTaskId)
        {
            if (kvp.Value.CreatedAt < limit)
            {
                _payloadsByTaskId.TryRemove(kvp.Key, out _);
            }
        }
        foreach (var kvp in _payloadsByName)
        {
            if (kvp.Value.CreatedAt < limit)
            {
                _payloadsByName.TryRemove(kvp.Key, out _);
            }
        }
    }

    /// <summary>
    /// Clears all stored payloads (primarily for test cleanup).
    /// </summary>
    public static void Clear()
    {
        _payloadsByTaskId.Clear();
        _payloadsByName.Clear();
    }
}
