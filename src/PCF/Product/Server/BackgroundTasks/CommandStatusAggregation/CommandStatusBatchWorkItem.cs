namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.CommandStatusAggregation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.SignalApplicability;

    /// <summary>
    /// A work item in an Azure Queue that writes a batch of events to Cold Storage.
    /// </summary>
    public class CommandStatusBatchWorkItem
    {
        public CommandStatusBatchWorkItem(
            CommandId commandId,
            DateTimeOffset? commandCreationTime,
            CommandLifecycleEvent[] events)
        {
            this.CommandId = commandId;
            this.CommandCreationTime = commandCreationTime;
            this.CompletedEvents = events.OfType<CommandCompletedEvent>().ToArray();
            this.StartedEvents = events.OfType<CommandStartedEvent>().ToArray();
            this.SoftDeleteEvents = events.OfType<CommandSoftDeleteEvent>().ToArray();
        }

        [Obsolete("Use the other constructor. This is here to make JSON.Net happy")]
        public CommandStatusBatchWorkItem()
        {
        }

        /// <summary>
        /// The command ID of the batch.
        /// </summary>
        public CommandId CommandId { get; set; }

        /// <summary>
        /// The command creation time.
        /// </summary>
        public DateTimeOffset? CommandCreationTime { get; set; }

        /// <summary>
        /// The list of events to aggregate.
        /// </summary>
        public CommandCompletedEvent[] CompletedEvents { get; set; }

        /// <summary>
        /// List of started events.
        /// </summary>
        public CommandStartedEvent[] StartedEvents { get; set; }

        /// <summary>
        /// List of soft delete events.
        /// </summary>
        public CommandSoftDeleteEvent[] SoftDeleteEvents { get; set; }

        /// <summary>
        /// Gets an enumerable list of all events.
        /// </summary>
        public IEnumerable<CommandLifecycleEvent> GetEvents()
        {
            return new CommandLifecycleEvent[0]
                .Concat(this.CompletedEvents ?? new CommandCompletedEvent[0])
                .Concat(this.StartedEvents ?? new CommandStartedEvent[0])
                .Concat(this.SoftDeleteEvents ?? new CommandSoftDeleteEvent[0]);
        }
    }

    /// <summary>
    /// Processes batch work items, for updates to cold storage. This handler takes a work item, and
    /// consolidates all of the lifecycle events inside of it into one single update to DocDB.
    /// </summary>
    public class CommandStatusBatchWorkItemQueueHandler : IAzureWorkItemQueueHandler<CommandStatusBatchWorkItem>
    {
        private readonly IAzureWorkItemQueuePublisher<CheckCompletionWorkItem> checkCompletionPublisher;
        private readonly ICommandHistoryRepository repository;
        private static readonly TimeSpan MaxCommandAge = TimeSpan.FromDays(Config.Instance.CommandHistory.DefaultTimeToLiveDays);

        public CommandStatusBatchWorkItemQueueHandler(
            IAzureWorkItemQueuePublisher<CheckCompletionWorkItem> checkCompletionPublisher,
            ICommandHistoryRepository repository)
        {
            this.repository = repository;
            this.checkCompletionPublisher = checkCompletionPublisher;
        }

        public SemaphorePriority WorkItemPriority => SemaphorePriority.Normal;

        /// <summary>
        /// Aggregates all of the events in this work item into one write to DocDB.
        /// </summary>
        public async Task<QueueProcessResult> ProcessWorkItemAsync(QueueWorkItemWrapper<CommandStatusBatchWorkItem> wrapper)
        {
            var workItem = wrapper.WorkItem;
            var record = await this.repository.QueryAsync(workItem.CommandId, CommandHistoryFragmentTypes.All);

            IncomingEvent.Current?.SetProperty("CommandId", workItem.CommandId.Value);
            IncomingEvent.Current?.SetProperty("CompletedEvents", workItem.CompletedEvents.Length.ToString());
            IncomingEvent.Current?.SetProperty("StartedEvents", workItem.StartedEvents.Length.ToString());
            IncomingEvent.Current?.SetProperty("SoftDeleteEvents", workItem.SoftDeleteEvents.Length.ToString());
            IncomingEvent.Current?.SetProperty("CommandCreationTime", workItem.CommandCreationTime?.ToString() ?? string.Empty);

            if (record == null || record.Core == null || record.AuditMap == null || record.ExportDestinations == null || record.StatusMap == null)
            {
                // Nonprod code to see if we can raise the QOS for this in PPE.
#if INCLUDE_TEST_HOOKS
                if (FlightingUtilities.IsEnabled(FlightingNames.CommandStatusBatchWorkItemAutoCompleteOnError))
                {
                    ProductionSafetyHelper.EnsureNotInProduction();
                    return QueueProcessResult.Success();
                }
#endif

                // Completes command status work items for commands which already expired from Command History.
                if (workItem.CommandCreationTime.HasValue)
                {
                    TimeSpan? commandAge = DateTimeOffset.UtcNow - workItem.CommandCreationTime;

                    if (commandAge > MaxCommandAge)
                    {
                        IncomingEvent.Current?.SetProperty("CompletedExpiredCommandId", workItem.CommandId.Value);
                        IncomingEvent.Current?.SetProperty("CommandAge", commandAge.ToString());

                        DualLogger.Instance.Warning(nameof(CommandStatusBatchWorkItem), $"Command already expired={workItem.CommandId} ignore status updates");

                        return QueueProcessResult.Success();
                    }
                }
                else
                {
                    DualLogger.Instance.Warning(nameof(CommandStatusBatchWorkItem), " No commandCreationTime available, ignore item");

                    // There's no CommandCreationTime; report success so that it will be removed from the queue
                    return QueueProcessResult.Success();
                }

                DualLogger.Instance.Error(nameof(CommandStatusBatchWorkItem), $"Unexpected! CommandStatusBatchWorkItem got command={workItem.CommandId} for which there was no record.");

                throw new InvalidOperationException("Unexpected! CommandStatusBatchWorkItem got command for which there was no record.");
            }

            DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(CommandStatusBatchWorkItem), $"Before replace commandHistory records for commandId= {record.CommandId} with icc={record.Core.IngestedCommandCount} and tcc={record.Core.TotalCommandCount}");

            // Aggregate all of the events on top of this record.
            var aggregator = new Aggregator(record);
            foreach (var @event in workItem.GetEvents())
            {
                @event.Process(aggregator);
            }

            CommandHistoryFragmentTypes modifiedFragments = aggregator.ModifiedFragments;
            if (record.Core.TotalCommandCount != record.StatusMap.Count)
            {
                record.Core.TotalCommandCount = record.StatusMap.Count;
                modifiedFragments |= CommandHistoryFragmentTypes.Core;
            }

            long ingestedCount = record.StatusMap.Count(x => x.Value.IngestionTime != null);
            if (record.Core.IngestedCommandCount != ingestedCount)
            {
                record.Core.IngestedCommandCount = ingestedCount;
                modifiedFragments |= CommandHistoryFragmentTypes.Core;
            }

            long completedCount = record.StatusMap.Count(x => x.Value.CompletedTime != null);
            if (record.Core.CompletedCommandCount != completedCount)
            {
                record.Core.CompletedCommandCount = completedCount;
                modifiedFragments |= CommandHistoryFragmentTypes.Core;
            }

            IncomingEvent.Current?.SetProperty("Modified", modifiedFragments.ToString());

            if (modifiedFragments != CommandHistoryFragmentTypes.None)
            {
                try
                {
                    await this.repository.ReplaceAsync(record, modifiedFragments);
                }
                catch (CommandFeedException ex)
                {
                    if (ex.ErrorCode == CommandFeedInternalErrorCode.Conflict)
                    {

                        DualLogger.Instance.LogErrorForCommandLifeCycle(nameof(CommandStatusBatchWorkItem), ex, $"Conflict while Updating commandHistory records for commandId= {record.CommandId} with icc={record.Core.IngestedCommandCount} and tcc={record.Core.TotalCommandCount} modifiedFragments={modifiedFragments} Will retry!");

                        // Conflicts are expected from time to time.
                        IncomingEvent.Current?.SetProperty("Conflict", "true");
                        return QueueProcessResult.TransientFailureRandomBackoff();
                    }
                    else
                    {
                        DualLogger.Instance.LogErrorForCommandLifeCycle(nameof(CommandStatusBatchWorkItem), ex, $"Unhandled error while Updating commandHistory records for commandId= {record.CommandId} with icc={record.Core.IngestedCommandCount} and tcc={record.Core.TotalCommandCount} modifiedFragments={modifiedFragments}");
                    }

                    throw;
                }
            }

            IncomingEvent.Current?.SetProperty("CompleteItemCount", record.Core.CompletedCommandCount.ToString());
            IncomingEvent.Current?.SetProperty("IngestedItemCount", record.Core.IngestedCommandCount.ToString());
            IncomingEvent.Current?.SetProperty("TotalItemCount", record.Core.TotalCommandCount.ToString());
            IncomingEvent.Current?.SetProperty("StatusMapCount", record.StatusMap.Count.ToString());

            string completionStatus = record.Core.IsGloballyComplete ? "AlreadyComplete" : "NotYetComplete";
            if (record.Core.CompletedCommandCount.Value >= record.Core.TotalCommandCount.Value && !record.Core.IsGloballyComplete)
            {
                completionStatus = "CompleteDueToTotalCount";

                await this.checkCompletionPublisher.PublishAsync(
                    new CheckCompletionWorkItem { CommandId = record.CommandId });
            }

            IncomingEvent.Current?.SetProperty("CompletionStatus", completionStatus);
            return QueueProcessResult.Success();
        }

        /// <summary>
        /// Defines an aggregator class that applies multiple events to the same DocDB cold storage record,
        /// so that we need only write the aggregated result once.
        /// </summary>
        private class Aggregator : ICommandLifecycleEventProcessor
        {
            private readonly CommandHistoryRecord record;

            public Aggregator(CommandHistoryRecord record)
            {
                this.record = record;
            }

            /// <summary>
            /// Indicates if we need to update the record based on the processed events. Defaults to false, only ever set to true.
            /// </summary>
            public CommandHistoryFragmentTypes ModifiedFragments { get; private set; }

            public void Process(CommandCompletedEvent completedEvent)
            {
                if (completedEvent.AgentId == null || completedEvent.AgentId.GuidValue == Guid.Empty)
                {
                    // This is sent for v2 agents, ignore.
                    return;
                }
                
                var assetGroup = this.GetOrCreateAssetGroup(completedEvent);

                if (assetGroup.CompletedTime == null || assetGroup.CompletedTime > completedEvent.Timestamp)
                {
                    assetGroup.CompletedTime = completedEvent.Timestamp;
                    assetGroup.Delinked = completedEvent.Delinked;
                    assetGroup.ClaimedVariants = completedEvent.ClaimedVariantIds;
                    assetGroup.ForceCompleted = completedEvent.ForceCompleted;
                    assetGroup.AffectedRows = completedEvent.AffectedRows;

                    this.ModifiedFragments |= CommandHistoryFragmentTypes.Status;
                }

                // Sometimes we only send "complete". Update ingestion time to be non-null in this case.
                if (assetGroup.IngestionTime == null || assetGroup.IngestionTime > completedEvent.Timestamp)
                {
                    assetGroup.IngestionTime = completedEvent.Timestamp;
                    this.ModifiedFragments |= CommandHistoryFragmentTypes.Status;
                }
            }

            public void Process(CommandSoftDeleteEvent softDeleteEvent)
            {
                var assetGroup = this.GetOrCreateAssetGroup(softDeleteEvent);

                if (assetGroup.SoftDeleteTime == null || assetGroup.SoftDeleteTime > softDeleteEvent.Timestamp)
                {
                    assetGroup.SoftDeleteTime = softDeleteEvent.Timestamp;
                    this.ModifiedFragments |= CommandHistoryFragmentTypes.Status;
                }

                if (assetGroup.IngestionTime == null || assetGroup.IngestionTime > softDeleteEvent.Timestamp)
                {
                    assetGroup.IngestionTime = softDeleteEvent.Timestamp;
                    this.ModifiedFragments |= CommandHistoryFragmentTypes.Status;
                }
            }

            public void Process(CommandStartedEvent startedEvent)
            {
                var assetGroup = this.GetOrCreateAssetGroup(startedEvent);

                if (assetGroup.IngestionTime == null || assetGroup.IngestionTime > startedEvent.Timestamp)
                {
                    assetGroup.IngestionTime = startedEvent.Timestamp;
                    this.ModifiedFragments |= CommandHistoryFragmentTypes.Status;
                }

                if (startedEvent.FinalExportDestinationUri != null && this.record.Core.FinalExportDestinationUri != startedEvent.FinalExportDestinationUri)
                {
                    this.record.Core.FinalExportDestinationUri = startedEvent.FinalExportDestinationUri;
                    this.ModifiedFragments |= CommandHistoryFragmentTypes.Core;
                }

                // For exports, update the export destination map.
                if (startedEvent.CommandType == PrivacyCommandType.Export &&
                    startedEvent.ExportStagingDestinationUri != null &&
                    !this.record.ExportDestinations.Any(r => r.Key.agentId == startedEvent.AgentId && r.Key.assetGroupId == startedEvent.AssetGroupId))
                {
                    var exportDestinationRecord = new CommandHistoryExportDestinationRecord(
                        startedEvent.AgentId,
                        startedEvent.AssetGroupId,
                        startedEvent.ExportStagingDestinationUri,
                        startedEvent.ExportStagingPath);

                    this.record.ExportDestinations[(startedEvent.AgentId, startedEvent.AssetGroupId)] = exportDestinationRecord;
                    this.ModifiedFragments |= CommandHistoryFragmentTypes.ExportDestinations;
                }

                // Make sure that audit state reflects that command has been sent to the agent.
                // We should never expect to get a started event for something not in the audit map. Such a case warrants an exception.
                var key = (startedEvent.AgentId, startedEvent.AssetGroupId);

                if (!this.record.AuditMap.TryGetValue(key, out var auditRecord))
                {
                    // This isn't supposed to happen, but we should at least fail gracefully if it does.
                    IncomingEvent.Current?.SetProperty("UnexpectedAuditMapEntry", "true");
                    PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "UnexpectedAuditMapEntries").Increment();

                    auditRecord = new CommandIngestionAuditRecord
                    {
                        ApplicabilityReasonCode = ApplicabilityReasonCode.None,
                        IngestionStatus = CommandIngestionStatus.SentToAgent,
                    };

                    this.record.AuditMap[key] = auditRecord;
                    this.ModifiedFragments |= CommandHistoryFragmentTypes.Audit;
                }

                if (auditRecord.IngestionStatus != CommandIngestionStatus.SentToAgent)
                {
                    auditRecord.IngestionStatus = CommandIngestionStatus.SentToAgent;
                    this.ModifiedFragments |= CommandHistoryFragmentTypes.Audit;
                }
            }

            public void Process(CommandRawDataEvent rawDataEvent)
            {
                // No-op
            }

            /// <inheritdoc />
            public void Process(CommandDroppedEvent droppedEvent)
            {
                // No-op
            }

            public void Process(CommandSentToAgentEvent sentToAgentEvent)
            {
                // No-op
            }

            public void Process(CommandPendingEvent pendingEvent)
            {
                // No-op
            }

            public void Process(CommandFailedEvent failedEvent)
            {
                // No-op
            }

            public void Process(CommandUnexpectedEvent unexpectedCommandEvent)
            {
                // No-op
            }

            public void Process(CommandVerificationFailedEvent verificationFailedEvent)
            {
                // No-op
            }

            public void Process(CommandUnexpectedVerificationFailureEvent unexpectedVerificationFailureEvent)
            {
                // No-op
            }

            private CommandHistoryAssetGroupStatusRecord GetOrCreateAssetGroup(CommandLifecycleEvent @event)
            {
                (AgentId, AssetGroupId) key = (@event.AgentId, @event.AssetGroupId);

                if (!this.record.StatusMap.TryGetValue(key, out var assetGroupInfo))
                {
                    assetGroupInfo = new CommandHistoryAssetGroupStatusRecord(@event.AgentId, @event.AssetGroupId);
                    this.record.StatusMap[key] = assetGroupInfo;

                    this.ModifiedFragments |= CommandHistoryFragmentTypes.Status;
                }

                return assetGroupInfo;
            }
        }
    }
}
