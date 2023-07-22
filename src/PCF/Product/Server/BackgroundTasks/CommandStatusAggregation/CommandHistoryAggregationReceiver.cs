namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.CommandStatusAggregation
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using System.Threading;
    using System.Diagnostics;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;

    /// <summary>
    /// Receives from event hub, aggregates by command ID, and inserts per-command items into azure queues for publishing to DocDB cold storage.
    /// </summary>
    public class CommandHistoryAggregationReceiver : ICommandLifecycleCheckpointProcessor
    {
        private readonly IAzureWorkItemQueuePublisher<CommandStatusBatchWorkItem> queuePublisher;
        private readonly string eventHubName;
        private ConcurrentQueue<CommandLifecycleEvent> events;
        private DateTimeOffset lastCheckpointTime;
        private CancellationToken cancellationToken;

        public CommandHistoryAggregationReceiver(IAzureWorkItemQueuePublisher<CommandStatusBatchWorkItem> queuePublisher, string eventHubName=null, CancellationToken cancellationToken = default)
        {
            this.events = new ConcurrentQueue<CommandLifecycleEvent>();
            this.lastCheckpointTime = DateTimeOffset.UtcNow;
            this.queuePublisher = queuePublisher;

            // TODO: Remove these from the constructor when the ingestion issue has been fixed (Remove by 5/19/2023)
            this.eventHubName = eventHubName ?? string.Empty;

            this.cancellationToken = cancellationToken;

#pragma warning disable CS4014 // Suppress CS4014
            // background taks to publish events to Azure Queue
            this.PublishEventsTaskAsync(cancellationToken);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        public void Process(CommandCompletedEvent completedEvent)
        {
            if (completedEvent.CommandType == Client.PrivacyCommandType.AgeOut)
            {
                // No-op for age out command
                return;
            }

            this.events.Enqueue(completedEvent);
        }

        public void Process(CommandSoftDeleteEvent softDeleteEvent)
        {
            if (softDeleteEvent.CommandType == Client.PrivacyCommandType.AgeOut)
            {
                // No-op for age out command
                return;
            }

            this.events.Enqueue(softDeleteEvent);
        }

        public void Process(CommandStartedEvent startedEvent)
        {
            DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(CommandHistoryAggregationReceiver), $"CommandStartedEvent received for commandId={startedEvent.CommandId} for agentId={startedEvent.AgentId}, assetGroupId={startedEvent.AssetGroupId}, eventHubName={eventHubName}, startedTime={startedEvent.Timestamp}");
            this.events.Enqueue(startedEvent);
        }

        /// <inheritdoc />
        public void Process(CommandRawDataEvent rawDataEvent)
        {
            // No-op
        }

        /// <inheritdoc />
        public void Process(CommandDroppedEvent droppedEvent)
        {
            // No-op
        }

        /// <inheritdoc />
        public void Process(CommandSentToAgentEvent sentToAgentEvent)
        {
            // No-op
        }

        /// <inheritdoc />
        public void Process(CommandPendingEvent pendingEvent)
        {
            // No-op
        }

        /// <inheritdoc />
        public void Process(CommandFailedEvent failedEvent)
        {
            // No-op
        }

        /// <inheritdoc />
        public void Process(CommandUnexpectedEvent unexpectedEvent)
        {
            // No-op
        }

        /// <inheritdoc />
        public void Process(CommandVerificationFailedEvent verificationFailedEvent)
        {
            // No-op
        }

        /// <inheritdoc />
        public void Process(CommandUnexpectedVerificationFailureEvent unexpectedVerificationFailureEvent)
        {
            // No-op
        }

        public bool ShouldCheckpoint()
        {
            int maxBatchSize = FlightingUtilities.GetConfigValue<int>(ConfigNames.PCF.CommandHistoryLifecycleBatchSize, defaultValue: 1000);
            int checkpointThreshold = FlightingUtilities.GetConfigValue<int>(ConfigNames.PCF.CommandHistoryAggregationReceiverCheckpointThreshold, defaultValue: maxBatchSize * 3);

            // Read checkpoint interval from app config first. If it's not
            // available in app config, fall back to the original value set in template.
            var checkPointInterval = FlightingUtilities.GetConfigValue<long>(
                config: ConfigNames.PCF.CommandHistoryAggregationReceiverEventHubCheckpointFrequencySeconds,
                defaultValue: Config.Instance.Worker.CommandHistoryEventHubCheckpointFrequencySeconds);

            if (this.events.Count > checkpointThreshold)
            {
                DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(CommandHistoryAggregationReceiver), $"ShouldCheckpoint Checkpointing: EventsCount={this.events.Count}, checkpointThreshold={checkpointThreshold}.");
                return true;
            }

            return DateTimeOffset.UtcNow - this.lastCheckpointTime > TimeSpan.FromSeconds(checkPointInterval) || this.events.Count > checkpointThreshold;
        }

        public async Task CheckpointAsync()
        {
            await Logger.InstrumentAsync(new OutgoingEvent(SourceLocation.Here()), ev => this.CheckpointHelperAsync());
            this.lastCheckpointTime = DateTimeOffset.UtcNow;
        }

        private async Task CheckpointHelperAsync(List<CommandLifecycleEvent> flattenedEvents = null)
        {
            // Pull all events out of our internal queue.
            if (flattenedEvents == null)
            {
                flattenedEvents = new List<CommandLifecycleEvent>(this.events.Count);
                while (this.events.TryDequeue(out var @event))
                {
                    flattenedEvents.Add(@event);
                }
            }

            // Group them by command ID.
            Dictionary<CommandId, CommandLifecycleEvent[]> groupedEvents = flattenedEvents.GroupBy(x => x.CommandId).ToDictionary(x => x.Key, x => x.ToArray());

            if (flattenedEvents.Any())
            {
                DualLogger.Instance.Information(nameof(CommandHistoryAggregationReceiver), $"CheckpointHelperAsync Consolidated {flattenedEvents.Count} into {groupedEvents.Count} grouped work items.");
            }

            // Schedule tasks to write the aggregated results.
            List<Task> insertTasks = new List<Task>();

            int maxBatchSize = FlightingUtilities.GetConfigValue<int>(ConfigNames.PCF.CommandHistoryLifecycleBatchSize, defaultValue: 1000);

            // TODO: Using Environment.ProcessorCount is too slow we have to use more threads untill we find better solution
            int maxThreads = FlightingUtilities.GetConfigValue<int>(ConfigNames.PCF.CommandHistoryAggregationReceiverCheckpointMaxThreadCount, defaultValue: maxBatchSize);

            // The default value is based on current queue item processing latency ~1500ms
            int multiplier = FlightingUtilities.GetConfigValue<int>(ConfigNames.PCF.CommandHistoryAggregationReceiverCheckpointPublishDelayMultiplier, defaultValue: 1500);

            foreach (var keyValuePair in groupedEvents)
            {
                CommandId commandId = keyValuePair.Key;
                DateTimeOffset? commandCreationTime = keyValuePair.Value.FirstOrDefault(e => e.CommandCreationTime.HasValue)?.CommandCreationTime;
                CommandLifecycleEvent[] events = keyValuePair.Value;

                var @workItem = new CommandStatusBatchWorkItem(commandId, commandCreationTime, events);

                DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(CommandHistoryAggregationReceiver), $"CheckpointHelperAsync Add publishing task CommandId={commandId}, numberOfEvents={events.Count()}");
                insertTasks.Add(this.queuePublisher.PublishWithSplitAsync(
                    events,
                    splitEvents => new CommandStatusBatchWorkItem(commandId, commandCreationTime, splitEvents.ToArray()),
                position => TimeSpan.FromMilliseconds(position * multiplier + RandomHelper.Next(0, multiplier))));

                // Run insert tasks in batches
                if (insertTasks.Count >= maxThreads)
                {
                    DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(CommandHistoryAggregationReceiver), $"CheckpointHelperAsync Run publishing tasks: numberOfThreads={insertTasks.Count}, maxThreads={maxThreads}");
                    await Task.WhenAll(insertTasks);
                    insertTasks.Clear();
                }
            }

            if (insertTasks.Any())
            {
                DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(CommandHistoryAggregationReceiver), $"CheckpointHelperAsync Run reminder of publishing tasks numberOfThreads={insertTasks.Count}, maxThreads={maxThreads}");
                await Task.WhenAll(insertTasks);
            }

            DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(CommandHistoryAggregationReceiver), $"CheckpointHelperAsync Finished publishing {flattenedEvents.Count} into {groupedEvents.Count} grouped work items.");
        }

        private async Task PublishEventsTaskAsync(CancellationToken cancellationToken)
        {
            List<CommandLifecycleEvent> flattenedEvents = new List<CommandLifecycleEvent>();

            while (!cancellationToken.IsCancellationRequested) 
            {
                try
                {
                    var maxBatchSize = FlightingUtilities.GetConfigValue<int>(ConfigNames.PCF.CommandHistoryLifecycleBatchSize, defaultValue: 1000);

                    // publish if queue size is more than maxBatchSize
                    if (this.events.Count > maxBatchSize)
                    {
                        DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(CommandHistoryAggregationReceiver), $"PublishEventsTaskAsync EventsCount={this.events.Count}, maxBatchSize={maxBatchSize}.");

                        // make sure the buffer is empty
                        flattenedEvents.Clear();

                        // move events to the buffer
                        while (this.events.TryDequeue(out var @event))
                        {
                            flattenedEvents.Add(@event);
                        }

                        DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(CommandHistoryAggregationReceiver),  $"PublishEventsTaskAsync publishing events. EventsCount={flattenedEvents.Count}.");

                        await Logger.InstrumentAsync(new OutgoingEvent(SourceLocation.Here()), ev => this.CheckpointHelperAsync(flattenedEvents: flattenedEvents));
                    }
                    else
                    {
                        // waiting for 1/2 sec if queue size is empty or not enough. We do not need to parameterize this value, just a linear standard delay.
                        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
                    }
                }
                catch (Exception ex) 
                {
                    DualLogger.Instance.Error(nameof(CommandHistoryAggregationReceiver), ex, $"PublishEventsTaskAsync Error while publishing events. EventsCount={flattenedEvents.Count}.");

                    // recover events to retry later
                    foreach (var @event in flattenedEvents)
                    {
                        this.events.Enqueue(@event);
                    }

                    // relax if error happened
                    var checkPointIntervalSeconds = FlightingUtilities.GetConfigValue<long>(
                        config: ConfigNames.PCF.CommandHistoryAggregationReceiverEventHubCheckpointFrequencySeconds,
                        defaultValue: Config.Instance.Worker.CommandHistoryEventHubCheckpointFrequencySeconds);
                    await Task.Delay(TimeSpan.FromMilliseconds(RandomHelper.Next(100, 1000 * (int)checkPointIntervalSeconds)), cancellationToken);
                }
            }
        }
    }
}
