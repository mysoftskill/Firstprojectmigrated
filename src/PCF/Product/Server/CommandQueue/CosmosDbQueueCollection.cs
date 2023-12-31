namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net;
    using System.Runtime.Caching;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.ComplianceServices.Common.Interfaces;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Provides queue semantics on top of a single cosmos DB partitioned collection.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "lifetime object")]
    public class CosmosDbQueueCollection : ICosmosDbQueueCollection
    {
        #region Perf Counters

        private const string CounterNamePrefix = "CosmosDB:";

        private static readonly IPerformanceCounter QueryDocCounter = PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, CounterNamePrefix + "QueryDocument");
        private static readonly IPerformanceCounter ReplaceDocCounter = PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, CounterNamePrefix + "ReplaceDocument");
        private static readonly IPerformanceCounter DeleteDocCounter = PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, CounterNamePrefix + "DeleteDocument");
        private static readonly IPerformanceCounter CreateDocCounter = PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, CounterNamePrefix + "CreateDocument");
        private static readonly IPerformanceCounter UpsertDocCounter = PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, CounterNamePrefix + "UpsertDocument");

        #endregion

        private readonly string disabledFlightId;
        private readonly string databaseId;
        private readonly Uri databaseUri;
        private readonly Uri collectionUri;
        private readonly DocumentClient retryClient;
        private readonly DocumentClient nonRetryClient;
        private readonly CosmosDbStoredProcedureService storedProcedureService;
        
        /// <summary>
        /// Sometimes when we pop from a particular queue, it comes back with no commands. This is a helpful
        /// hint that we shouldn't call it so quickly next time. As a primitive step, add a "blocker" that
        /// prevents us from calling for a minute after an empty response.
        /// </summary>
        private readonly MemoryCache queuePopBlockers;
        private IReadOnlyList<PartitionKeyRangeStatistics> partitionKeyRangeStatistics;

        /// <summary>
        /// Initializes a new instance of the CosmosDbQueueCollection class.
        /// </summary>
        public CosmosDbQueueCollection(
            string databaseMoniker, 
            string databaseId,
            DocumentClient retryClient,
            DocumentClient nonRetryClient,
            SubjectType subjectType,
            int weight)
        {
            this.databaseId = databaseId;
            this.retryClient = retryClient;
            this.nonRetryClient = nonRetryClient;
            this.DatabaseMoniker = databaseMoniker;
            this.SubjectType = subjectType;
            this.Weight = weight;
            this.disabledFlightId = $"{this.DatabaseMoniker}.{this.SubjectType}".ToUpperInvariant();

            this.CollectionId = GetQueueCollectionId(this.SubjectType);
            this.databaseUri = UriFactory.CreateDatabaseUri(this.databaseId);
            this.collectionUri = UriFactory.CreateDocumentCollectionUri(this.databaseId, this.CollectionId);
            this.storedProcedureService = new CosmosDbStoredProcedureService(this.nonRetryClient, this.databaseId, this.DatabaseMoniker, this.CollectionId);
            this.PartitionKeyStatDictionary = new Dictionary<PartitionKey, CosmosDBPartitionKeyStat>();
            this.partitionKeyRangeStatistics = new List<PartitionKeyRangeStatistics>();
            this.queuePopBlockers = new MemoryCache($"{nameof(this.queuePopBlockers)}.{this.DatabaseMoniker}.{this.SubjectType}");
        }

        /// <summary>
        /// The database Moniker. A collection is part of a database, and we reference databases by a Moniker.
        /// </summary>
        public string DatabaseMoniker { get; }

        /// <summary>
        /// The ID of this collection. This is inferred from the subject type.
        /// </summary>
        public string CollectionId { get; }

        /// <summary>
        /// The subject type of this collection.
        /// </summary>
        public SubjectType SubjectType { get; }

        /// <summary>
        /// The relative weight of this collection.
        /// </summary>
        public int Weight { get; }

        /// <summary>
        /// Partition key statistics dictionary.
        /// </summary>
        public Dictionary<PartitionKey, CosmosDBPartitionKeyStat> PartitionKeyStatDictionary { get; private set; }

        /// <summary>
        /// Initializes this collection with retry.
        /// </summary>
        public async Task InitializeAsync()
        {
            async Task RunWithRetry(string step, Func<Task> callback)
            {
                int nextBackoffMs = 1000;
                int attemptCount = 0;
                while (true)
                {
                    try
                    {
                        attemptCount++;
                        await callback();
                        break;
                    }
                    catch (DocumentClientException ex)
                    {
                        // Exponential backoff, with some jitter. Multiply the next backoff times
                        // a random value between 1.5 and 2.5. This keeps all machines from having the same backoff.
                        double multiplier = 1.5 + RandomHelper.NextDouble();
                        nextBackoffMs = (int)(nextBackoffMs * multiplier);

                        if (attemptCount >= 5)
                        {
                            throw;
                        }

                        // Request rate too large.
                        if (ex.StatusCode == (HttpStatusCode)429 || ex.StatusCode == HttpStatusCode.ServiceUnavailable)
                        {
                            int retryAfter = Math.Max((int)ex.RetryAfter.TotalMilliseconds, nextBackoffMs);

                            DualLogger.Instance.Information(nameof(CosmosDbQueueCollection), $"Throttling error during database setup step: {step}; sleeping for {retryAfter}ms");
                            await Task.Delay(retryAfter);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
            
            // TODO: Improve this.
            if (EnvironmentInfo.IsDevBoxEnvironment)
            {
                // Only create if not in AP. This works for dev-box setups.
                await RunWithRetry("CreateDatabase", this.CreateDatabaseAsync);
                await RunWithRetry("CreateCollection", this.CreateCollectionAsync);
            }

            // Create collection if not exists
            await RunWithRetry("CreateCollection", this.CreateCollectionAsync);
            await RunWithRetry("InstallSproc", this.storedProcedureService.InstallAsync);
        }

        /// <summary>
        /// Initializes the collection, installing the stored procedure and
        /// configuring server-side indexing.
        /// </summary>
        private async Task CreateDatabaseAsync()
        {
            // create the database, if appropriate
            await this.retryClient.CreateDatabaseIfNotExistsAsync(new Database { Id = this.databaseId });
        }

        private async Task CreateCollectionAsync()
        {
            var collection = new DocumentCollection
            {
                Id = this.CollectionId
            };

            // For partitioned collections, we must configure the Partition Key
            // Set up "pk" as the partition key.
            collection.PartitionKey.Paths.Add("/pk");

            await this.retryClient.CreateDocumentCollectionIfNotExistsAsync(
                this.databaseUri,
                collection,
                new RequestOptions { OfferEnableRUPerMinuteThroughput = true, OfferThroughput = Config.Instance.CosmosDBQueues.DefaultRUProvisioning });
        }

        /// <summary>
        /// Pop the next set of items off of the named queue (partition key).
        /// </summary>
        /// <param name="requestedLeaseDuration">The duration of the leases.</param>
        /// <param name="partitionKey">The name of the queue.</param>
        /// <param name="maxToPop">The maximum number of items to pop.</param>
        public async Task<List<Document>> PopAsync(TimeSpan requestedLeaseDuration, string partitionKey, int maxToPop)
        {
            this.EnsureNotDisabled();
         
            if (this.queuePopBlockers.Contains(partitionKey))
            {
                return new List<Document>();
            }

            PartitionKey docDbPartitionKey = CreatePartitionKey(partitionKey);
            
            var result = await this.storedProcedureService.PopNextItemsByCompoundKeyAsync(
                docDbPartitionKey, 
                partitionKey, 
                maxToPop, 
                requestedLeaseDuration);

            if (result == null || result.Count == 0)
            {
                // Nothing came back. Let's wait one minute before circling back to this queue.
                this.queuePopBlockers.Add(partitionKey, new object(), DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1));
            }

            return result;
        }

        /// <summary>
        /// Get the queue statistics for the named queue (partition key).
        /// </summary>
        /// <param name="partitionKey">The name of the queue.</param>
        /// <param name="getDetailedStatistics">Indicates whether to fetch detailed statistics.</param>
        /// <param name="token">The cancellation token</param>
        public Task<AgentQueueStatistics> GetQueueStatisticsAsync(string partitionKey, bool getDetailedStatistics, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            this.EnsureNotDisabled();
            return this.storedProcedureService.GetQueueStatAsync(CreatePartitionKey(partitionKey), partitionKey, getDetailedStatistics);
        }

        /// <summary>
        /// Call the AgentQueueFlush for the named queue (partition key).
        /// </summary>
        /// <param name="partitionKey">The name of the queue.</param>
        /// <param name="maxFlushDate">Date up to which the commands need to be flushed</param>
        /// <param name="token">The cancellation token.</param>
        public async Task FlushAgentQueueAsync(string partitionKey, DateTimeOffset maxFlushDate, CancellationToken token)
        {
            this.EnsureNotDisabled();

            int nextBackoffMs = 1000;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var response = this.storedProcedureService.FlushAgentQueueAsync(CreatePartitionKey(partitionKey), partitionKey, maxFlushDate);

                    // call till there are no more commands in the agent queue that match the condition
                    if (response.Result.TotalItems == 0)
                    {
                        return;
                    }

                    // no throttle anymore, reset nextbackoff
                    nextBackoffMs = 1000;
                }
                catch (CommandFeedException ex)
                {
                    if (ex.ErrorCode == CommandFeedInternalErrorCode.NotFound)
                    {
                        // NotFound is expected since Agent might just completed the command. Just continue the flush
                        await Task.Delay(TimeSpan.FromMilliseconds(100));
                        continue;
                    }

                    if (ex.ErrorCode == CommandFeedInternalErrorCode.Throttle)
                    {
                        int retryAfter = (int)(nextBackoffMs * 1.5);
                        await Task.Delay(retryAfter);
                        continue;
                    }

                    throw;
                }

                // Logic to slow processing down based on flighting, if necessary.
                int delayMilliseconds = 100;
                if (FlightingUtilities.IsEnabled(FlightingNames.AgentQueueFlushSprocExecDelay1000ms))
                {
                    delayMilliseconds = 1000;
                }
                else if (FlightingUtilities.IsEnabled(FlightingNames.AgentQueueFlushSprocExecDelay500ms))
                {
                    delayMilliseconds = 500;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(delayMilliseconds));
            }
        }

        /// <summary>
        /// Inserts the command into the collection.
        /// </summary>
        public Task InsertAsync(Document command)
        {
            this.EnsureNotDisabled();

            CreateDocCounter.Increment();

            return this.retryClient.InstrumentedCreateDocumentAsync(
                this.collectionUri,
                this.DatabaseMoniker,
                this.CollectionId,
                command,
                partitionKey: command.GetPropertyValue<string>("pk"),
                expectConflicts: true);
        }

        /// <summary>
        /// Inserts the command into the collection.
        /// </summary>
        public Task UpsertAsync(string partitionKey, Document command)
        {
            this.EnsureNotDisabled();

            UpsertDocCounter.Increment();

            var pk = CreatePartitionKey(partitionKey);

            return this.retryClient.InstrumentedUpsertDocumentAsync(
                this.collectionUri,
                this.DatabaseMoniker,
                this.CollectionId,
                command,
                partitionKey: partitionKey,
                requestOptions: new RequestOptions { PartitionKey = pk });
        }

        /// <summary>
        /// Queries the collection for the command matching the given partition key and command ID.
        /// </summary>
        /// <param name="partitionKey">The name of the queue.</param>
        /// <param name="commandId">The ID of the command.</param>
        public async Task<Document> QueryAsync(string partitionKey, string commandId)
        {
            this.EnsureNotDisabled();

            QueryDocCounter.Increment();

            var pk = CreatePartitionKey(partitionKey);

            var query = this.retryClient
                .CreateDocumentQuery(this.collectionUri, new FeedOptions { PartitionKey = pk })
                .Where(x => x.Id == commandId)
                .AsDocumentQuery();

            var response = await this.retryClient.InstrumentedReadDocumentAsync<Document>(
                UriFactory.CreateDocumentUri(this.databaseId, this.CollectionId, commandId),
                this.DatabaseMoniker,
                this.CollectionId,
                partitionKey: partitionKey,
                expectNotFound: true,
                requestOptions: new RequestOptions { PartitionKey = pk });

            return response;
        }

        /// <summary>
        /// Replaces the given document using an etag check.
        /// </summary>
        public async Task<string> ReplaceAsync(Document replacementDocument, string etag)
        {
            this.EnsureNotDisabled();

            if (string.IsNullOrEmpty(etag))
            {
                // If we pass null/empty for an etag, DocDB will cheerfully
                // assume we meant to not pass an etag at all, so it's
                // important that we check for this condition.
                throw new ArgumentOutOfRangeException(nameof(etag));
            }

            ReplaceDocCounter.Increment();

            AccessCondition condition = new AccessCondition
            {
                Condition = etag,
                Type = AccessConditionType.IfMatch
            };

            var documentUri = UriFactory.CreateDocumentUri(this.databaseId, this.CollectionId, replacementDocument.Id);
            
            var document = await this.retryClient.InstrumentedReplaceDocumentAsync(
                documentUri,
                this.DatabaseMoniker,
                this.CollectionId,
                replacementDocument,
                partitionKey: replacementDocument.GetPropertyValue<string>("pk"),
                expectThrottles: true,
                expectConflicts: true,
                expectNotFound: true,
                requestOptions: new RequestOptions { AccessCondition = condition });

            return document.ETag;
        }

        /// <summary>
        /// Deletes the document in the given partition.
        /// </summary>
        public Task DeleteAsync(string partitionKey, string commandId)
        {
            this.EnsureNotDisabled();

            DeleteDocCounter.Increment();

            var documentUri = UriFactory.CreateDocumentUri(this.databaseId, this.CollectionId, commandId);

            return this.retryClient.InstrumentedDeleteDocumentAsync(
                documentUri, 
                this.DatabaseMoniker, 
                this.CollectionId, 
                partitionKey, 
                expectNotFound: true, 
                expectThrottles: true,
                expectConflicts: true,
                requestOptions: new RequestOptions { PartitionKey = CreatePartitionKey(partitionKey) });
        }

        /// <summary>
        /// Maps subject type to collection name.
        /// </summary>
        public static string GetQueueCollectionId(SubjectType subjectType)
        {
            switch (subjectType)
            {
                case SubjectType.Aad:
                    return "aadQueueCollection";

                case SubjectType.Aad2:
                    return "aad2QueueCollection";

                case SubjectType.Msa:
                    return "msaQueueCollection";

                case SubjectType.Device:
                    return "deviceQueueCollection";

                case SubjectType.NonWindowsDevice:
                    return "nonWinDeviceQueueCollection";

                case SubjectType.Demographic:
                case SubjectType.MicrosoftEmployee:
                    return "demographicQueueCollection";

                case SubjectType.EdgeBrowser:
                    return "edgeBrowserQueueCollection";
            }

            throw new ArgumentOutOfRangeException(nameof(subjectType), $"The value {subjectType} was out of range.");
        }

        /// <summary>
        /// This is reported based on a sub-sampling of partition keys within the partition key range and hence these are approximate. 
        /// If your partition keys are below 1GB of storage, they may not show up in the reported statistics.
        /// This function calculate approximate documents count (qdepth).
        /// </summary>
        public int GetApproximateDocumentsCount()
        {
            // taken by partition keys 
            long takenSizeInKB = 0;

            // approximate number of documents if PK is not listed
            long approximate = 0;

            foreach (var pkStatItem in this.partitionKeyRangeStatistics)
            {
                if (pkStatItem.DocumentCount == 0)
                {
                    continue;
                }

                takenSizeInKB += pkStatItem.PartitionKeyStatistics.Select(x => x.SizeInKB).Sum();
                double averageDocumentSizeInKB = (double)pkStatItem.SizeInKB / pkStatItem.DocumentCount;

                // approximate number of documents if PK is not listed
                long approximateCurrent = (long)((pkStatItem.SizeInKB - takenSizeInKB) / averageDocumentSizeInKB);

                // worst case
                approximate = Math.Max(approximateCurrent, approximate);
            }

            return (int)approximate;
        }

        /// <summary>
        /// Update PartitionKeyRangeStatistics.
        /// </summary>
        public async Task UpdatePartitionKeyRangeStatisticsAsync()
        {
            // todo: wrap this into sll instrumentation
            DocumentCollection collection = await this.nonRetryClient.ReadDocumentCollectionAsync(
                            this.collectionUri,
                            new RequestOptions { PopulatePartitionKeyRangeStatistics = true });

            this.PartitionKeyStatDictionary = GetPartitionKeyStatDictionary(collection.PartitionKeyRangeStatistics);
            this.partitionKeyRangeStatistics = collection.PartitionKeyRangeStatistics;

            DualLogger.Instance.Verbose(nameof(CosmosDbQueueCollection), $"Partition key statistic updated. DatabaseMoniker={this.DatabaseMoniker}, CollectionId={this.CollectionId}");
        }

        /// <summary>
        /// Get command queue command types page using continuation token and page size.
        /// </summary>
        public async Task<(List<PrivacyCommandType> CommandTypeCount, string ContinuationToken)> GetCommandQueueCommandTypesAsync(
            string partitionKey,
            int maxItemCount,
            string continuationToken,
            DateTimeOffset startTime)
        {
            var dbPk = new PartitionKey(partitionKey);
            var feedOptions = new FeedOptions
            {
                MaxItemCount = maxItemCount,
                PartitionKey = dbPk,
                RequestContinuation = continuationToken
            };

            var query = this.retryClient
                .CreateDocumentQuery<StoragePrivacyCommand>(this.collectionUri, feedOptions)
                .Where(x => x.CreatedTime < startTime)
                .Select(x => x.CommandType)
                .AsDocumentQuery();

            var response = await query.InstrumentedExecuteNextAsync<PrivacyCommandType>(
                this.DatabaseMoniker,
                this.CollectionId,
                partitionKey,
                expectThrottles: true);

            return (response.items.ToList(), response.continuation);
        }

        private static PartitionKey CreatePartitionKey(string partitionKey)
        {
            return new PartitionKey(partitionKey);
        }

        private void EnsureNotDisabled()
        {
            bool isDisabled = FlightingUtilities.IsFeatureEnabled(FlightingNames.CosmosDbQueueDisabledDatabases, "MonikerSubject", this.disabledFlightId);
            if (isDisabled)
            {
                throw new CommandFeedException("QueueDB is disabled by flight")
                {
                    IsExpected = false
                };
            }
        }

        /// <summary>
        /// Add collection agent depths to baseline list.
        /// </summary>
        private static Dictionary<PartitionKey, CosmosDBPartitionKeyStat> GetPartitionKeyStatDictionary(
            IReadOnlyList<PartitionKeyRangeStatistics> partitionKeyRangeStatistics)
        {
            Dictionary<PartitionKey, CosmosDBPartitionKeyStat> pkStatDictionary = new Dictionary<PartitionKey, CosmosDBPartitionKeyStat>();

            foreach (var stat in partitionKeyRangeStatistics)
            {
                foreach (var pkStats in stat.PartitionKeyStatistics)
                {
                    pkStatDictionary.Add(pkStats.PartitionKey, new CosmosDBPartitionKeyStat(
                        partitionKey: pkStats.PartitionKey,
                        partitionKeySizeInKB: pkStats.SizeInKB,
                        partitionKeyRangeId: stat.PartitionKeyRangeId,
                        partitionRangeSizeInKB: stat.SizeInKB,
                        partitionRangeDocumentCount: stat.DocumentCount));
                }
            }

            return pkStatDictionary;
        }
    }
}
