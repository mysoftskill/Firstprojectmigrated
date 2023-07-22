
namespace Microsoft.Azure.ComplianceServices.Common
{
    using System;
    using StackExchange.Redis;

    /// <summary>
    /// Interface for Redis connection
    /// </summary>
    public interface IRedisConnection : IDisposable
    {
        /// <summary>
        /// Obtain an interactive connection to a database inside redis
        /// </summary>
        /// <param name="dbId">The ID to get a database for.</param>
        /// <returns>The connection to the specified database</returns>
        IDatabase GetDatabase(int dbId);

        /// <summary>
        /// Force a new ConnectionMultiplexer to be created.
        /// </summary>
        void ForceReconnect();
    }
}
