namespace Microsoft.PrivacyServices.CommandFeedv2.Common.Storage
{
    using global::Azure.Identity;
    using Microsoft.Azure.Cosmos;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Generic client for interacting with cosmos db.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CosmosDbClient<T> : ICosmosDbClient<T>
    {
        /// <summary>
        /// Defines the cosmos container.
        /// </summary>
        private readonly Container container;

        public CosmosDbClient(string containerName, string dbName, string cosmosEndpoint)
        {
            var clientOption = new CosmosClientOptions
            {
                // Do they need to be configurable?
                MaxRetryAttemptsOnRateLimitedRequests = 5,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(60),
                ConnectionMode = ConnectionMode.Gateway,
                SerializerOptions = new CosmosSerializationOptions()
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            };

            var cosmosClient = new CosmosClient(cosmosEndpoint, new DefaultAzureCredential(), clientOption);
            this.container = cosmosClient.GetContainer(dbName, containerName);
        }

        /// <summary>
        /// Creates an entry in the cosmos db container
        /// </summary>
        /// <param name="entry">The entry to be inserted.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the entry is upserted.</returns>
        public async Task<bool> UpsertEntryAsync(T entry, string partitionKey, CancellationToken cancellationToken = default)
        {
            var itemResponse = await this.container.UpsertItemAsync(entry, new PartitionKey(partitionKey), null, cancellationToken);
            return itemResponse.StatusCode == HttpStatusCode.Created;
        }

        /// <summary>
        /// Reads all items using the query and request options.
        /// </summary>
        /// <param name="queryText">The id for the entry to be read.</param>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of cosmos db records found using the query.</returns>
        public async Task<IList<T>> ReadEntriesAsync(string queryText, string partitionKey = null, CancellationToken cancellationToken = default)
        {
            var result = new List<T>();
            string continuationToken = null;

            // Limit the number of items per iteration to 100
            var queryRequestOptions = new QueryRequestOptions
            {
                MaxItemCount = 100
            };

            // Add the partition key if one is provided as a parameter
            if (!string.IsNullOrEmpty(partitionKey))
            {
                queryRequestOptions.PartitionKey = new PartitionKey(partitionKey);
            }

            using (FeedIterator<T> feedIterator = this.container.GetItemQueryIterator<T>(
                queryText,
                continuationToken,
                queryRequestOptions))
            {

                while (feedIterator.HasMoreResults)
                {
                    var response = await feedIterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                    continuationToken = response.ContinuationToken;
                    result.AddRange(response.AsEnumerable().ToList());
                }
            }

            return result;
        }
    }
}