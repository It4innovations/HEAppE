using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Threading;

namespace HEAppE.Utils;

public static class CacheUtils
{
    // Globální token pro invalidaci všech položek
    private static CancellationTokenSource _globalResetToken = new();

    public static CancellationToken GlobalResetToken => _globalResetToken.Token;

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
    /// Invalidates all cache entries by cancelling the global reset token.
    /// </summary>
    public static void InvalidateAllCache(ILogger _logger)
    {
        _logger.LogDebug("Invalidating ALL cache entries via global reset token.");

        _globalResetToken.Cancel();
        _globalResetToken.Dispose();
        _globalResetToken = new CancellationTokenSource();
    }

    /// <summary>
    /// Adds a global invalidation token to the cache entry.
    /// </summary>
    public static void AddGlobalInvalidation(ICacheEntry entry)
    {
        entry.AddExpirationToken(new CancellationChangeToken(_globalResetToken.Token));
    }
}