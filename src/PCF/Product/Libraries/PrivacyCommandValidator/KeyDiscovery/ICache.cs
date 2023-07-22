namespace Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the methods for caching
    /// </summary>
    public interface ICache
    {
        /// <summary>
        /// Reads the Key from cache and returns the CacheItem
        /// </summary>
        /// <param name="keyId">The Id of the key being read</param>
        /// <returns>The cached item matching the key. Returns null, otherwise.</returns>
        Task<CacheItem> ReadAsync(string keyId);

        /// <summary>
        /// Writes the given dictionary of KeyId/Key pairs to cache
        /// </summary>
        /// <param name="items">Dictionary of keyId and CacheItem</param>
        /// <param name="cancellationToken">cancellation Token</param>
        /// <returns>Async Task</returns>
        Task WriteAsync(IDictionary<string, CacheItem> items, CancellationToken cancellationToken);
    }
}
