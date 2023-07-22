

namespace Microsoft.Azure.ComplianceServices.Common
{
    using System;

    /// <summary>
    /// Interface for Redis client
    /// </summary>
    public interface IRedisClient
    {
        /// <summary>
        /// [Optional] Set which Redis database should be used
        /// </summary>
        /// <param name="dbId">The database number will pass to the GetDatabase call</param>
        void SetDatabaseNumber(RedisDatabaseId dbId);

        // Provides strong-typed StringGet/Set to avoid StackExchange types from bleeding into client
        // DateTimeOffset is the only type needed so far, but more types (int, string) can be added in the future if needed

        /// <summary>
        /// Gets the DataTimeOffset value of key. If the key does not exist the special value default is returned.
        /// </summary>
        /// <param name="key">The key of the string.</param>
        /// <returns>The value of key, or default when key does not exist.</returns>
        DateTime GetDataTime(string key);

        /// <summary>
        /// Sets key to hold the DateTimeOffset value. If key already holds a value, it is overwritten.
        /// </summary>
        /// <param name="key">The key of the string.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="expiry">The expiry to set.</param>
        /// <returns>True if the string was set, false otherwise.</returns>
        bool SetDataTime(string key, DateTime value, TimeSpan? expiry = null);

        /// <summary>
        /// Gets the string value of key. If the key does not exist the special value default is returned.
        /// </summary>
        /// <param name="key">The key of the string.</param>
        /// <returns>The value of key, or default when key does not exist.</returns>
        string GetString(string key);

        /// <summary>
        /// Sets key to hold the string value. If key already holds a value, it is overwritten.
        /// </summary>
        /// <param name="key">The key of the string.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="expiry">The expiry to set.</param>
        /// <returns>True if the string was set, false otherwise.</returns>
        bool SetString(string key, string value, TimeSpan? expiry = null);
    }
}
