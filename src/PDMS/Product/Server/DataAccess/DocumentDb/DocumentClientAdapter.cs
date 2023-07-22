namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;

    /// <summary>
    /// Implements adapter methods for document client.
    /// </summary>
    public class DocumentClientAdapter : IDocumentClientAdapter
    {
        private readonly DocumentClient documentClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentClientAdapter"/> class.
        /// </summary>
        /// <param name="documentClient">The document client to adapt.</param>
        public DocumentClientAdapter(DocumentClient documentClient)
        {
            this.documentClient = documentClient;
        }

        /// <summary>
        /// Gets the partition information of the collection.
        /// </summary>
        /// <param name="collectionUri">The collection uri.</param>
        /// <returns>The partition key ranges.</returns>
        public Task<FeedResponse<PartitionKeyRange>> ReadPartitionKeyRangeFeedAsync(Uri collectionUri)
        {
            return this.documentClient.ReadPartitionKeyRangeFeedAsync(collectionUri);
        }

        /// <summary>
        /// Creates the change feed query object.
        /// </summary>
        /// <param name="collectionUri">The collection uri.</param>
        /// <param name="feedOptions">The change feed options.</param>
        /// <returns>The document query.</returns>
        public IDocumentQuery<Document> CreateDocumentChangeFeedQuery(Uri collectionUri, ChangeFeedOptions feedOptions)
        {
            return this.documentClient.CreateDocumentChangeFeedQuery(collectionUri, feedOptions);
        }
    }
}