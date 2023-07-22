namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.QueueDepth
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.Repository;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Azure Queue task handler
    /// Calculates queue depths for given agent task.
    /// </summary>
    public class QueueDepthWorkItemHandler : IAzureWorkItemQueueHandler<QueueDepthWorkItem>
    {
        private readonly ITelemetryRepository telemetryRepository;
        private readonly Dictionary<string, DatabaseConnectionInfo> databaseMonikerDbInfoMap;
        private readonly int minMaxItemCount = 100;
        private readonly int maxMaxItemCount = 1000;
        private readonly ConcurrentDictionary<string, CosmosDbQueueCollection> cosmosDbQueueCollectionMap;

        /// <summary>
        /// Create queue depth task workitem handler
        /// </summary>
        public QueueDepthWorkItemHandler(ITelemetryRepository telemetryRepository)
        {
            this.telemetryRepository = telemetryRepository;
            this.databaseMonikerDbInfoMap = DatabaseConnectionInfo
                .GetDatabaseConnectionInfosFromConfig()
                .ToDictionary(db => db.DatabaseMoniker.ToUpperInvariant(), db => db);

            this.cosmosDbQueueCollectionMap = new ConcurrentDictionary<string, CosmosDbQueueCollection>();
        }

        public SemaphorePriority WorkItemPriority => SemaphorePriority.Normal;

        /// <inheritdoc />
        public async Task<QueueProcessResult> ProcessWorkItemAsync(QueueWorkItemWrapper<QueueDepthWorkItem> wrapper)
        {
            var workItem = wrapper.WorkItem;
            
            if (FlightingUtilities.IsEnabled(FlightingNames.QueueDepthDrainTasks))
            {
                DualLogger.Instance.Warning(nameof(QueueDepthWorkItemHandler), $"Queue depth drain is enabled. Deleting {workItem}.");
                return QueueProcessResult.Success();
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            QueueProcessResult result = QueueProcessResult.Success();

            // set work item start date
            if (workItem.StartTime == DateTimeOffset.MinValue)
            {
                workItem.StartTime = DateTimeOffset.UtcNow;
            }

            try
            {
                IncomingEvent.Current?.SetProperty("BaselineCreateTime", workItem.CreateTime.ToString("o"));
                IncomingEvent.Current?.SetProperty("BaselineStartTime", workItem.StartTime.ToString("o"));
                IncomingEvent.Current?.SetProperty("AgentId", workItem.AgentId.Value);
                IncomingEvent.Current?.SetProperty("AssetGroupId", workItem.AssetGroupId.Value);
                IncomingEvent.Current?.SetProperty("DatabaseMoniker", workItem.DatabaseId);
                IncomingEvent.Current?.SetProperty("CollectionId", CosmosDbQueueCollection.GetQueueCollectionId(workItem.SubjectType));
                IncomingEvent.Current?.SetProperty("MaxItemsCount", workItem.MaxItemsCount.ToString());
                IncomingEvent.Current?.SetProperty("BatchSize", workItem.BatchSize.ToString());
                IncomingEvent.Current?.SetProperty("Retries", workItem.Retries.ToString());
                IncomingEvent.Current?.SetProperty("Iteration", workItem.Iteration.ToString());

                int count = 0;

                var endTime = DateTimeOffset.UtcNow.AddMinutes(1);

                // run in batches
                while (DateTimeOffset.UtcNow < endTime)
                {
                    result = await this.ProcessAsync(workItem);

                    // break if not to continue immediately
                    if (result.Complete || result.Delay != TimeSpan.Zero)
                    {
                        break;
                    }
                }

                IncomingEvent.Current?.SetProperty("BatchCount", count.ToString());

                // increase maxItemCount for next iteration
                if (!result.Complete && result.Delay == TimeSpan.Zero)
                {
                    // increase MaxItemsCount
                    workItem.MaxItemsCount = Math.Min(workItem.MaxItemsCount + 100, this.maxMaxItemCount);

                    // reschedule with some delay
                    result = QueueProcessResult.RetryAfter(TimeSpan.FromSeconds(RandomHelper.Next(1, 60)));
                }
            }
            catch (Exception ex)
            {
                // log the error and complete workitem
                DualLogger.Instance.Error(nameof(QueueDepthWorkItemHandler), ex, $"Fail to process {workItem}.");
                workItem.EndTime = DateTimeOffset.UtcNow;
                await this.telemetryRepository.AddTaskAsync(workItem, TaskActionName.Fail);
                return QueueProcessResult.Success();
            }
            finally
            {
                workItem.EndTime = DateTimeOffset.UtcNow;
                stopwatch.Stop();
                DualLogger.Instance.Verbose(nameof(QueueDepthWorkItemHandler), $"Elapsed={stopwatch.Elapsed}, {workItem}.");
            }

            workItem.EndTime = DateTimeOffset.UtcNow;

            // publish if result is completed
            if (result.Complete)
            {
                await this.telemetryRepository.AddBaselineAsync(workItem);
                await this.telemetryRepository.AddTaskAsync(workItem, TaskActionName.Complete);
            }

            return result;
        }

        /// <summary>
        /// Process QueueDepthSchedulerWorkItem workItem
        /// </summary>
        private async Task<QueueProcessResult> ProcessAsync(QueueDepthWorkItem workItem)
        {
            if (!this.databaseMonikerDbInfoMap.ContainsKey(workItem.DbMoniker.ToUpperInvariant()))
            {
                throw new ArgumentException($"DatabaseId={workItem.DbMoniker} not found.", nameof(workItem));
            }

            QueueProcessResult result = QueueProcessResult.Success();

            Dictionary<PrivacyCommandType, int> counts = new Dictionary<PrivacyCommandType, int>();
            try
            {
                CosmosDbQueueCollection collection = this.GetCosmosDbQueueCollection(workItem);

                var queryResponse = await collection.GetCommandQueueCommandTypesAsync(
                    partitionKey: CosmosDbCommandQueue.CreatePartitionKeyOptimized(workItem.AgentId, workItem.AssetGroupId),
                    maxItemCount: workItem.MaxItemsCount,
                    continuationToken: workItem.ContinuationToken,
                    startTime: workItem.StartTime);

                if (queryResponse.CommandTypeCount.Any())
                {
                    counts = queryResponse.CommandTypeCount.GroupBy(x => x).ToDictionary(x => (PrivacyCommandType)x.Key, x => x.ToList().Count);
                }
                else
                {
                    counts = new Dictionary<PrivacyCommandType, int>();
                }

                // update workitem counts
                foreach (var key in counts.Keys)
                {
                    if (workItem.CommandTypeCountDictionary.ContainsKey(key))
                    {
                        workItem.CommandTypeCountDictionary[key] += counts[key];
                    }
                    else
                    {
                        workItem.CommandTypeCountDictionary.Add(key, counts[key]);
                    }
                }

                workItem.Iteration++;

                if (!string.IsNullOrEmpty(queryResponse.ContinuationToken))
                {
                    // update continuation token and push workitem back to run it immediately
                    workItem.ContinuationToken = queryResponse.ContinuationToken;
                    result = QueueProcessResult.RetryAfter(TimeSpan.Zero);
                }
            }
            catch (CommandFeedException ex) when (ex.ErrorCode == CommandFeedInternalErrorCode.Throttle)
            {
                var message = $"Throttle exception. WorkItem={workItem}";

                if (workItem.MaxItemsCount <= this.minMaxItemCount)
                {
                    workItem.Retries++;
                }

                if (
                    workItem.Retries > Config.Instance.Telemetry.MaxRetries
                    && workItem.MaxItemsCount <= this.minMaxItemCount)
                {
                    // take longer break
                    result = QueueProcessResult.RetryAfter(TimeSpan.FromMinutes(RandomHelper.Next(1, 10)));
                    workItem.Retries = 0;
                }
                else
                {
                    // decrease MaxItemsCount
                    workItem.MaxItemsCount = Math.Max(workItem.MaxItemsCount - 100, this.minMaxItemCount);
                    result = QueueProcessResult.TransientFailureRandomBackoff();
                }

                DualLogger.Instance.Warning(nameof(QueueDepthWorkItemHandler), message);
            }
            catch (HttpRequestException ex)
            {
                // weird why we are getting this?
                DualLogger.Instance.Error(nameof(QueueDepthWorkItemHandler), ex, ex.Message);
                workItem.Retries++;

                if (workItem.Retries > Config.Instance.Telemetry.MaxRetries)
                {
                    throw;
                }

                result = QueueProcessResult.TransientFailureRandomBackoff();
            }
            catch (Exception ex)
            {
                DualLogger.Instance.Error(nameof(QueueDepthWorkItemHandler), ex, ex.Message);
                workItem.Retries++;

                if (workItem.Retries > Config.Instance.Telemetry.MaxRetries)
                {
                    throw;
                }

                result = QueueProcessResult.TransientFailureRandomBackoff();
            }

            return result;
        }

        private CosmosDbQueueCollection GetCosmosDbQueueCollection(QueueDepthWorkItem workItem)
        {
            var key = $"{workItem.DbMoniker}.{workItem.SubjectType}";

            if (!this.cosmosDbQueueCollectionMap.Keys.Contains(key))
            {
                var dbInfo = this.databaseMonikerDbInfoMap[workItem.DbMoniker.ToUpperInvariant()];
                var client = new DocumentClient(
                    dbInfo.AccountUri,
                    dbInfo.AccountKey,
                    DocumentClientHelpers.CreateConnectionPolicy(maxRetryAttemptsOnThrottledRequests: 0),
                    ConsistencyLevel.Strong);

                this.cosmosDbQueueCollectionMap[key] =  new CosmosDbQueueCollection(
                                        dbInfo.DatabaseMoniker,
                                        dbInfo.DatabaseId,
                                        client,
                                        client,
                                        workItem.SubjectType,
                                        dbInfo.Weight);
            }

            return this.cosmosDbQueueCollectionMap[key];
        }
    }
}
