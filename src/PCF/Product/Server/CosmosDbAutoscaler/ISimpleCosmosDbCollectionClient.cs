namespace Microsoft.PrivacyServices.CommandFeed.Service.Autoscaler
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Adjusts offers and fetches metrics for CosmosDb collections.
    /// </summary>
    public interface ISimpleCosmosDbCollectionClient
    {
        /// <summary>
        /// Gets a friendly name for the given collection.
        /// </summary>
        string FriendlyName { get; }

        /// <summary>
        /// Gets the current offer for the given collection.
        /// </summary>
        Task<int> GetCurrentThroughputAsync();
        
        /// <summary>
        /// Replaces the current offer.
        /// </summary>
        Task ReplaceThroughputAsync(int throughput);

        /// <summary>
        /// Gets the document collection's partition count asynchronously.
        /// </summary>
        Task<int> GetPartitionCountAsync();
        
        /// <summary>
        /// Gets throttle statistics from the given CosmosDb collection.
        /// </summary>
        /// <param name="endTime">The end time (must be a whole minute).</param>
        /// <returns>The throttle summary.</returns>
        Task<ThrottleSummary> GetThrottleStatsAsync(DateTimeOffset endTime);
    }
}
