namespace Microsoft.PrivacyServices.CommandFeedv2.Common.Storage
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Generic client for interacting with cosmos db.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICosmosDbClient<T>
    {
        /// <summary>
        /// Creates an entry in the cosmos db container
        /// </summary>
        /// <param name="entry">The entry to be inserted.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the entry is upserted.</returns>
        Task<bool> UpsertEntryAsync(T entry, string partitionKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads all items using the query and request options.
        /// </summary>
        /// <param name="queryText">The id for the entry to be read.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of cosmos db records found using the query.</returns>
        Task<IList<T>> ReadEntriesAsync(string queryText, string partitionKey = null, CancellationToken cancellationToken = default);
    }
}