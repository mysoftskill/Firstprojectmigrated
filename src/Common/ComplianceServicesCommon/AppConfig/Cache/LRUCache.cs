namespace Microsoft.Azure.ComplianceServices.Common.AppConfig.Cache
{
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// Implements a generic cache with least recently used as the eviction policy.
    /// </summary>
    /// <typeparam name="V"></typeparam>
    public class LruCache<V> : ICache<V>
    {
        private readonly MemoryCache cache;
        private readonly int MinutesUntilExpiration;

        public LruCache() : this(100000, 30)
        {
        }
        
        /// <summary>
        ///  Constructor
        /// </summary>
        /// <param name="capacity">Max number of items in the cache.</param>
        public LruCache(int capacity, int expiration = 30)
        {
            MinutesUntilExpiration = expiration;
            cache = new MemoryCache(new MemoryCacheOptions()
            {
                SizeLimit = capacity,
                CompactionPercentage = .5 // Remove 50% of items when capacity is met.
            }) ;
        }

        /// <inheritdoc />
        public void Reset()
        {
            // Remove all entries.
            cache.Compact(1); 
        }

        /// <inheritdoc />
        public long Count()
        {
            return cache.Count;
        }
        
        /// <inheritdoc />
        public void AddItem(string key, V value)
        {
            cache.Set(key, value, new MemoryCacheEntryOptions()
            {
                Size = 1,
                AbsoluteExpirationRelativeToNow = System.TimeSpan.FromMinutes(MinutesUntilExpiration)
            }) ;
        }

        /// <inheritdoc />
        public bool GetItem(string key, out V item)
        {
            return cache.TryGetValue(key, out item);
        }
    }
}
