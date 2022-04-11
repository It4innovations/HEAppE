using System;

namespace HEAppE.Utils.Caching
{
    /// <summary>
    /// Wrapper for cached value.
    /// </summary>
    /// <typeparam name="T">Type of cached value.</typeparam>
    public class Cached<T>
    {
        /// <summary>
        /// Result of cache loading function.
        /// </summary>
        /// <typeparam name="T">Type of loaded value.</typeparam>
        public class CacheLoadResult
        {
            /// <summary>
            /// Loaded value.
            /// </summary>
            internal T Value { get; }

            /// <summary>
            /// Expiration time in seconds.
            /// </summary>
            internal int ExpiresIn { get; }

            /// <summary>
            /// Create load cache result.
            /// </summary>
            /// <param name="value">Value to be cached.</param>
            /// <param name="expiresIn">Expiration time of the value.</param>
            public CacheLoadResult(T value, int expiresIn) { Value = value; ExpiresIn = expiresIn; }
        }


        private T _cachedValue = default;
        private DateTime _expiresIn;
        private Func<CacheLoadResult> _loadFunction;

        

        public T GetValue() => GetOrLoadValue();

        /// <summary>
        /// Create cached value wrapper with reload function.
        /// </summary>
        /// <param name="reloadValueFunction">Function, which loads the cached value.</param>
        /// <param name="loadImmidietely">True if value should be loaded immidietely.</param>
        public Cached(Func<CacheLoadResult> reloadValueFunction, bool loadImmidietely)
        {
            _loadFunction = reloadValueFunction;

            if (loadImmidietely)
                LoadValue();
        }

        /// <summary>
        /// Load value into the cache and set the _expiresIn field.
        /// </summary>
        private void LoadValue()
        {
            var loadResult = _loadFunction();
            _cachedValue = loadResult.Value;
            _expiresIn = DateTime.Now.AddSeconds(loadResult.ExpiresIn);
        }

        private bool isExpired() => (DateTime.Now > _expiresIn);

        /// <summary>
        /// Get cached value. If the cache is expired reload the value first.
        /// </summary>
        /// <returns>The cached value.</returns>
        private T GetOrLoadValue()
        {
            if (isExpired())
                LoadValue();

            return _cachedValue;
        }

    }
}
