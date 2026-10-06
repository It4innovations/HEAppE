using System;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace HEAppE.Utils;

public static class CacheUtils
{
    // Atomic cache version (epoch) for cluster and command template cache entries.
    // Incrementing this version instantly and cleanly invalidates cached items without wiping the entire memory cache.
    private static long _clusterCacheVersion = 1;

    public static long ClusterCacheVersion => Volatile.Read(ref _clusterCacheVersion);

    // Global token for general invalidation
    private static CancellationTokenSource _globalResetToken = new();

    // Domain-specific tokens for granular cache invalidation
    private static CancellationTokenSource _clusterInfoResetToken = new();
    private static CancellationTokenSource _userPermissionsResetToken = new();

    public static CancellationToken GlobalResetToken => _globalResetToken.Token;
    public static CancellationToken ClusterInfoResetToken => _clusterInfoResetToken.Token;
    public static CancellationToken UserPermissionsResetToken => _userPermissionsResetToken.Token;

    /// <summary>
    /// Removes a specific key from the cache with logging.
    /// </summary>
    public static void RemoveKeyFromCache(this IMemoryCache _cacheProvider, ILogger _logger, string key,
        string calledMethodName)
    {
        _logger.LogDebug($"Endpoint: \"Management\" Method: \"{calledMethodName}\"");
        _logger.LogDebug($"Removing cache key: \"{key}\"");
        _cacheProvider.Remove(key);
    }

    /// <summary>
    /// Invalidates cluster & command template cache entries by incrementing the cache epoch version.
    /// Existing cache entries for the old version become unreachable and expire naturally.
    /// </summary>
    public static void InvalidateClusterCache(ILogger logger, IMemoryCache cache = null)
    {
        var newVersion = Interlocked.Increment(ref _clusterCacheVersion);
        logger.LogDebug("Cluster and CommandTemplate cache invalidated via epoch increment. New cache version: {Version}", newVersion);
    }

    /// <summary>
    /// Invalidates user permissions and project assignment cache entries.
    /// </summary>
    public static void InvalidateUserCache(ILogger logger)
    {
        logger.LogDebug("Invalidating User permissions and project assignment cache entries.");
        var oldTokenSource = Interlocked.Exchange(ref _userPermissionsResetToken, new CancellationTokenSource());
        try
        {
            oldTokenSource.Cancel();
        }
        finally
        {
            oldTokenSource.Dispose();
        }
    }

    /// <summary>
    /// Invalidates all cache entries by incrementing cache versions and cancelling reset tokens.
    /// </summary>
    public static void InvalidateAllCache(ILogger logger)
    {
        logger.LogDebug("Invalidating ALL cache entries.");
        InvalidateClusterCache(logger);
        InvalidateUserCache(logger);

        var oldTokenSource = Interlocked.Exchange(ref _globalResetToken, new CancellationTokenSource());
        try
        {
            oldTokenSource.Cancel();
        }
        finally
        {
            oldTokenSource.Dispose();
        }
    }

    /// <summary>
    /// Adds a global invalidation token to the cache entry.
    /// </summary>
    public static void AddGlobalInvalidation(ICacheEntry entry)
    {
        entry.AddExpirationToken(new CancellationChangeToken(_globalResetToken.Token));
    }

    /// <summary>
    /// Adds a cluster-scoped invalidation token to the cache entry.
    /// Obsolete: use CacheUtils.ClusterCacheVersion in cache keys instead to avoid CancellationTokenRegistration leaks.
    /// </summary>
    [System.Obsolete("Use CacheUtils.ClusterCacheVersion in cache keys instead of cancellation change tokens.")]
    public static void AddClusterInvalidation(ICacheEntry entry)
    {
        // No-op to prevent CancellationTokenSource CallbackNode memory leak
    }

    /// <summary>
    /// Adds a cluster-scoped invalidation token to the cache entry options.
    /// Obsolete: use CacheUtils.ClusterCacheVersion in cache keys instead to avoid CancellationTokenRegistration leaks.
    /// </summary>
    [System.Obsolete("Use CacheUtils.ClusterCacheVersion in cache keys instead of cancellation change tokens.")]
    public static void AddClusterInvalidation(MemoryCacheEntryOptions options)
    {
        // No-op to prevent CancellationTokenSource CallbackNode memory leak
    }

    /// <summary>
    /// Calculates an adaptive TTL for caching tokens with a dynamic safety buffer.
    /// Formula: Buffer = min(maxBufferSeconds, max(minBufferSeconds, totalDurationSeconds * ratio))
    /// Usable TTL = totalDuration - Buffer
    /// </summary>
    /// <param name="totalDuration">Total token lifetime or remaining lifetime.</param>
    /// <param name="ratio">Ratio of lifetime to reserve as buffer (default: 0.15, i.e. 15%).</param>
    /// <param name="minBufferSeconds">Minimum buffer in seconds (default: 5s).</param>
    /// <param name="maxBufferSeconds">Maximum buffer in seconds (default: 60s).</param>
    /// <returns>Usable cache TTL with safety buffer applied, or TimeSpan.Zero if duration is insufficient.</returns>
    public static TimeSpan CalculateAdaptiveTtl(TimeSpan totalDuration, double ratio = 0.15, double minBufferSeconds = 5, double maxBufferSeconds = 60)
    {
        if (totalDuration <= TimeSpan.Zero)
            return TimeSpan.Zero;

        var totalSeconds = totalDuration.TotalSeconds;
        var bufferSeconds = Math.Min(maxBufferSeconds, Math.Max(minBufferSeconds, totalSeconds * ratio));
        var usableSeconds = totalSeconds - bufferSeconds;

        return usableSeconds > 0 ? TimeSpan.FromSeconds(usableSeconds) : TimeSpan.Zero;
    }

    /// <summary>
    /// Calculates an adaptive TTL from an expiration DateTime (UTC).
    /// </summary>
    public static TimeSpan CalculateAdaptiveTtl(DateTime expirationUtc, double ratio = 0.15, double minBufferSeconds = 5, double maxBufferSeconds = 60)
    {
        var remaining = expirationUtc - DateTime.UtcNow;
        return CalculateAdaptiveTtl(remaining, ratio, minBufferSeconds, maxBufferSeconds);
    }

    /// <summary>
    /// Calculates an adaptive TTL from an integer expires_in (seconds).
    /// </summary>
    public static TimeSpan CalculateAdaptiveTtl(int expiresInSeconds, double ratio = 0.15, double minBufferSeconds = 5, double maxBufferSeconds = 60)
    {
        return CalculateAdaptiveTtl(TimeSpan.FromSeconds(expiresInSeconds), ratio, minBufferSeconds, maxBufferSeconds);
    }
}