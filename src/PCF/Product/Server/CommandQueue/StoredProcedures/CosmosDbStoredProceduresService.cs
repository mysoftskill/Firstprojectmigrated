namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.ComplianceServices.Common.Interfaces;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Newtonsoft.Json;

    internal class CosmosDbStoredProcedureService
    {
        #region Perf Counters

        private const string CounterNamePrefix = "CosmosDB:";

        private static readonly IPerformanceCounter ExecSprocCounter = PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, CounterNamePrefix + "ExecuteStoredProcedure");

        #endregion
        
        private const string PopNextItemsByCompoundKeySprocName = "PopNextItemsByCompoundKey2";
        private const string PopNextUnExpiredItemsByCompoundKeySprocName = "PopNextUnExpiredItemsByCompoundKey2";
        private const string GetQueueStatSprocName = "GetQueueStat7";
        private const string FlushAgentQueueStoredProcName = "FlushAgentQueueV1";
        private static readonly TimeSpan MaxCommandLifeSpan = TimeSpan.FromDays(Config.Instance.CommandHistory.DefaultTimeToLiveDays);

        private readonly DocumentClient documentClient;
        private readonly Uri collectionUri;
        private readonly string databaseMoniker;
        private readonly string collectionId;

        /// <summary>
        /// A Colleciton of CosmosDB Stored Procedures
        /// Key is the Stored Procedure name
        /// Tuple item 1 is stored procedure script file name 
        /// Tuple item 2 is stored procedure Uri
        /// </summary>
        private readonly Dictionary<string, Tuple<string, Uri>> storedProcedures;

        /// <summary>
        /// Initializes a new instance of the CosmosDbStoredProcedureService class.
        /// </summary>
        internal CosmosDbStoredProcedureService(DocumentClient client, string databaseId, string databaseMoniker, string collectionId)
        {
            this.documentClient = client;
            this.collectionId = collectionId;
            this.databaseMoniker = databaseMoniker;

            this.collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
            this.storedProcedures = new Dictionary<string, Tuple<string, Uri>>
            {
                {
                    GetQueueStatSprocName,
                    new Tuple<string, Uri>(
                        "GetQueueStat.js", 
                        UriFactory.CreateStoredProcedureUri(databaseId, collectionId, GetQueueStatSprocName))
                },
                {
                    PopNextItemsByCompoundKeySprocName,
                    new Tuple<string, Uri>(
                        "PopFromQueue.js",
                        UriFactory.CreateStoredProcedureUri(databaseId, collectionId, PopNextItemsByCompoundKeySprocName))
                },
                {
                    PopNextUnExpiredItemsByCompoundKeySprocName,
                    new Tuple<string, Uri>(
                        "PopUnExpiredFromQueue.js",
                        UriFactory.CreateStoredProcedureUri(databaseId, collectionId, PopNextUnExpiredItemsByCompoundKeySprocName))
                },
                {
                    FlushAgentQueueStoredProcName,
                    new Tuple<string, Uri>(
                        "FlushAgentQueue.js",
                        UriFactory.CreateStoredProcedureUri(databaseId, collectionId, FlushAgentQueueStoredProcName))
                }
            };
        }

        /// <summary>
        /// Install all stored procedures for this CosmosDB Collection
        /// </summary>
        internal Task InstallAsync()
        {
            // Install all stored procedures in parallel
            return Task.WhenAll(this.storedProcedures.Select(x => this.InstallStoredProcedureAsync(x.Key)));           
        }

        /// <summary>
        /// Execute Store Procedure PopNextItem to pop the next set of items off the queue.
        /// </summary>
        /// <param name="docdbPk">DocDB request options Partition Key</param>
        /// <param name="columnPk">The name of the queue.</param>
        /// <param name="maxToPop">The maximum number of items to pop.</param>
        /// <param name="leaseDuration">The requested lease duration.</param>
        internal async Task<List<Document>> PopNextItemsByCompoundKeyAsync(
            PartitionKey docdbPk, 
            string columnPk, 
            int maxToPop,
            TimeSpan leaseDuration)
        {
            ExecSprocCounter.Increment();
            long currentTimeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            string minCompoundKey = StoragePrivacyCommand.CreateCompoundKey(columnPk, DateTimeOffset.FromUnixTimeSeconds(0));
            string maxCompoundKey = StoragePrivacyCommand.CreateCompoundKey(columnPk, now);
            string minTimespan = now.Subtract(MaxCommandLifeSpan).ToString("yyyy-MM-dd");
            string updateCompoundKey = StoragePrivacyCommand.CreateCompoundKey(columnPk, now + leaseDuration);
            long nextNvtValue = (now + leaseDuration).ToUnixTimeSeconds();

            var response = await this.documentClient.InstrumentedExecuteStoredProcedureAsync<PopItemsResponse>(
                this.storedProcedures[PopNextUnExpiredItemsByCompoundKeySprocName].Item2,
                this.databaseMoniker,
                this.collectionId,
                new dynamic[] { minTimespan, minCompoundKey, maxCompoundKey, updateCompoundKey, nextNvtValue, maxToPop, columnPk },
                partitionKey: columnPk,
                requestOptions: new RequestOptions { PartitionKey = docdbPk, ConsistencyLevel = ConsistencyLevel.Strong },
                expectThrottles: true,
                extraLogging: (ev, docDbResponse) =>
                {
                    ev.CommandIds = string.Join(";", docDbResponse.Response.Items.Select(x => x.Id));
                    ev["MinKey"] = minCompoundKey;
                    ev["MaxKey"] = maxCompoundKey;
                    ev["UpdateKey"] = updateCompoundKey;
                    ev["MinTs"] = minTimespan;
                    ev["LeaseDuration"] = leaseDuration.TotalSeconds.ToString();
                    ev["CommandCount"] = docDbResponse.Response.Items.Count.ToString();
                });

            return response.Items;
        }

        /// <summary>
        /// Execute Store Procedure GetQueueStat to get the agent queue statistic data.
        /// </summary>
        /// <param name="docdbPk">DocDB request options Partition Key</param>
        /// <param name="columnPk">The name of the queue.</param>
        /// <param name="getDetailedStatistics">True to get detailed stats.</param>
        internal async Task<AgentQueueStatistics> GetQueueStatAsync(PartitionKey docdbPk, string columnPk, bool getDetailedStatistics)
        {
            ExecSprocCounter.Increment();

            var response = await this.documentClient.InstrumentedExecuteStoredProcedureAsync<GetQueueStatsResponse>(
                this.storedProcedures[GetQueueStatSprocName].Item2,
                this.databaseMoniker,
                this.collectionId,
                new dynamic[] { getDetailedStatistics, DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                partitionKey: columnPk,
                expectThrottles: true,
                requestOptions: new RequestOptions { PartitionKey = docdbPk, ConsistencyLevel = ConsistencyLevel.Strong });

            DateTimeOffset? minLeaseAvailableTime = null;
            if (response.MinLeaseAvailableUnixSecs != null)
            {
                minLeaseAvailableTime = DateTimeOffset.FromUnixTimeSeconds(response.MinLeaseAvailableUnixSecs.Value);
            }

            return new AgentQueueStatistics
            {
                DbMoniker = this.databaseMoniker,
                MinLeaseAvailableTime = minLeaseAvailableTime,
                MinPendingCommandCreationTime = response.MinPendingCommandCreationTime,                
                QueryDate = DateTime.UtcNow.Date,
                UnleasedCommandCount = response.UnleasedCommandCount,
                PendingCommandCount = response.PendingCommandCount,
            };
        }

        /// <summary>
        /// Execute Store Procedure FlushAgentQueue to get the agent queue data deleted/flushed.
        /// </summary>
        /// <param name="docdbPk">DocDB request options Partition Key</param>
        /// <param name="columnPk">The name of the queue.</param>
        /// <param name="maxFlushDate">Maximum flush date upto which the commands need to be flushed</param>
        internal async Task<AgentQueueFlushResult> FlushAgentQueueAsync(PartitionKey docdbPk, string columnPk, DateTimeOffset maxFlushDate)
        {
            int maxToDelete = 100;

            ExecSprocCounter.Increment();
            var response = await this.documentClient.InstrumentedExecuteStoredProcedureAsync<DeleteFromQueueResponse>(
                this.storedProcedures[FlushAgentQueueStoredProcName].Item2,
                this.databaseMoniker,
                this.collectionId,
                new dynamic[] { maxFlushDate, maxToDelete, columnPk },
                partitionKey: columnPk,
                expectThrottles: true,
                requestOptions: new RequestOptions { PartitionKey = docdbPk, ConsistencyLevel = ConsistencyLevel.Strong });

            return new AgentQueueFlushResult
            {
                ItemsDeleted = response.ItemsDeleted,
                TotalItems = response.TotalItems,
            };
        }

        private async Task InstallStoredProcedureAsync(string sprocName)
        {
            // Install the stored procedure in the collection.
            Assembly current = typeof(CosmosDbQueueCollection).Assembly;
            string resourceName = current.GetManifestResourceNames().Single(x => x.IndexOf(this.storedProcedures[sprocName].Item1) >= 0);

            string sprocText;
            using (var streamReader = new StreamReader(current.GetManifestResourceStream(resourceName)))
            {
                sprocText = streamReader.ReadToEnd();
            }

            try
            {
                await this.documentClient.CreateStoredProcedureAsync(
                    this.collectionUri,
                    new StoredProcedure
                    {
                        Id = sprocName,
                        Body = sprocText
                    });
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode != HttpStatusCode.Conflict)
                {
                    throw;
                }
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        private class PopItemsResponse
        {
            [JsonProperty("items")]
            public List<Document> Items { get; set; }
        }
        
        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        private class GetQueueStatsResponse
        {
            [JsonProperty("minTimestamp")]
            public DateTimeOffset? MinPendingCommandCreationTime { get; set; }

            [JsonProperty("minNextVisibleTime")]
            public long? MinLeaseAvailableUnixSecs { get; set; }
            
            [JsonProperty("pendingCommandCount")]
            public long? PendingCommandCount { get; set; }

            [JsonProperty("unleasedCommandCount")]
            public long? UnleasedCommandCount { get; set; }
        }

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        private class DeleteFromQueueResponse
        {
            [JsonProperty("deleted")]
            public int ItemsDeleted { get; set; }

            [JsonProperty("total")]
            public int TotalItems { get; set; }
        }
    }
}
