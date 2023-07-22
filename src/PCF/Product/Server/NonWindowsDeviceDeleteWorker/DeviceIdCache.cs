// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.ComplianceServices.NonWindowsDeviceDeleteWorker
{
    using System;
    using System.Runtime.Caching;

    /// <summary>
    /// Device Id Cache with lease expiration.
    /// </summary>
    public class DeviceIdCache : IDeviceIdCache
    {
        private readonly TimeSpan cacheItemExpiration;

        /// <summary>
        /// Create DeviceId Cache with given lease expiration.
        /// </summary>
        /// <param name="cacheItemExpiration">Items added to the cache will be expired (removed) from the cache after this time.</param>
        public DeviceIdCache(TimeSpan cacheItemExpiration)
        {
            this.cacheItemExpiration = cacheItemExpiration;
        }

        /// <inheritdoc />
        public void Add(string deviceId, object value)
        {
            CacheItemPolicy cip = new CacheItemPolicy()
            {
                AbsoluteExpiration = new DateTimeOffset(DateTime.Now.Add(this.cacheItemExpiration))
            };
            MemoryCache.Default.Set(new CacheItem(deviceId, value), cip);
        }

        /// <inheritdoc />
        public bool Contains(string deviceId)
        {
            return MemoryCache.Default.Contains(deviceId);
        }

        /// <inheritdoc />
        public void Update(string deviceId, object value)
        {
            CacheItemPolicy cip = new CacheItemPolicy()
            {
                AbsoluteExpiration = new DateTimeOffset(DateTime.Now.Add(this.cacheItemExpiration))
            };

            // If the specified entry does not exist, it is created. If the specified entry exists, it is updated.
            MemoryCache.Default.Set(new CacheItem(deviceId, value), cip);
        }
    }
}
