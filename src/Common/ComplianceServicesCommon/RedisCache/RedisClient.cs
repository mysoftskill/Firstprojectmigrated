
namespace Microsoft.Azure.ComplianceServices.Common
{
    using Microsoft.PrivacyServices.Common.Azure;
    using StackExchange.Redis;
    using System;
    using System.Net.Sockets;

    /// <summary>
    /// A client class performs the actual Redis calls
    /// </summary>
    public class RedisClient : IRedisClient
    {
        private readonly IRedisConnection redisConnection;

        private readonly ILogger logger;

        private int dbId = (int)RedisDatabaseId.Default;

        public RedisClient(IRedisConnection redisConnection, ILogger logger)
        {
            this.redisConnection = redisConnection;
            this.logger = logger;
        }

        /// <inheritdoc />
        public void SetDatabaseNumber(RedisDatabaseId dbId)
        {
            this.dbId = (int)dbId;
        }

        /// <inheritdoc />
        public DateTime GetDataTime(string key)
        {
            var value = StringGet(key);
            return value.HasValue ? DateTime.FromFileTimeUtc((long)value) : default;
        }

        /// <inheritdoc />
        public bool SetDataTime(string key, DateTime value, TimeSpan? expiry = null)
        {
            return StringSet(key, value.ToFileTimeUtc(), expiry);
        }

        /// <inheritdoc />
        public string GetString(string key)
        {
            var value = StringGet(key);
            return value.HasValue ? value : default;
        }

        /// <inheritdoc />
        public bool SetString(string key, string value, TimeSpan? expiry = null)
        {
            return StringSet(key, value, expiry);
        }

        /// <summary>
        /// A pass-though call to IDatabase.StringGet with try/catch for reconnection
        /// </summary>
        /// <param name="key">The key of the string.</param>
        /// <param name="flags">The flags to use for this operation.</param>
        /// <returns>The value of key, or default when key does not exist.</returns>
        private RedisValue StringGet(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            try
            {
                var cache = this.redisConnection.GetDatabase(dbId);
                if (cache == null)
                {
                    logger.Error(nameof(RedisClient), $"RedisCache GetDatabase for {dbId} is null.");
                    return default;
                }

                return cache.StringGet(key, flags);
            }
            catch (Exception ex) when (ex is RedisConnectionException || ex is SocketException)
            {
                logger.Error(nameof(RedisClient), ex, "Exception in StringGet");
                redisConnection.ForceReconnect();
            }
            catch (ObjectDisposedException ex)
            {
                logger.Error(nameof(RedisClient), ex, "ObjectDisposedException in StringGet");
            }

            return default;
        }

        /// <summary>
        /// A pass-though call to IDatabase.StringSet with try/catch for reconnection
        /// </summary>
        /// <param name="key">The key of the string.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="expiry">The expiry to set (defaults to never).</param>
        /// <param name="when">Which condition to set the value under (detaults to always).</param>
        /// <param name="flags">The flags to use for this operation.</param>
        /// <returns>True if the string was set, false otherwise.</returns>
        private bool StringSet(RedisKey key, RedisValue value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            try
            {
                var cache = this.redisConnection.GetDatabase(dbId);
                if (cache == null)
                {
                    logger.Error(nameof(RedisClient), $"RedisCache GetDatabase for {dbId} is null.");
                    return default;
                }

                return cache.StringSet(key, value, expiry, when, flags);
            }
            catch (Exception ex) when (ex is RedisConnectionException || ex is SocketException)
            {
                logger.Error(nameof(RedisClient), ex, "Exception in StringSet");
                redisConnection.ForceReconnect();
            }
            catch (ObjectDisposedException ex)
            {
                logger.Error(nameof(RedisClient), ex, "ObjectDisposedException in StringSet");
            }

            return default;
        }
    }
}
