namespace Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.BackgroundTasks
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Telemetry;
    using Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.Repository;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Receives command lifecycle events from EventHub, aggregate and publish to Telemetry DB 
    /// </summary>
    public class TelemetryLifecycleCheckpointProcessor : ICommandLifecycleCheckpointProcessor
    {
        private ITelemetryRepository telemetryRepository;
        private DateTimeOffset lastCheckpointTime;
        private TimeSpan checkpointFrequency;
        private readonly ConcurrentQueue<LifecycleEventTelemetry> telemetryEvents;

        public TelemetryLifecycleCheckpointProcessor(ITelemetryRepository telemetryRepository, TimeSpan checkpointFrequency)
        {
            this.lastCheckpointTime = DateTimeOffset.UtcNow;
            this.telemetryRepository = telemetryRepository;
            this.checkpointFrequency = checkpointFrequency;
            this.telemetryEvents = new ConcurrentQueue<LifecycleEventTelemetry>();
        }

        /// <inheritdoc />
        public void Process(CommandCompletedEvent completedEvent)
        {
            if (completedEvent.CompletedByPcf)
            {
                this.AddLifecycleEvent(completedEvent, LifecycleEventType.CommandCompletedByPcfEvent);
            }
            else
            {
                this.AddLifecycleEvent(completedEvent, LifecycleEventType.CommandCompletedEvent);
            }
        }

        /// <inheritdoc />
        public void Process(CommandSoftDeleteEvent softDeleteEvent)
        {
            this.AddLifecycleEvent(softDeleteEvent, LifecycleEventType.CommandSoftDeleteEvent);
        }

        /// <inheritdoc />
        public void Process(CommandStartedEvent startedEvent)
        {
            this.AddLifecycleEvent(startedEvent, LifecycleEventType.CommandStartedEvent);
        }

        /// <inheritdoc />
        public void Process(CommandSentToAgentEvent sentToAgentEvent)
        {
            this.AddLifecycleEvent(sentToAgentEvent, LifecycleEventType.CommandSentToAgentEvent);
        }

        /// <inheritdoc />
        public void Process(CommandPendingEvent pendingEvent)
        {
            this.AddLifecycleEvent(pendingEvent, LifecycleEventType.CommandPendingEvent);
        }

        /// <inheritdoc />
        public void Process(CommandFailedEvent failedEvent)
        {
            this.AddLifecycleEvent(failedEvent, LifecycleEventType.CommandFailedEvent);
        }

        /// <inheritdoc />
        public void Process(CommandUnexpectedEvent unexpectedEvent)
        {
            this.AddLifecycleEvent(unexpectedEvent, LifecycleEventType.CommandUnexpectedEvent);
        }

        /// <inheritdoc />
        public void Process(CommandVerificationFailedEvent verificationFailedEvent)
        {
            this.AddLifecycleEvent(verificationFailedEvent, LifecycleEventType.CommandVerificationFailedEvent);
        }

        /// <inheritdoc />
        public void Process(CommandUnexpectedVerificationFailureEvent unexpectedVerificationFailureEvent)
        {
            this.AddLifecycleEvent(unexpectedVerificationFailureEvent, LifecycleEventType.CommandUnexpectedVerificationFailureEvent);
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

        public bool ShouldCheckpoint()
        {
            return 
                (DateTimeOffset.UtcNow - this.lastCheckpointTime > this.checkpointFrequency) 
                || this.telemetryEvents.Count > 5000;
        }

        public async Task CheckpointAsync()
        {
            List<LifecycleEventTelemetry> flattenedEvents = new List<LifecycleEventTelemetry>(this.telemetryEvents.Count);
            DualLogger.Instance.Verbose(nameof(TelemetryLifecycleCheckpointProcessor), $"Number of events={flattenedEvents.Count}.");
            
            // Pull all events out of our internal queue.
            while (this.telemetryEvents.TryDequeue(out var @event))
            {
                flattenedEvents.Add(@event);
            }

            Logger.Instance?.LogTelemetryLifecycleCheckpointInfo(
                new TelemetryLifecycleCheckpointInfo
                {
                    CheckpointFrequency = this.checkpointFrequency,
                    EventsCount = flattenedEvents.Count,
                    LastCheckpointTime = this.lastCheckpointTime
                });

            await this.telemetryRepository.AddAsync(flattenedEvents);

            this.lastCheckpointTime = DateTimeOffset.UtcNow;
        }

        private void AddLifecycleEvent(CommandLifecycleEvent lifecycleEvent, LifecycleEventType telemetryLifecycleEventType)
        {
            if (lifecycleEvent.CommandType == Client.PrivacyCommandType.AgeOut)
            {
                // Filter out age out related events from kusto.
                return;
            }

            this.telemetryEvents.Enqueue(
                new LifecycleEventTelemetry()
                {
                    EventType = telemetryLifecycleEventType,
                    AgentId = lifecycleEvent.AgentId,
                    AssetGroupId = lifecycleEvent.AssetGroupId,
                    CommandType = lifecycleEvent.CommandType,
                    CommandId = lifecycleEvent.CommandId,
                    Count = 1,
                    Timestamp = lifecycleEvent.Timestamp,
                });
        }
    }
}
