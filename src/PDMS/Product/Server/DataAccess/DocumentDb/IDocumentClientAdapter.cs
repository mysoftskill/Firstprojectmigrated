namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;

    /// <summary>
    /// An adapter for document client APIs that are not available in the IDocumentClient interface.
    /// </summary>
    public interface IDocumentClientAdapter
    {
        /// <summary>
        /// Gets the partition information of the collection.
        /// </summary>
        /// <param name="collectionUri">The collection uri.</param>
        /// <returns>The partition key ranges.</returns>
        Task<FeedResponse<PartitionKeyRange>> ReadPartitionKeyRangeFeedAsync(Uri collectionUri);

        /// <summary>
        /// Creates the change feed query object.
        /// </summary>
        /// <param name="collectionUri">The collection uri.</param>
        /// <param name="feedOptions">The change feed options.</param>
        /// <returns>The document query.</returns>
        IDocumentQuery<Document> CreateDocumentChangeFeedQuery(Uri collectionUri, ChangeFeedOptions feedOptions);
    }
}