namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.ComplianceServices.Common.Interfaces;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.EventHubs.Processor;
    using Microsoft.Azure.Storage;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen;
    using Microsoft.PrivacyServices.Common.Azure;

    using PerformanceCounterType = Microsoft.Azure.ComplianceServices.Common.Instrumentation.PerformanceCounterType;

    /// <summary>
    /// An event handler for command lifecycle events.
    /// </summary>
    public interface ICommandLifecycleEventProcessor
    {
        /// <summary>
        /// Processes a completed event.
        /// </summary>
        void Process(CommandCompletedEvent completedEvent);

        /// <summary>
        /// Processes a soft delete event.
        /// </summary>
        void Process(CommandSoftDeleteEvent softDeleteEvent);

        /// <summary>
        /// Processes a started event.
        /// </summary>
        void Process(CommandStartedEvent startedEvent);

        /// <summary>
        /// Processes a command sent to agent event.
        /// </summary>
        void Process(CommandSentToAgentEvent sentToAgentEvent);

        /// <summary>
        /// Processes a pending event.
        /// </summary>
        void Process(CommandPendingEvent pendingEvent);

        /// <summary>
        /// Processes a failed event.
        /// </summary>
        void Process(CommandFailedEvent failedEvent);

        /// <summary>
        /// Processes a unexpected command event.
        /// </summary>
        void Process(CommandUnexpectedEvent unexpectedEvent);

        /// <summary>
        /// Processes a verification failed event.
        /// </summary>
        void Process(CommandVerificationFailedEvent verificationFailedEvent);

        /// <summary>
        /// Processes a unexpected verification failure event.
        /// </summary>
        void Process(CommandUnexpectedVerificationFailureEvent unexpectedVerificationFailureEvent);

        /// <summary>
        /// Processes a command raw data event.
        /// </summary>
        void Process(CommandRawDataEvent rawDataEvent);

        /// <summary>
        /// Processes a command raw data event.
        /// </summary>
        void Process(CommandDroppedEvent droppedEvent);
    }

    /// <summary>
    /// An event handler capable of checkpointing.
    /// </summary>
    public interface ICommandLifecycleCheckpointProcessor : ICommandLifecycleEventProcessor
    {
        /// <summary>
        /// Returns a value indicating if we should checkpoint the event hub.
        /// </summary>
        bool ShouldCheckpoint();

        /// <summary>
        /// Invokes the checkpoint operation. Upon returning without an exception,
        /// the underlying event hub will be checkpointed.
        /// </summary>
        Task CheckpointAsync();
    }

    /// <summary>
    /// Helper class to receive commands from event hubs. Forwards received events to an instane of ICommandLifecycleEventProcessor.
    /// </summary>
    public class CommandLifecycleEventReceiver
    {
        // Special event name that we just use to keep the event hub sludge moving and make sure each
        // partition gets poked periodically so that checkpoints happen on schedule.
        internal const string NoopEventName = "noop";
        internal const string LegacyAuditEventName = "CommandIngestionAudit";

        private static readonly IPerformanceCounter ConcurrencyCounter = PerfCounterUtility.GetOrCreate(PerformanceCounterType.Number, "EventHubInstantaneousWork");

        private EventProcessorHost eventHost;
        private readonly IConfiguredEventHub config;
        private readonly Func<ICommandLifecycleCheckpointProcessor> handlerFactory;
        private readonly string eventHubName;
        private readonly string consumerGroupName;
        private readonly string hostName;
        private readonly string moniker;
        private readonly SemaphorePriority workItemPriority;

        /// <summary>
        /// Initializes the CommandLifecycleEventReceiver.
        /// eventHubName is used as Event Hub lease storage blob container name
        /// consumerGroupName is the provisioned Consumer Group for the event hub
        /// </summary>
        public CommandLifecycleEventReceiver(
            string eventHubName,
            string consumerGroupName,
            IConfiguredEventHub config,
            Func<ICommandLifecycleCheckpointProcessor> eventHandlerFactory,
            SemaphorePriority workItemPriority = SemaphorePriority.High)
        {
            this.hostName = $"eh-{consumerGroupName}-{EnvironmentInfo.NodeName}-{RandomHelper.Next()}";

            this.moniker = config.Moniker;
            this.eventHubName = eventHubName;
            this.consumerGroupName = consumerGroupName;
            this.config = config;
            this.handlerFactory = eventHandlerFactory;
            this.workItemPriority = workItemPriority;
        }

        /// <summary>
        /// Begins receiving events.
        /// </summary>
        public Task StartReceivingAsync()
        {
            // Parse the blob connection string to ensure that it's well-formed.
            CloudStorageAccount.Parse(this.config.CheckpointAccountConnectionString);

            this.eventHost = new EventProcessorHost(this.hostName, this.config.Path, this.consumerGroupName, this.config.ConnectionString, this.config.CheckpointAccountConnectionString, this.eventHubName);
            return this.eventHost.RegisterEventProcessorFactoryAsync(new EventProcessorFactory(this.handlerFactory, this));
        }

        /// <summary>
        /// Stops receiving events.
        /// </summary>
        public Task StopReceivingAsync()
        {
            return this.eventHost.UnregisterEventProcessorAsync();
        }

        private class EventProcessorFactory : IEventProcessorFactory
        {
            private readonly Func<ICommandLifecycleCheckpointProcessor> handlerFactory;
            private readonly CommandLifecycleEventReceiver parent;
            private readonly AzureWorkItemQueue<JsonEventData[]> retryPublisher;

            public EventProcessorFactory(
                Func<ICommandLifecycleCheckpointProcessor> handlerFactory,
                CommandLifecycleEventReceiver parent)
            {
                this.handlerFactory = handlerFactory;
                this.parent = parent;
                this.retryPublisher = new AzureWorkItemQueue<JsonEventData[]>($"eh-rq-{this.parent.config.Path}-{this.parent.consumerGroupName}");
            }

            public IEventProcessor CreateEventProcessor(PartitionContext context)
            {
                return new EventProcessor(this.handlerFactory, this.retryPublisher, this.parent, context);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
        private class EventProcessor : IEventProcessor, IAzureWorkItemQueueHandler<JsonEventData[]>
        {
            private readonly Func<ICommandLifecycleCheckpointProcessor> processorFactory;

            // We use a receiver "sink" to handle our receives from here. Much of this class
            // depends on Event Hub internals, so as much business logic as possible is shifted
            // into this class for testability.
            private readonly CommandLifecycleReceiverSink eventSink;

            private readonly CommandLifecycleEventReceiver parent;
            private readonly string handlerTypeName;
            private readonly string identifier;

            // The event sink publishes necessary retries to this queue. This class
            // drains the queue.
            private readonly AzureWorkItemQueue<JsonEventData[]> retryQueue;

            private readonly CancellationTokenSource cancellationTokenSource;
            private Task warmEventHubTask;
            private Task retryQueueTask;
            private DateTime mostRecentMessageEnqueuedTime = DateTime.MinValue;
            
            public EventProcessor(
                Func<ICommandLifecycleCheckpointProcessor> processorFactory,
                AzureWorkItemQueue<JsonEventData[]> retryQueue,
                CommandLifecycleEventReceiver parent,
                PartitionContext context)
            {
                this.processorFactory = processorFactory;
                this.retryQueue = retryQueue;

                this.identifier = $"{parent.moniker}.{context.ConsumerGroupName}.{context.PartitionId}";
                this.eventSink = new CommandLifecycleReceiverSink(
                    processorFactory, 
                    this.retryQueue, 
                    this.identifier);
                this.parent = parent;
                this.handlerTypeName = processorFactory().GetType().Name;
                this.cancellationTokenSource = new CancellationTokenSource();
            }

            public SemaphorePriority WorkItemPriority => parent.workItemPriority;

            public Task OpenAsync(PartitionContext context)
            {
                Logger.InstrumentSynchronous(
                    new IncomingEvent(SourceLocation.Here()),
                    ev =>
                    {
                        ev.OperationName = $"{this.handlerTypeName}.{nameof(this.OpenAsync)}";
                        ev["Identifier"] = this.identifier;
                        ev["Partition"] = context?.PartitionId;
                        ev["ConsumerGroup"] = context?.ConsumerGroupName;
                        ev["EventHubName"] = this.parent.moniker;

                        DualLogger.Instance.Verbose(nameof(CommandLifecycleEventReceiver), $"Acquired lease on {this.identifier}");

                        if (Config.Instance.CommandLifecycle.EnablePeriodicNoOpEvents)
                        {
                            this.warmEventHubTask = this.WarmEventHub(context);
                        }
                        else
                        {
                            this.warmEventHubTask = Task.FromResult(true);
                        }

                        // Disable retry queue processing.
                        bool disableRetryQueue = FlightingUtilities.IsStringValueEnabled(
                            FlightingNames.CommandLifecycleEventReceiverDisableRetryQueue,
                            this.parent.consumerGroupName);

                        if (disableRetryQueue)
                        {
                            DualLogger.Instance.Information($"{nameof(EventProcessor)}", $"Disable retry queue processing. Moniker={this.parent.moniker};ConsumerGroupName={this.parent.consumerGroupName}");
                            ev.StatusCode = HttpStatusCode.OK;

                            return;
                        }

                        this.retryQueueTask = this.retryQueue.BeginProcessAsync(this, this.cancellationTokenSource.Token);
                        ev.StatusCode = HttpStatusCode.OK;
                    });

                return Task.FromResult(true);
            }

            /// <summary>
            /// A thread that runs as long as we have the lease on this partition. Once a second, it injects a "noop" event into the event hub.
            /// The practical effect that this has is that it keeps our event hubs from stagnating, so that checkpoints still occur at their regular intervals.
            /// </summary>
            private async Task WarmEventHub(PartitionContext context)
            {
                EventHubClient client = EventHubClient.CreateFromConnectionString(this.parent.config.ConnectionString + ";EntityPath=" + context.EventHubPath);
                var partitionSender = client.CreatePartitionSender(context.PartitionId);

                var lastInfoDumpTime = DateTime.MinValue;
                while (!this.cancellationTokenSource.IsCancellationRequested)
                {
                    // Each second, send a "poke" to this partition to keep things flowing.
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    // Dump information every minute.
                    if (DateTime.UtcNow - lastInfoDumpTime > TimeSpan.FromMinutes(1))
                    {
                        var info = await client.GetPartitionRuntimeInformationAsync(context.PartitionId);
                        DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(CommandLifecycleEventReceiver), $"LastMessageEnqueuedTime={info.LastEnqueuedTimeUtc} MostRecentMessageReadEnqueuedTime={this.mostRecentMessageEnqueuedTime} for {this.identifier}");
                        lastInfoDumpTime = DateTime.UtcNow;
                    }
                    
                    var data = new EventData(new byte[5]);
                    data.Properties[CommandLifecycleEventParser.EventNameProperty] = NoopEventName;

                    try
                    {
                        await partitionSender.SendAsync(data);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance?.UnexpectedException(ex);
                    }
                }

                await partitionSender.CloseAsync();
                await client.CloseAsync();
            }

            public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
            {
                this.mostRecentMessageEnqueuedTime = messages.Max(m => m.SystemProperties.EnqueuedTimeUtc);
                // Combine handler type name with random percentage to create a speed bump. 
                // This is a while loop, so the effects can compound. The bigger the number for random percentage, the longer the sleep (exponentially).
                // For example, RandomPercentage < 75 => 2.4 second slow down on average
                //              RandomPercentage < 90 => 6.5 second slow down
                //                               < 99 => 68.9 seconds
                // Update: Wait time is maxed out at 20 seconds, to keep it within lease time of the Event Hub partition which defaults to 30 according to EventHub implementation at
                // https://github.com/Azure/azure-sdk-for-net/blob/1264615ef858e1c368e1ba2b89771fdb1be69428/sdk/eventhub/Microsoft.Azure.EventHubs.Processor/src/PartitionManagerOptions.cs#L17

                Stopwatch sw = Stopwatch.StartNew(); 
                while (FlightingUtilities.IsStringValueEnabled(FlightingNames.CommandLifecycleEventHubReceiverSlowdown, this.handlerTypeName))
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    if (sw.Elapsed.TotalSeconds >= 20)
                    {
                        break;
                    }
                }
                sw.Stop();

                if (FlightingUtilities.IsEnabled(FlightingNames.PCFEventHubProcessingFullThrottle))
                {
                    await ProcessEventInternalAsync(context, messages);
                }
                else
                {
                    using (await PrioritySemaphore.Instance.WaitAsync(this.WorkItemPriority))
                    {
                        await ProcessEventInternalAsync(context, messages);
                    }
                }
            }

            private async Task ProcessEventInternalAsync(PartitionContext context, IEnumerable<EventData> messages)
            {
                await Logger.InstrumentAsync(
                        new IncomingEvent(SourceLocation.Here()),
                        async ev =>
                        {
                            try
                            {
                                ConcurrencyCounter.Increment();

                                ev.OperationName = $"{this.handlerTypeName}.{nameof(this.ProcessEventsAsync)}";

                                ev["MessageCount"] = messages.Count().ToString();
                                ev["Partition"] = context?.PartitionId;
                                ev["ConsumerGroup"] = context?.ConsumerGroupName;
                                ev["EventHubName"] = this.parent.moniker;
                                ev["Identifier"] = this.identifier;

                                IEnumerable<JsonEventData> jsonMessages = messages.Select(x => new JsonEventData(x));

                                this.SetEventHubClientProperties(context);

                                await this.eventSink.HandleEventBatchAsync(jsonMessages, context.CheckpointAsync);
                            }
                            finally
                            {
                                ConcurrencyCounter.Decrement();
                            }
                        });
            }

            private void SetEventHubClientProperties(PartitionContext context)
            {
                string connectString = this.parent.config.ConnectionString + ";EntityPath=" + context.EventHubPath;
                this.eventSink.EventHubConnectionString = connectString;
                this.eventSink.EventHubPartnerId = context.PartitionId;
            }

            public Task CloseAsync(PartitionContext context, CloseReason reason)
            {
                return Logger.InstrumentAsync(
                    new IncomingEvent(SourceLocation.Here()),
                    async ev =>
                    {
                        ev.OperationName = $"{this.handlerTypeName}.{nameof(this.CloseAsync)}";
                        ev["Identifier"] = this.identifier;
                        ev["Partition"] = context?.PartitionId;
                        ev["ConsumerGroup"] = context?.ConsumerGroupName;
                        ev["EventHubName"] = this.parent.moniker;
                        ev["CloseReason"] = reason.ToString();

                        DualLogger.Instance.Verbose(nameof(CommandLifecycleEventReceiver), $"Lost lease on {this.identifier}");
                        using (this.cancellationTokenSource)
                        {
                            this.cancellationTokenSource.Cancel();

                            await this.retryQueueTask;
                            await this.warmEventHubTask;
                        }

                        ev.StatusCode = HttpStatusCode.OK;
                    });
            }

            public Task ProcessErrorAsync(PartitionContext context, Exception error)
            {
                // Errors are often quite chatty. We can log them, but they don't usually mean anything
                // since they can occur during healthy operation.
                if (error != null)
                {
                    DualLogger.Instance.Warning(nameof(CommandLifecycleEventReceiver), $"Event Hub error: on {this.identifier}: {error.GetType().Name}, {error.Message}");
                }

                return Task.FromResult(true);
            }

            /// <summary>
            /// Retry work item handler.
            /// </summary>
            async Task<QueueProcessResult> IAzureWorkItemQueueHandler<JsonEventData[]>.ProcessWorkItemAsync(QueueWorkItemWrapper<JsonEventData[]> wrapper)
            {
                var incomingEvent = IncomingEvent.Current;
                if (incomingEvent != null)
                {
                    incomingEvent.OperationName = $"{this.handlerTypeName}.ProcessRetryWorkItemAsync";
                }

                ICommandLifecycleCheckpointProcessor processor = this.processorFactory();

                foreach (var data in wrapper.WorkItem)
                {
                    IEnumerable<CommandLifecycleEvent> events = CommandLifecycleEventParser.ParseEvents(data);
                    foreach (var @event in events)
                    {
                        @event?.Process(processor);
                    }
                }

                // Note: we don't ask the processor if it's ready to checkpoint here; we just do it since
                // this is a retry.
                await processor.CheckpointAsync();
                return QueueProcessResult.Success();
            }
        }
    }
}
