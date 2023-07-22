namespace Microsoft.PrivacyServices.DataManagement.Worker.ChangeFeedReader
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Azure.Documents;

    /// <summary>
    /// Defines methods for interacting with the change feed.
    /// </summary>
    public interface IChangeFeedReader
    {
        /// <summary>
        /// Reads a batch of items from the change feed.
        /// </summary>
        /// <param name="continuationToken">The continuation token. If null, starts at the beginning.</param>
        /// <returns>The set of documents.</returns>
        Task<IEnumerable<Document>> ReadItemsAsync(string continuationToken = null);
    }
}
