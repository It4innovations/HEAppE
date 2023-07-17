using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HEAppE.Utils
{
    public static class CacheUtils
    {
        public static void RemoveKeyFromCache(this IMemoryCache _cacheProvider, ILogger _logger, string key, string calledMethodName)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"{calledMethodName}\"");
            _logger.LogDebug($"Removing cache key: \"{key}\"");
            _cacheProvider.Remove(key);
        }
    }
}
