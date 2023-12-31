namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// Represents the set of all active CosmosDB collections. This context contains 
    /// one ICosmosDbQueueCollection for every distinct collection in the CosmosDB instances.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CosmosDbContext
    {
        // Each subject type maps to one collection per database instance.
        private readonly Dictionary<SubjectType, List<ICosmosDbQueueCollection>> collections;

        /// <summary>
        /// Initializes a new CosmosDB context instance based on a list of {Moniker, Uri, Key, Weight} tuples.
        /// </summary>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public CosmosDbContext(List<DatabaseConnectionInfo> databases)
        {
            var dictionary = new Dictionary<SubjectType, List<ICosmosDbQueueCollection>>();
            
            foreach (var item in databases)
            {
                var retryClient = new DocumentClient(
                    item.AccountUri,
                    item.AccountKey,
                    DocumentClientHelpers.CreateConnectionPolicy(maxRetryAttemptsOnThrottledRequests: 3, maxRetryWaitTimeInSeconds: 2),
                    ConsistencyLevel.Strong);
                
                var nonRetryClient = new DocumentClient(
                    item.AccountUri,
                    item.AccountKey,
                    DocumentClientHelpers.CreateConnectionPolicy(maxRetryAttemptsOnThrottledRequests: 0),
                    ConsistencyLevel.Strong);

                foreach (SubjectType subjectType in Enum.GetValues(typeof(SubjectType)))
                {
                    if (!dictionary.ContainsKey(subjectType))
                    {
                        dictionary[subjectType] = new List<ICosmosDbQueueCollection>();
                    }
                    
                    dictionary[subjectType].Add(new CosmosDbQueueCollection(item.DatabaseMoniker, item.DatabaseId, retryClient, nonRetryClient, subjectType, item.Weight));
                }
            }

            this.collections = dictionary;
        }

        /// <summary>
        /// Initializes a new CosmosDbContext from the given list of collection / subject type pairs.
        /// </summary>
        public CosmosDbContext(IEnumerable<Tuple<SubjectType, ICosmosDbQueueCollection>> queueCollections)
        {
            this.collections = new Dictionary<SubjectType, List<ICosmosDbQueueCollection>>();

            foreach (var item in queueCollections)
            {
                if (!this.collections.ContainsKey(item.Item1))
                {
                    this.collections[item.Item1] = new List<ICosmosDbQueueCollection>();
                }

                this.collections[item.Item1].Add(item.Item2);
            }
        }

        /// <summary>
        /// Initializes all of the collections in this context.
        /// </summary>
        public Task InitializeAsync()
        {
            // Initialize all collections in parallel.
            return Task.WhenAll(this.collections.SelectMany(x => x.Value).Select(c => c.InitializeAsync()));
        }

        /// <summary>
        /// Builds and initializes a CosmosDB context from the current configuration.
        /// </summary>
        public static async Task<CosmosDbContext> FromConfiguration()
        {
            var ctx = new CosmosDbContext(DatabaseConnectionInfo.GetDatabaseConnectionInfosFromConfig());
            await ctx.InitializeAsync();
            return ctx;
        }

        /// <summary>
        /// Gets the current list of repositories.
        /// </summary>
        public IReadOnlyList<ICosmosDbQueueCollection> GetCollections(SubjectType subjectType)
        {
            if (this.collections.TryGetValue(subjectType, out var value))
            {
                return value;
            }

            return new ICosmosDbQueueCollection[0];
        }
    }
}
