namespace Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using global::Azure;
    using global::Azure.Identity;
    using global::Azure.Monitor.Query;
    using global::Azure.Monitor.Query.Models;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Helpers;
    using Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.Repository;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json;

    public class CosmosDbPartitionSizeWorker : WorkerTaskBase<object, CosmosDbPartitionSizeWorker.LockState>
    {
        // The Azure Log Analytics workspace where we populate CosmosDb partition size to
        // /subscriptions/b4b176cf-fe78-4b59-bd1a-9b8c11536f4d/resourcegroups/pcf-ci1/providers/microsoft.operationalinsights/workspaces/adgcsloganalitics
        private readonly string workspaceId;

        // Which resource group to query (pcf-ppe/pcf-prod)
        private readonly string dbResourceGroup;

        private readonly TimeSpan workerFrequency;
        private readonly TimeSpan globalFrequency;

        private readonly IRedisClient redisClient;
        private readonly ITelemetryRepository telemetryRepository;

        public CosmosDbPartitionSizeWorker(IRedisClient redisClient, ITelemetryRepository telemetryRepository, string workspaceId, string dbResourceGroup, ITaskConfiguration taskConfig, string taskName, TimeSpan workerFrequency, TimeSpan globalFrequency)
            : base(taskConfig, taskName, null)
        {
            this.redisClient = redisClient;
            this.telemetryRepository = telemetryRepository;
            this.workspaceId = workspaceId;
            this.dbResourceGroup = dbResourceGroup;
            this.workerFrequency = workerFrequency;
            this.globalFrequency = globalFrequency;
        }

        private async Task QueryAndPublishPartitionSizeAsync()
        {
            string quertText = $@"
AzureDiagnostics
| where ResourceProvider == ""MICROSOFT.DOCUMENTDB"" and Category == ""PartitionKeyStatistics""
| where ResourceGroup == ""{dbResourceGroup}""
| extend name = strcat(partitionKey_s, ""|"", accountName_s, ""|"", databaseName_s, ""|"", collectionName_s)
| summarize max(sizeKb_d) by name
";

            var stopwatch = Stopwatch.StartNew();
            try
            {
                DualLogger.Instance.Information(nameof(CosmosDbPartitionSizeWorker), $"Query partition size");

                var client = new LogsQueryClient(new DefaultAzureCredential());

                var batch = new LogsBatchQuery();

                string queryId = batch.AddWorkspaceQuery(
                    workspaceId,
                    quertText,
                    new QueryTimeRange(TimeSpan.FromHours(1)));

                Response<LogsBatchQueryResultCollection> response = await client.QueryBatchAsync(batch);

                var entries = response.Value.GetResult<AnalyticsQueryResult>(queryId);

                var partitionSizeEntries = new List<CosmosDbPartitionSize>(0);
                foreach (var entry in entries)
                {
                    partitionSizeEntries.Add(entry.ToCosmosDbPartitionSize());
                }

                DualLogger.Instance.Information(nameof(CosmosDbPartitionSizeWorker), $"Partition size result - {partitionSizeEntries.Count} entries");

                await PublishResultToKustoAsync(partitionSizeEntries);
                await PublishResultToRedisAsync(partitionSizeEntries);

            }
            catch (Exception ex)
            {
                // unexpected exception
                DualLogger.Instance.Error(nameof(CosmosDbPartitionSizeWorker), ex, $"exception during QueryDbSizeAsync");
            }
            finally
            {
                DualLogger.Instance.Information(nameof(CosmosDbPartitionSizeWorker), $"Elapsed={stopwatch.Elapsed}");
            }
        }

        private async Task PublishResultToKustoAsync(List<CosmosDbPartitionSize> sizeEntries)
        {
            DualLogger.Instance.Information(nameof(CosmosDbPartitionSizeWorker), $"Publish partition size to kusto");
            await this.telemetryRepository.AddCosmosDbPartitionSizeAsync(sizeEntries);
        }

        private async Task PublishResultToRedisAsync(List<CosmosDbPartitionSize> sizeEntries)
        {
            DualLogger.Instance.Information(nameof(CosmosDbPartitionSizeWorker), $"Publish partition size to Redis");

            // Grouping results by AgentId, AssetGroupId, CollectionId, then set the key as a sorted list of (DbMoniker, PartitionSizeKb)
            var groupedSizes = sizeEntries.GroupBy(
                keySelector: e => new PartitionSizeRedisHelper.PartitionSizeEntryKey { AgentId = e.AgentId, AssetGroupId = e.AssetGroupId, CollectionId = e.CollectionId },
                elementSelector: e => new PartitionSizeRedisHelper.PartitionSizeEntryValue { DbMoniker = e.DbMoniker, PartitionSizeKb = e.PartitionSizeKb },
                resultSelector: (key, value) => new PartitionSizeRedisHelper.PartitionSizeRecord { Key = key, Sizes = value.OrderBy(x => x.PartitionSizeKb).ToList() });

            DualLogger.Instance.Information(nameof(CosmosDbPartitionSizeWorker), $"{groupedSizes.Count()} Redis entries after grouping");

            foreach (var sizeEntry in groupedSizes)
            {
                var value = JsonConvert.SerializeObject(sizeEntry.Sizes);
                this.redisClient.SetString(sizeEntry.Key.RedisCacheKey, value, TimeSpan.FromDays(1));
            }

            this.redisClient.SetDataTime(PartitionSizeRedisHelper.LastRunRedisKey, DateTime.UtcNow);
        }

        protected override IEnumerable<Func<Task>> GetTasksAsync(LockState state, object parameters)
        {
            return new List<Func<Task>>() { this.QueryAndPublishPartitionSizeAsync };
        }

        protected override bool ShouldRun(LockState lockState)
        {
            DateTimeOffset nextTimeToRun = lockState?.NextStartTime ?? DateTimeOffset.MinValue;
            if (nextTimeToRun <= DateTimeOffset.UtcNow)
            {
                var lastRunTime = this.redisClient.GetDataTime(PartitionSizeRedisHelper.LastRunRedisKey);
                return ((lastRunTime == default) || (DateTime.UtcNow - lastRunTime.ToUniversalTime() > this.globalFrequency));
            }

            return false;
        }

        protected override LockState GetFinalLockState(out TimeSpan leaseTime)
        {
            leaseTime = workerFrequency;
            return new LockState { NextStartTime = DateTimeOffset.UtcNow.Add(workerFrequency) };
        }

        public class LockState
        {
            public DateTimeOffset NextStartTime { get; set; }
        }

        /// <summary>
        /// The Azure Analytics Query Result
        /// The property needs to exactly match to the result column including casing
        /// </summary>
        private class AnalyticsQueryResult
        {
            public string name { get; set; }

            public long max_sizeKb_d { get; set; }

            public CosmosDbPartitionSize ToCosmosDbPartitionSize()
            {
                // sample name value: ["f907e72011a6470299cea606319f97f7.e22b027fdb79401c912508591524ae91"]|pcfprod-west-04|db24|aadQueueCollection
                const int GuidStringLength = 32;

                var parts = name.Split('|');

                return new CosmosDbPartitionSize
                {
                    Timestamp = DateTime.UtcNow,
                    AgentId = Guid.Parse(parts[0].Substring(2, GuidStringLength)),
                    AssetGroupId = Guid.Parse(parts[0].Substring(3 + GuidStringLength, GuidStringLength)),
                    DbMoniker = $"{parts[1]}.{parts[2]}",
                    CollectionId = parts[3],
                    PartitionSizeKb = max_sizeKb_d,
                };
            }
        };
    }
}
