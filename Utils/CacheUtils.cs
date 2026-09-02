using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Threading;

namespace HEAppE.Utils;

public static class CacheUtils
{
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
    /// Invalidates cluster & command template cache entries via cluster reset token and memory cache clearing.
    /// </summary>
    public static void InvalidateClusterCache(ILogger logger, IMemoryCache cache = null)
    {
        logger.LogDebug("Invalidating Cluster and CommandTemplate cache entries via cluster reset token.");
        if (cache is MemoryCache memCache)
        {
            memCache.Clear();
        }
        var oldTokenSource = Interlocked.Exchange(ref _clusterInfoResetToken, new CancellationTokenSource());
        oldTokenSource.Cancel();
        oldTokenSource.Dispose();
    }

    /// <summary>
    /// Invalidates user permissions and project assignment cache entries.
    /// </summary>
    public static void InvalidateUserCache(ILogger logger)
    {
        logger.LogDebug("Invalidating User permissions and project assignment cache entries.");
        var oldTokenSource = Interlocked.Exchange(ref _userPermissionsResetToken, new CancellationTokenSource());
        oldTokenSource.Cancel();
        oldTokenSource.Dispose();
    }

    /// <summary>
    /// Invalidates all cache entries by cancelling all reset tokens.
    /// </summary>
    public static void InvalidateAllCache(ILogger logger)
    {
        logger.LogDebug("Invalidating ALL cache entries via reset tokens.");
        InvalidateClusterCache(logger);
        InvalidateUserCache(logger);

        var oldTokenSource = Interlocked.Exchange(ref _globalResetToken, new CancellationTokenSource());
        oldTokenSource.Cancel();
        oldTokenSource.Dispose();
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
    /// </summary>
    public static void AddClusterInvalidation(ICacheEntry entry)
    {
        entry.AddExpirationToken(new CancellationChangeToken(_clusterInfoResetToken.Token));
    }

    /// <summary>
    /// Adds a cluster-scoped invalidation token to the cache entry options.
    /// </summary>
    public static void AddClusterInvalidation(MemoryCacheEntryOptions options)
    {
        options.AddExpirationToken(new CancellationChangeToken(_clusterInfoResetToken.Token));
    }
}