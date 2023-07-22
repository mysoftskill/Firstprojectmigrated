using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Osgs.Core.Extensions;
using Microsoft.Osgs.Infra.Cache;
using Microsoft.Osgs.Infra.Cache.Policies;
using Microsoft.Osgs.Infra.Cache.Tracking;

/*
 * This is a local clone of AMC's Product\WebRole\Source\Core\Cms\SimpleMemoryCacheClient.cs.
 * Keep changes minimal, or backport them to AMC.
 */

namespace Microsoft.PrivacyServices.UX.Core.Cache
{
    /// <summary>
    /// A bare-bones in-memory cache implementation based on <see cref="MemoryCache"/>.
    /// </summary>
    public sealed class SimpleMemoryCacheClient : ICacheClient
    {
        private static readonly TraceSource trace = new TraceSource(nameof(SimpleMemoryCacheClient));

        private readonly ObjectCache cache;

        private readonly ICacheTracking cacheTracking;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="memoryLimitMegabytes"></param>
        /// <param name="allowedPhysicalMemoryPercentage"></param>
        /// <param name="pollingInterval"></param>
        /// <param name="cacheTracking"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public SimpleMemoryCacheClient(string name, uint memoryLimitMegabytes, uint allowedPhysicalMemoryPercentage, string pollingInterval, ICacheTracking cacheTracking)
        {
            cache = new MemoryCache(name, new NameValueCollection
            {
                { "CacheMemoryLimitMegabytes", memoryLimitMegabytes.ToString(CultureInfo.InvariantCulture) },
                { "PhysicalMemoryLimitPercentage", allowedPhysicalMemoryPercentage.ToString(CultureInfo.InvariantCulture) },
                { "PollingInterval", pollingInterval }
            });

            this.cacheTracking = cacheTracking ?? throw new ArgumentNullException(nameof(cacheTracking));
        }

        /// <summary>
        /// Constructs an instance of <see cref="SimpleMemoryCacheClient"/> with explicitly provided dependencies
        /// for the purposes of UTs.
        /// </summary>
        /// <param name="cacheTracking">An instance of a cache tracker.</param>
        /// <param name="cache">An instance of an underlying cache object.</param>
        public SimpleMemoryCacheClient(ICacheTracking cacheTracking, ObjectCache cache)
        {
            this.cacheTracking = cacheTracking ?? throw new ArgumentNullException(nameof(cacheTracking));
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// 
        /// </summary>
        ~SimpleMemoryCacheClient()
        {
            Dispose(disposing: false);
        }

        private bool disposed = false;

        private void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }
            disposed = true;

            if (cache is IDisposable disposableCache)
            {
                disposableCache.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool TryInitialize()
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemKey"></param>
        public void RemoveItem(string itemKey)
        {
            cache.Remove(itemKey);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <param name="timeToLive"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task<bool> PutAsync<T>(string key, T item, TimeSpan timeToLive, LoggingContext context = null) where T : class
        {
            return cacheTracking.PutAsync(key, () => PutAsyncImpl(key, item, timeToLive, context));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="operationContext"></param>
        /// <param name="loggingContext"></param>
        /// <returns></returns>
        public Task<T> GetAsync<T>(string key, CacheSmartGetOperationContext operationContext, LoggingContext loggingContext = null) where T : class
        {
            return cacheTracking.GetAsync(key, (getCtx) => GetAsyncImpl<T>(key, operationContext, loggingContext));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="sourceGetter"></param>
        /// <param name="operationContext"></param>
        /// <param name="ct"></param>
        /// <param name="lockTimeout"></param>
        /// <param name="overridePolicy"></param>
        /// <param name="loggingContext"></param>
        /// <returns></returns>
        public async Task<T> SmartGetAsync<T>(string key, Func<CancellationToken, Task<TimeStampedItem<T>>> sourceGetter, CacheSmartGetOperationContext operationContext, CancellationToken ct, TimeSpan lockTimeout, CachePolicy overridePolicy = null, LoggingContext loggingContext = null) where T : class
        {
            //  Try getting the item from cache first.
            var itemCandidate = await GetAsync<TimeStampedItem<T>>(key, operationContext, loggingContext).ConfigureAwait(false);

            if (null == itemCandidate?.CacheItem)
            {
                operationContext.IsCacheMiss = true;

                //  Cache miss - get item from source. Note that tracking is not called - it's done by the get call earlier.
                itemCandidate = await GetAndCacheItemAsync().ConfigureAwait(false);
            }

            return itemCandidate.CacheItem;

            async Task<TimeStampedItem<T>> GetAndCacheItemAsync()
            {
                var newItem = await cacheTracking.SourceGetter(key, () => sourceGetter(ct), loggingContext).ConfigureAwait(false);

                if (null == newItem)
                {
                    //  In some cases cacheTracking returns null as the result. This is OK for operations
                    //  like GET (cache miss is expected), but is fatal, if source getter is failing.
                    //  We cannot recover from that.
                    throw new SimpleMemoryCacheClientException("Cache: Source getter operation had completely failed.");
                }

                if (null != newItem.CacheItem)
                {
                    trace.TraceVerbose("Cache: Got '{0}' from source. Desired expiration time: {1:o}.", key, newItem.ExpirationTime.ToUniversalTime());
                    await PutAsync(key, newItem, newItem.ExpirationTime.ToUniversalTime() - DateTimeOffset.UtcNow, loggingContext).ConfigureAwait(false);
                }
                else
                {
                    trace.TraceWarning($"Cache: Attempted to retrieve '{key}' from the source, but null was returned. This may or may not be desired result.");
                }

                return newItem;
            }
        }

        private Task<T> GetAsyncImpl<T>(string key, CacheSmartGetOperationContext operationContext, LoggingContext loggingContext = null) where T : class
        {
            var cacheItem = cache.Get(key);

            if (null == cacheItem)
            {
                cacheTracking.CacheMiss(key);
            }
            else
            {
                cacheTracking.CacheHit(key);
            }

            return Task.FromResult((T)cacheItem);
        }

        private Task<bool> PutAsyncImpl<T>(string key, T item, TimeSpan timeToLive, LoggingContext context = null) where T : class
        {
            if (null == item)
            {
                return Task.FromResult(false);
            }

            var cacheItemPolicy = new CacheItemPolicy()
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow + timeToLive
            };

            cache.Set(key, item, cacheItemPolicy);
            trace.TraceInformation("Cache: Stored {0} in cache. Expiration time: {1:o}.", key, cacheItemPolicy.AbsoluteExpiration.ToUniversalTime());

            return Task.FromResult(true);
        }
    }
}
