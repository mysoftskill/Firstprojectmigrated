
namespace Microsoft.Azure.ComplianceServices.Common
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A simple in memory hashtable to mimic some of Redis functionalities. 
    /// For local development and testing purpose only.
    /// </summary>
    public class InMemoryRedisClient : IRedisClient
    {
        private readonly object lockObj = new object();

        private readonly Dictionary<string, string> cacheStorage = new Dictionary<string, string>();

        public InMemoryRedisClient()
        {
        }

        /// <inheritdoc />
        public void SetDatabaseNumber(RedisDatabaseId dbId)
        {
            // no-op
        }

        /// <inheritdoc />
        public DateTime GetDataTime(string key)
        {
            lock (lockObj)
            {
                if (cacheStorage.TryGetValue(key, out string value))
                {
                    return DateTime.FromFileTimeUtc(long.Parse(value));
                }
            }

            return default;
        }

        /// <inheritdoc />
        public bool SetDataTime(string key, DateTime value, TimeSpan? expiry = null)
        {
            // TODO: support expiry?
            lock (lockObj)
            {
                cacheStorage[key] = value.ToFileTimeUtc().ToString();
            }

            return true;
        }

        /// <inheritdoc />
        public string GetString(string key)
        {
            lock (lockObj)
            {
                if (cacheStorage.TryGetValue(key, out string value))
                {
                    return value;
                }
            }

            return default;
        }

        /// <inheritdoc />
        public bool SetString(string key, string value, TimeSpan? expiry = null)
        {
            lock (lockObj)
            {
                cacheStorage[key] = value;
            }

            return true;
        }
    }
}
