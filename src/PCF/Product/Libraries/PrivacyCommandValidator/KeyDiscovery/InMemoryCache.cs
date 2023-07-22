namespace Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <inheritdoc />
    /// <summary>
    /// Implements ICache and provides inMemory caching
    /// </summary>
    public class InMemoryCache : ICache
    {
        private static readonly ConcurrentDictionary<string, CacheItem> CachedKeys = new ConcurrentDictionary<string, CacheItem>();

        /// <inheritdoc />
        public Task<CacheItem> ReadAsync(string keyId)
        {
            if (CachedKeys.TryGetValue(keyId, out CacheItem cacheItem) && cacheItem.Expiration.CompareTo(DateTimeOffset.UtcNow) > 0)
            {
                return Task.FromResult(cacheItem);
            }

            // Try remove the expired item
            CachedKeys.TryRemove(keyId, out cacheItem);
            return Task.FromResult<CacheItem>(null);
        }

        /// <inheritdoc />
        public Task WriteAsync(IDictionary<string, CacheItem> items, CancellationToken cancellationToken)
        {
            if (items == null || items.Count == 0)
            {
                return Task.FromResult(true);
            }

            foreach (string key in items.Keys)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                CachedKeys[key] = items[key];
            }

            return Task.FromResult(true);
        }
    }
}
