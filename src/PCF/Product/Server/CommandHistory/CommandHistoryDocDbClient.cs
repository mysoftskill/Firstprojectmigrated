namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandHistory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    internal class CommandHistoryDocDbClient : ICommandHistoryDocDbClient
    {
        private const string DatabaseName = "CommandHistoryDb";
        private const string CollectionName = "CommandHistory";

        private static readonly Uri DatabaseUri = UriFactory.CreateDatabaseUri(DatabaseName);
        private static readonly Uri CollectionUri = UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName);

        private readonly DocumentClient documentClient;

        public CommandHistoryDocDbClient()
        {
            this.documentClient = new DocumentClient(
                new Uri(Config.Instance.CommandHistory.Uri),
                Config.Instance.CommandHistory.Key,
                DocumentClientHelpers.CreateConnectionPolicy(maxRetryAttemptsOnThrottledRequests: 5),
                ConsistencyLevel.Eventual);
        }

        public async Task InitializeAsync()
        {
            var accountInfo = await this.documentClient.GetDatabaseAccountAsync();

            var writeLocations = accountInfo.WritableLocations.Select(x => x.Name);
            var readLocations = accountInfo.ReadableLocations.Select(x => x.Name).Except(writeLocations).ToList();

            // If we have multiple read locations, shuffle them so that we spread load around.
            while (readLocations.Count > 0)
            {
                string location = RandomHelper.TakeElement(readLocations);
                this.documentClient.ConnectionPolicy.PreferredLocations.Add(location);
                readLocations.Remove(location);
            }

            foreach (var writeLocation in writeLocations)
            {
                this.documentClient.ConnectionPolicy.PreferredLocations.Add(writeLocation);
            }

            // create the database, if appropriate
            await this.documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = DatabaseName });

            var collection = new DocumentCollection
            {
                Id = CollectionName,
                DefaultTimeToLive = (int)TimeSpan.FromDays(Config.Instance.CommandHistory.DefaultTimeToLiveDays).TotalSeconds
            };

            // Cold storage doesn't need much fancy partitioning; 
            // we can get away with just using the "ID" of the document
            // as a partition key.
            collection.PartitionKey.Paths.Add("/id");

            await this.documentClient.CreateDocumentCollectionIfNotExistsAsync(
                DatabaseUri,
                collection,
                new RequestOptions { OfferEnableRUPerMinuteThroughput = true, OfferThroughput = Config.Instance.CommandHistory.DefaultRUProvisioning });
        }

        public Task<CoreCommandDocument> PointQueryAsync(CommandId commandId)
        {
            return this.documentClient.InstrumentedReadDocumentAsync<CoreCommandDocument>(
                UriFactory.CreateDocumentUri(DatabaseName, CollectionName, commandId.Value),
                "CommandHistory",
                "CommandHistory",
                commandId.Value,
                expectNotFound: true,
                requestOptions: new RequestOptions { PartitionKey = new PartitionKey(commandId.Value) });
        }

        public async Task<(IEnumerable<CoreCommandDocument> documents, string continuationToken)> CrossPartitionQueryAsync(SqlQuerySpec query, string continuationToken, int maxItemCount = 1000)
        {
            var feedOptions = new FeedOptions
            {
                EnableCrossPartitionQuery = true,
                RequestContinuation = continuationToken,
                MaxItemCount = maxItemCount
            };

            var documentQuery = this.documentClient
                .CreateDocumentQuery<CoreCommandDocument>(CollectionUri, query, feedOptions)
                .AsDocumentQuery();

            var result = await documentQuery.InstrumentedExecuteNextAsync("CommandHistory", "CommandHistory");

            return result;
        }

        public async Task<(IEnumerable<CoreCommandDocument> documents, string continuationToken)> MaxParallelismCrossPartitionQueryAsync(SqlQuerySpec query, string continuationToken)
        {
            var feedOptions = new FeedOptions
            {
                EnableCrossPartitionQuery = true,
                //     MaxDegreeOfParallelism: Number of concurrent operations run client side during parallel
                //     query execution in the Azure Cosmos DB service. A positive property value limits
                //     the number of concurrent operations to the set value. If it is set to less than
                //     0, the system automatically decides the number of concurrent operations to run.
                MaxDegreeOfParallelism = -1,
                RequestContinuation = continuationToken,
                MaxItemCount = 1000,
            };

            var documentQuery = this.documentClient
                .CreateDocumentQuery<CoreCommandDocument>(CollectionUri, query, feedOptions)
                .AsDocumentQuery();

            var result = await documentQuery.InstrumentedExecuteNextAsync("CommandHistory", "CommandHistory");

            return result;
        }

        public Task InsertAsync(CoreCommandDocument document)
        {
            return this.documentClient.InstrumentedCreateDocumentAsync(
                CollectionUri,
                "CommandHistory",
                "CommandHistory",
                document,
                expectConflicts: true);
        }

        public Task ReplaceAsync(CoreCommandDocument document, string etag)
        {
            var requestOptions = new RequestOptions
            {
                AccessCondition = new AccessCondition
                {
                    Type = AccessConditionType.IfMatch,
                    Condition = etag,
                },
            };

            return this.documentClient.InstrumentedReplaceDocumentAsync(
                UriFactory.CreateDocumentUri(DatabaseName, CollectionName, document.Id),
                DatabaseName,
                CollectionName,
                document,
                expectConflicts: true,
                requestOptions: requestOptions,
                extraLogging: (ev, r) =>
                {
                    ev["CommandId"] = document.Id;
                });
        }
    }
}
