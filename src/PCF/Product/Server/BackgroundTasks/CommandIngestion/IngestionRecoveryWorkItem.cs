namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Kusto.Data.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Work item that is triggered to recover partially ingested commands.
    /// </summary>
    public class IngestionRecoveryWorkItem
    {
        /// <summary>
        /// The continuation token for the query to the CommandHistoryRepository.
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// The oldest record creation time to look for partially ingested commands.
        /// </summary>
        public DateTimeOffset OldestRecordCreationTime { get; set; }

        /// <summary>
        /// The newest record creation time to look for partially ingested commands.
        /// </summary>
        public DateTimeOffset NewestRecordCreationTime { get; set; }

        /// <summary>
        /// bool to limit ingestion recovery to only export commands
        /// </summary>
        public bool exportOnly { get; set; }

        /// <summary>
        /// bool to limit ingestion recovery to non export commands
        /// </summary>
        public bool nonExportOnly { get; set; }

        /// <summary>
        /// bool to indicate that the queue item is part of ingestion 
        /// </summary>
        public bool isOnDemandRepairItem { get; set; }
    }

    /// <summary>
    /// Processes IngestionRecoveryWorkItems out of an Azure Queue.
    /// </summary>
    public class IngestionRecoveryWorkItemHandler : IAzureWorkItemQueueHandler<IngestionRecoveryWorkItem>
    {
        private readonly ICommandHistoryRepository commandHistoryRepository;
        private readonly IDataAgentMapFactory dataAgentMapFactory;
        private readonly IAzureWorkItemQueuePublisher<InsertIntoQueueWorkItem> workItemPublisher;
        private readonly IKustoClient kustoClient;

        /// <summary>
        /// The page size. Equivalent to the maximum number of records to return in a single call to CosmosDb.
        /// </summary>
        private const int MaxItemCount = 20;

        /// <summary>
        /// Creates a new handler of IngestionRecoveryWorkItems.
        /// </summary>
        /// <param name="workItemPublisher">The insert into queue item publisher.</param>
        /// <param name="dataAgentMapFactory">The data agent map factory.</param>
        /// <param name="commandHistoryRepository">The Command History repository.</param>
        public IngestionRecoveryWorkItemHandler(
            IAzureWorkItemQueuePublisher<InsertIntoQueueWorkItem> workItemPublisher,
            ICommandHistoryRepository commandHistoryRepository,
            IDataAgentMapFactory dataAgentMapFactory,
            IKustoClient kustoClient
            )
        {
            this.commandHistoryRepository = commandHistoryRepository;
            this.dataAgentMapFactory = dataAgentMapFactory;
            this.workItemPublisher = workItemPublisher;
            this.kustoClient = kustoClient;
        }

        public SemaphorePriority WorkItemPriority => SemaphorePriority.High;

        /// <summary>
        /// Queries CommandHistory, finds signals that failed to be ingested and publishes the work to the replay queue.
        /// If the command history repository returns a continuation token, the handler saves this information
        /// and returns the item back to the queue for continued processing.
        /// </summary>
        public async Task<QueueProcessResult> ProcessWorkItemAsync(QueueWorkItemWrapper<IngestionRecoveryWorkItem> wrapper)
        {
            // return without processing if repair or recovery is disabled
            if (wrapper.WorkItem.isOnDemandRepairItem)
            {
                if (FlightingUtilities.IsEnabled(FlightingNames.IngestionRepairItemsProcessingDisabled))
                {
                    return QueueProcessResult.Success();
                }
            }
            else if (FlightingUtilities.IsEnabled(FlightingNames.IngestionRecoveryItemsProcessingDisabled))
            {
                return QueueProcessResult.Success();
            }

            var workItem = wrapper.WorkItem;
            IEnumerable<CommandHistoryRecord> records;
            string nextContinuationToken;

            // Query
            (records, nextContinuationToken) = await this.commandHistoryRepository.QueryPartiallyIngestedCommandsAsync(
                workItem.OldestRecordCreationTime,
                workItem.NewestRecordCreationTime,
                MaxItemCount,
                workItem.exportOnly,
                workItem.nonExportOnly,
                workItem.ContinuationToken);

            IncomingEvent.Current?.SetProperty("OldestRecordCreationTime", workItem.OldestRecordCreationTime.ToString());
            IncomingEvent.Current?.SetProperty("NewestRecordCreationTime", workItem.NewestRecordCreationTime.ToString());
            IncomingEvent.Current?.SetProperty("ContinuationToken", workItem.ContinuationToken ?? "null");

            // Generate work items
            var insertIntoQueueWorkItems = new List<InsertIntoQueueWorkItem>();
            var recordsWithInvalidRawPxsCommand = new List<string>();

            var BatchSize = 10;
            var recordSize = records.Count();

            for (int i = 0; i < (recordSize / BatchSize) + 1; i++)
            {
                var currentBatch = records.Skip(i * BatchSize).Take(BatchSize);
                var updatedRecords = await UpdateRecordsBasedOnKusto(currentBatch, workItem.OldestRecordCreationTime);

                foreach (CommandHistoryRecord record in updatedRecords)
                {
                    if (string.IsNullOrWhiteSpace(record.Core.RawPxsCommand))
                    {
                        recordsWithInvalidRawPxsCommand.Add(record.CommandId.Value);
                        DualLogger.Instance.Information(nameof(IngestionRecoveryWorkItem), $"Invalid Raw PXS data CommandId={record.CommandId.Value}.");
                        continue;
                    }

                    if (record.Core.IsGloballyComplete == true)
                    {
                        DualLogger.Instance.Information(nameof(IngestionRecoveryWorkItem), $" {record.CommandId} Globally complete, cannot reingest");
                        continue;
                    }

                    DualLogger.Instance.Information(nameof(IngestionRecoveryWorkItem), $"Record CommandId={record.CommandId.Value}.");

                    IDataAgentMap dataAgentMap;
                    if (record.Core.IngestionDataSetVersion.HasValue)
                    {
                        // Pull the version of the data set indicated in the work item. This is important in cases where
                        // this work item fails and another with a potentially different "current" data set picks it up.
                        // This technique allows us to execute idempotently here, which is crucial.
                        dataAgentMap = await this.dataAgentMapFactory.GetDataAgentMapAsync(record.Core.IngestionDataSetVersion.Value);
                    }
                    else
                    {
                        dataAgentMap = this.dataAgentMapFactory.GetDataAgentMap();
                    }

                    var statusRecords = record.StatusMap.Where(kvp => ShouldRecoverRecord(kvp.Key, kvp.Value));

                    var destinations = new List<PxsFilteredCommandDestination>();

                    foreach (var statusRecord in statusRecords)
                    {
                        AgentId agentId = statusRecord.Key.agentId;
                        AssetGroupId assetGroupId = statusRecord.Key.assetGroupId;

                        dataAgentMap[agentId].TryGetAssetGroupInfo(assetGroupId, out IAssetGroupInfo assetGroupInfo);

                        string targetMoniker = statusRecord.Value.StorageAccountMoniker;

                        if (targetMoniker == null || !CommandMonikerHash.GetAllMonikers(record.Core.QueueStorageType).Contains(targetMoniker))
                        {
                            targetMoniker = CommandMonikerHash.GetPreferredMoniker(record.CommandId, assetGroupId, CommandMonikerHash.GetCurrentWeightedMonikers(record.Core.QueueStorageType));
                        }

                        var destination = new PxsFilteredCommandDestination
                        {
                            AgentId = agentId,
                            AssetGroupId = assetGroupId,
                            TargetMoniker = targetMoniker,
                            AssetGroupQualifier = assetGroupInfo.AssetGroupQualifier,
                            DataTypes = assetGroupInfo.SupportedDataTypes.ToList(),
                            QueueStorageType = record.Core.QueueStorageType
                        };

                        destinations.Add(destination);
                        DualLogger.Instance.Information(nameof(IngestionRecoveryWorkItem), $"Record destination: AgentId={destination.AgentId}, AssetGroupId={destination.AssetGroupId}. for commandId={record.CommandId}");
                    }

                    if (!destinations.Any())
                    {
                        continue;
                    }

                    var pxsCommand = JsonConvert.DeserializeObject<JObject>(record.Core.RawPxsCommand);
                    var insertIntoQueueWorkItem = new InsertIntoQueueWorkItem
                    {
                        PxsCommand = pxsCommand,
                        CommandId = record.CommandId,
                        CommandType = record.Core.CommandType,
                        IsIngestionRecovery = true,
                        IsReplayCommand = false,
                        DataSetVersion = record.Core.IngestionDataSetVersion,
                        Destinations = destinations
                    };

                    insertIntoQueueWorkItems.Add(insertIntoQueueWorkItem);
                }

            }
            if (recordsWithInvalidRawPxsCommand.Any())
            {
                IncomingEvent.Current?.SetProperty("RawPxsCommandsNullOrEmptyCommandIds", string.Join(",", recordsWithInvalidRawPxsCommand));
            }

            // Publish
            await this.PublishToQueueAsync(insertIntoQueueWorkItems);
            int destinationsCount = insertIntoQueueWorkItems.Sum(x => x.Destinations.Count);
            IncomingEvent.Current?.SetProperty("TotalNumberOfReplayedDestinations", destinationsCount.ToString());
            DualLogger.Instance.Information(nameof(IngestionRecoveryWorkItem), $"TotalNumberOfReplayedDestinations={destinationsCount}.");

            // Check if we have more pages to process, so update continuation token and retry item.
            if (nextContinuationToken != null)
            {
                workItem.ContinuationToken = nextContinuationToken;
                return QueueProcessResult.RetryAfter(TimeSpan.Zero);
            }

            if (workItem.isOnDemandRepairItem)
            {
                DualLogger.Instance.Information(nameof(IngestionRecoveryWorkItem), $"Completed ingestion repair item with startDate={workItem.OldestRecordCreationTime} " +
                    $"and endDate={workItem.NewestRecordCreationTime} exportIOnly={workItem.exportOnly} and nonExportOnly={workItem.nonExportOnly}");
            }
            else
            {
                DualLogger.Instance.Information(nameof(IngestionRecoveryWorkItem), $"Completed ingestion recovery item with startDate={workItem.OldestRecordCreationTime} " +
                    $"and endDate={workItem.NewestRecordCreationTime} exportIOnly={workItem.exportOnly} and nonExportOnly={workItem.nonExportOnly}");
            }
            // All items processed, complete item.
            return QueueProcessResult.Success();
        }

        /// <summary>
        /// If a record has not been processed (i.e. ingestionTime=completedTime=null) then this method checks the kusto logs to see if
        /// if it was already marked already ingested by agent, completed or force completed. If so, it updates the record accordingly.
        /// These situations occur because the corresponding eventhubs can be backed up and events lost. So the record is not updated.
        /// </summary>
        /// <param name="records"></param>
        /// <param name="oldestCreationTime"></param>
        /// <returns></returns>
        private async Task<IEnumerable<CommandHistoryRecord>> UpdateRecordsBasedOnKusto(IEnumerable<CommandHistoryRecord> records, DateTimeOffset oldestCreationTime)
        {

            // return early if no records are passed in.
            if (records == null)
            {
                DualLogger.Instance.Information(nameof(UpdateRecordsBasedOnKusto), $"No records passed.");
                return records;
            }

            // remove null entires and return early if needed.
            records = records.Where(x => x != null);
            if (records.Count() == 0)
            {
                DualLogger.Instance.Information(nameof(UpdateRecordsBasedOnKusto), $"No records passed.");
                return records;
            }

            // ensure records have not been modified coming into this method
            foreach (var record in records)
            {
                if (record.GetChangedFragments() != CommandHistoryFragmentTypes.None)
                {
                    throw new Exception("Record was modified before livesite fix. This should not happen.");
                }
            }

            // CommandIds are guid's without hyphens in commandhistory, but have hyphens in kusto. We convert them to kusto format here
            // for the query
            var commandIdsString = "(" + string.Join(", ", records.Select(x => $"'{new Guid(x.CommandId.Value)}'")) + ")";

            // Queries to see which records have reached the agent (i.e. sent out a CommandSentToAgentEvent, CommandCompletedEvent) or force completed (i.e. CommandCompletedByPcfEvent)
            // And also queries to see if the record has been marked as completed (i.e. CommandCompletedEvent, CommandCompletedByPcfEvent) so that we don't increase the CommandCompletedCount for those
            // as this 
            string query = $"let commandStarted =PCFLifecycleEventsV2" +
                $"| where TimeStamp between( datetime({oldestCreationTime.ToUniversalTime()}) .. datetime({DateTimeOffset.UtcNow}))" +
                $"| where CommandId in {commandIdsString}" +
                $"| where EventType in ('CommandStartedEvent')" +
                $"| summarize IngestionTime = min(TimeStamp) by AgentId, AssetGroupId, CommandId;" +
                $"let commandCompleted = PCFLifecycleEventsV2" +
                $"| where TimeStamp between( datetime({oldestCreationTime.ToUniversalTime()}) .. datetime({DateTimeOffset.UtcNow}))" +
                $"| where CommandId in {commandIdsString}" +
                $"| where EventType in ('CommandCompletedByPcfEvent', 'CommandCompletedEvent')" +
                $"| summarize CompletedTime = min(TimeStamp) by AgentId, AssetGroupId, CommandId;" +
                $"commandStarted" +
                $"| join kind=leftouter (commandCompleted) on AgentId, AssetGroupId, CommandId" +
                $"| project CommandId, AgentId, AssetGroupId, IngestionTime, CompletedTime";

            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                int numberOfRecordsReadFromKusto = 0;
                using (var reader = await kustoClient.QueryAsync(query, new ClientRequestProperties()))
                {
                    while (reader.Read())
                    {
                        numberOfRecordsReadFromKusto++;
                        var assetgroupIdFromReader = new AssetGroupId((string)reader["AssetGroupId"]);
                        var agentIdFromReader = new AgentId((string)reader["AgentId"]);
                        var commandIdFromReader = new Guid((string)reader["CommandId"]);
                        var kustoKey = (agentIdFromReader, assetgroupIdFromReader);

                        foreach (var record in records)
                        {
                            if (record.StatusMap.ContainsKey(kustoKey) && (record.CommandId.GuidValue == commandIdFromReader))
                            {
                                var status = record.StatusMap[kustoKey];

                                if (status.CompletedTime == null && reader["CompletedTime"] != System.DBNull.Value)
                                {
                                    // not all records are completed and have a completed time, so we need to check for null
                                    var CompletedTime = new DateTimeOffset((DateTime)reader["CompletedTime"]);
                                    if (CompletedTime != null)
                                    {
                                        status.CompletedTime = CompletedTime;
                                        record.Core.CompletedCommandCount++;
                                    }
                                }

                                if (status.IngestionTime == null)
                                {
                                    record.Core.IngestedCommandCount++;
                                    status.IngestionTime = new DateTimeOffset((DateTime)reader["IngestionTime"]);
                                }
                            }
                        }
                    }
                }
                DualLogger.Instance.Information(nameof(UpdateRecordsBasedOnKusto), $"Kusto query successfully completed read {numberOfRecordsReadFromKusto} in {sw.ElapsedMilliseconds} ms.\n with query: {query}");
                sw.Restart();

                int numRecordsFixed = 0;
                // update all records
                foreach (var record in records)
                {
                    if (record.GetChangedFragments() != CommandHistoryFragmentTypes.None)
                    {
                        await commandHistoryRepository.ReplaceAsync(record, record.GetChangedFragments());
                        numRecordsFixed++;
                    }
                }
                DualLogger.Instance.Information(nameof(UpdateRecordsBasedOnKusto), $"{numRecordsFixed} command history items updated in {sw.ElapsedMilliseconds} ms.");

            }
            catch (Exception ex)
            {
                DualLogger.Instance.Error(nameof(UpdateRecordsBasedOnKusto), $"Filtering Failed with the following exception message: {ex.Message}\n stackTrace:{ex.StackTrace} \n query:{query} \n commandIds:{commandIdsString}");
                throw;
            }

            return records;
        }

        /// <summary>
        /// Determines if a record for an agentId and assetGroupId combination should be recovered.
        /// </summary>
        /// <param name="key">An agentId and assetgroupId tuple.</param>
        /// <param name="record">The command history status record.</param>
        /// <returns>True if record should be recovered.</returns>
        internal static bool ShouldRecoverRecord((AgentId agentId, AssetGroupId assetGroupId) key, CommandHistoryAssetGroupStatusRecord record)
        {
            if (FlightingUtilities.IsIngestionBlockedForAgentId(key.agentId))
            {
                IncomingEvent.Current?.SetProperty("IngestionBlockedForAgentId", key.agentId.ToString());
                return false;
            }

            if (FlightingUtilities.IsIngestionBlockedForAssetGroupId(key.assetGroupId))
            {
                IncomingEvent.Current?.SetProperty("IngestionBlockedForAssetGroupId", key.assetGroupId.ToString());
                return false;
            }

            if (record.IngestionTime == null && record.CompletedTime == null)
            {
                // Collect the (AgentId, AssetGroupId) pairs for which the command wasn't ingested (both ingestion and completed time are null).
                // Ingestion batches EventHub ingestion and completion events and the order of these events are not guaranteed to be in FIFO order.
                // Thus, it's possible for a status record to be marked as completed before the event is started.
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Serially publish commands to the insert into queue work item handler.
        /// </summary>
        private async Task PublishToQueueAsync(IEnumerable<InsertIntoQueueWorkItem> workItems)
        {
            foreach (InsertIntoQueueWorkItem item in workItems)
            {
                await this.workItemPublisher.PublishWithSplitAsync(
                    item.Destinations,
                    splitDestinations => new InsertIntoQueueWorkItem
                    {
                        CommandId = item.CommandId,
                        CommandType = item.CommandType,
                        IsIngestionRecovery = item.IsIngestionRecovery,
                        IsReplayCommand = item.IsReplayCommand,
                        Destinations = splitDestinations.ToList(),
                        PxsCommand = item.PxsCommand,
                        DataSetVersion = item.DataSetVersion
                    },
                    x => TimeSpan.Zero);
            }
        }
    }
}
