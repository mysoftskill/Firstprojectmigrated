using CacheManager.Core;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue.Cache
{
    /// <summary>
    /// CustomInMemoryCache is a wrapper around MemoryCache that supports setting cache entry size.
    /// This custom cache class provides an easy way to manage cache entries with specific MemoryCacheEntryOptions,
    /// like setting size, sliding, and absolute expiration, etc.
    /// </summary>
    /// <typeparam name="T">The type of object to be stored in the cache.</typeparam>
    public class CustomInMemoryCache<T>
    {
        private readonly MemoryCache _memoryCache;
        private readonly MemoryCacheEntryOptions _cacheEntryOptions;

        /// <summary>
        /// Initializes a new instance of the CustomMemoryCache class.
        /// </summary>
        /// <param name="options">An instance of MemoryCacheOptions to configure the cache.</param>
        /// <param name="cacheEntryOptions">An instance of MemoryCacheEntryOptions to be applied when adding cache entries.</param>
        public CustomInMemoryCache(MemoryCacheOptions options, MemoryCacheEntryOptions cacheEntryOptions)
        {
            _memoryCache = new MemoryCache(options);
            _cacheEntryOptions = cacheEntryOptions;
        }

        /// <summary>
        /// Adds a cache entry with the provided key and value, applying the MemoryCacheEntryOptions.
        /// </summary>
        /// <param name="key">The key of the cache entry.</param>
        /// <param name="value">The value of the cache entry.</param>
        public void Add(string key, T value)
        {
            _memoryCache.Set(key, value, _cacheEntryOptions);
        }

        /// <summary>
        /// Retrieves a cache entry by the provided key.
        /// </summary>
        /// <param name="key">The key of the cache entry to retrieve.</param>
        /// <returns>The value of the cache entry if found; otherwise, default(T).</returns>
        public T Get(string key)
        {
            return _memoryCache.Get<T>(key);
        }
    }
}
