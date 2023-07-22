namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Telemetry;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Azure Document DB Queue Depth Collection
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    internal class QueueDepthCollectionReader
    {
        private readonly DocumentClient documentClient;
        private readonly DatabaseConnectionInfo databaseConnectionInfo;
        private readonly string collectionId;
        private readonly int maxItemCount;

        /// <summary>
        /// Create instance using database connection info and collection id
        /// </summary>
        public QueueDepthCollectionReader(DatabaseConnectionInfo dbInfo, string collectionId)
        {
            this.maxItemCount = Config.Instance.Telemetry.MaxItemCount;
            this.databaseConnectionInfo = dbInfo;
            this.collectionId = collectionId;
            this.documentClient =  new DocumentClient(
                    dbInfo.AccountUri,
                    dbInfo.AccountKey,
                    DocumentClientHelpers.CreateConnectionPolicy(maxRetryAttemptsOnThrottledRequests: 3, maxRetryWaitTimeInSeconds: 2),
                    ConsistencyLevel.Session);
        }

        /// <summary>
        /// Create instance using database connection info and collection id
        /// </summary>
        public QueueDepthCollectionReader(DocumentClient documentClient, DatabaseConnectionInfo databaseConnectionInfo, string collectionId)
        {
            this.collectionId = collectionId;
            this.documentClient = documentClient;
            this.databaseConnectionInfo = databaseConnectionInfo;
        }

        /// <summary>
        /// Queue depth page size
        /// </summary>
        public int GetMaxItemCount()
        {
            return this.maxItemCount;
        }

        /// <summary>
        /// Document DB Client
        /// </summary>
        public DocumentClient DocumentClient => this.documentClient;

        /// <summary>
        /// Collection id
        /// </summary>
        public string CollectionId => this.collectionId;

        /// <summary>
        /// Database id
        /// </summary>
        public string DatabaseId => this.databaseConnectionInfo.DatabaseId;

        /// <summary>
        /// Database moniker
        /// </summary>
        public string DatabaseMoniker => this.databaseConnectionInfo.DatabaseMoniker;

        /// <summary>
        /// Database account uri
        /// </summary>
        public Uri AccountUri => this.databaseConnectionInfo.AccountUri;

        /// <summary>
        /// Collection uri
        /// </summary>
        public Uri CollectionUri => UriFactory.CreateDocumentCollectionUri(this.DatabaseId, this.CollectionId);

        /// <summary>
        /// Add collection agent depths to baseline list
        /// </summary>
        public async Task AddQueueDepthAsync(
            ConcurrentBag<CollectionQueueDepth> baseline, 
            AgentId agentId, 
            AssetGroupId assetGroupId, 
            string version)
        {
            var items = await this.GetQueueDepthAsync(agentId, assetGroupId, version);

            DualLogger.Instance.Verbose(nameof(QueueDepthCollectionReader), $"Found {items.Select(x => x.CommandsCount).Sum()} commands in DBMoniker={this.DatabaseMoniker}, CollectionId={this.CollectionId}");

            items.ForEach(x =>
            {
                Logger.Instance?.LogQueueDepth(x);

                if (x.CommandsCount > 0)
                {
                    baseline.Add(x);
                }
            });
        }

        /// <summary>
        /// Add collection agent depths to baseline list
        /// </summary>
        public async Task AddQueueDepthAsync(IDataAgentMap dataAgentMap, ConcurrentBag<CollectionQueueDepth> baseline, string version)
        {
            DualLogger.Instance.Information(nameof(QueueDepthCollectionReader), $"AddQueueDepthAsync: DatabaseMoniker={this.DatabaseMoniker}, CollectionId={this.CollectionId}");

            foreach (var agentId in dataAgentMap.GetAgentIds())
            {
                foreach (var assetGroup in dataAgentMap[agentId].AssetGroupInfos)
                {
                    await this.AddQueueDepthAsync(baseline, agentId, assetGroup.AssetGroupId, version);
                }
            }
        }

        /// <summary>
        /// Count collection queue depth
        /// </summary>
        private async Task<List<CollectionQueueDepth>> GetQueueDepthAsync(AgentId agentId, AssetGroupId assetGroupId, string version)
        {
            DualLogger.Instance.Verbose(nameof(QueueDepthCollectionReader), $"DatabaseMoniker={this.DatabaseMoniker}, CollectionId={this.CollectionId}, AgentId={agentId}, AssetGroupId={assetGroupId}");
            Dictionary<PrivacyCommandType, CollectionQueueDepth> commandTypeDepths = new Dictionary<PrivacyCommandType, CollectionQueueDepth>();

            foreach (var commandType in Enum.GetValues(typeof(PrivacyCommandType)).Cast<PrivacyCommandType>())
            {
                commandTypeDepths[commandType] = new CollectionQueueDepth()
                {
                    DbMoniker = this.DatabaseMoniker,
                    CommandType = commandType,
                    BaselineVersion = version,
                    AgentId = agentId,
                    AssetGroupId = assetGroupId,
                    CollectionId = this.CollectionId,
                    StartTime = DateTimeOffset.UtcNow,
                    Timestamp = DateTimeOffset.UtcNow,
                };
            }

            string partitionKey = CosmosDbCommandQueue.CreatePartitionKeyOptimized(agentId, assetGroupId);
            var dbPk = new PartitionKey(partitionKey);

            string continuationToken = null;

            int iter = 0;
            do
            {
                var feedOptions = new FeedOptions
                {
                    MaxItemCount = this.GetMaxItemCount(),
                    PartitionKey = dbPk,
                    RequestContinuation = continuationToken
                };

                var query = this.DocumentClient.CreateDocumentQuery<int>(
                    this.CollectionUri,
                    "select value c.ct from c",
                    feedOptions).AsDocumentQuery();

                int retries = 0;

                while (query.HasMoreResults)
                {
                    try
                    {
                        var response = await query.InstrumentedFeedResponseExecuteNextAsync<int>(
                            this.DatabaseMoniker,
                            this.CollectionId,
                            partitionKey,
                            expectThrottles: true);

                        commandTypeDepths.AsParallel().ForAll(item =>
                        {
                            item.Value.RequestCharge += response.RequestCharge;
                            item.Value.CommandsCount += response.ToList().Where(x => x == (int)item.Key).Count();
                        });

                        continuationToken = response.ResponseContinuation;
                    }
                    catch (CommandFeedException ex)
                    {
                        retries++;

                        if (!ex.IsExpected)
                        {
                            DualLogger.Instance.Error(nameof(QueueDepthCollectionReader), $"Unexpected exception. DatabaseMoniker={this.DatabaseMoniker}, CollectionId={this.CollectionId}, Retry={retries}, ErrorCode={ex.ErrorCode}.");
                            throw;
                        }

                        DualLogger.Instance.Warning(nameof(QueueDepthCollectionReader), $"Database error: {ex.ErrorCode}. DatabaseMoniker={this.DatabaseMoniker}, CollectionId={this.CollectionId}, Retry={retries}, PartitionKey={partitionKey}.");
                        if (retries > Config.Instance.Telemetry.MaxRetries)
                        {
                            DualLogger.Instance.Error(nameof(QueueDepthCollectionReader), $"Give up: number of retries exceed given limit. DatabaseMoniker={this.DatabaseMoniker}, CollectionId={this.CollectionId}, Retry={retries}, ErrorCode={ex.ErrorCode}.");
                            throw;
                        }
                    }

                    if (query.HasMoreResults)
                    {
                        iter++;
                        var sleepMs = Config.Instance.Telemetry.RetryDelayMillisecs * retries;
                        DualLogger.Instance.Verbose(nameof(QueueDepthCollectionReader), $"Sleep {sleepMs} before next run. Iteration={iter}, Retries={retries}, DatabaseMoniker={this.DatabaseMoniker}, CollectionId={this.CollectionId}, PartitionKey={partitionKey}.");
                        await Task.Delay(sleepMs);
                    }
                }

                commandTypeDepths.AsParallel().ForAll(item =>
                {
                    item.Value.Retries += retries;
                });
            }
            while (continuationToken != null);

            commandTypeDepths.AsParallel().ForAll(item =>
            {
                item.Value.Iterations = iter;
                item.Value.EndTime = DateTimeOffset.UtcNow;
            });

            return commandTypeDepths.Values.ToList();
        }
    }
}
