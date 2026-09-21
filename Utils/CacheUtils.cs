using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Threading;

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
}