namespace Microsoft.PrivacyServices.DataManagement.Worker.ChangeFeedReader
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb;

    /// <summary>
    /// Implements methods for interacting with the change feed.
    /// </summary>
    public class ChangeFeedReader : IChangeFeedReader
    {
        private readonly IDocumentDatabaseConfig databaseConfiguration;
        private readonly IDocumentClientAdapter documentClient;
        private readonly IDocumentQueryFactory documentQueryFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeFeedReader"/> class.
        /// </summary>
        /// <param name="databaseConfiguration">The database configuration.</param>
        /// <param name="documentClient">The document client.</param>
        /// <param name="documentQueryFactory">The document query factory.</param>
        public ChangeFeedReader(
            IDocumentDatabaseConfig databaseConfiguration,
            IDocumentClientAdapter documentClient,
            IDocumentQueryFactory documentQueryFactory)
        {
            this.documentClient = documentClient;
            this.databaseConfiguration = databaseConfiguration;
            this.documentQueryFactory = documentQueryFactory;
        }

        /// <summary>
        /// Reads a batch of items from the change feed.
        /// </summary>
        /// <param name="continuationToken">The continuation token. If null, starts at the beginning.</param>
        /// <returns>The set of documents.</returns>
        public async Task<IEnumerable<Document>> ReadItemsAsync(string continuationToken = null)
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri(this.databaseConfiguration.DatabaseName, this.databaseConfiguration.EntityCollectionName);

            var partitionKeyResponse = await this.documentClient.ReadPartitionKeyRangeFeedAsync(collectionUri).ConfigureAwait(false);

            // We only have a single partition, so no need to account for multiple.
            var feedOptions = new ChangeFeedOptions
            {
                PartitionKeyRangeId = partitionKeyResponse.First().Id,
                MaxItemCount = ChangeFeedReaderConfig.BatchSize,
                StartFromBeginning = continuationToken == null,
                RequestContinuation = continuationToken
            };

            var query = this.documentClient.CreateDocumentChangeFeedQuery(collectionUri, feedOptions);

            var results = await query.QueryAsync<Document>(this.documentQueryFactory, false).ConfigureAwait(false);

            return results;
        }
    }
}