namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.EventHubs;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Helpers;
    using Microsoft.Azure.ComplianceServices.Common;

    /// <summary>
    /// Handles processing command lifecycle events received from Event Hub. This class is called from CommandLifecycleEventReceiver,
    /// but is kept separate for unit testing purposes.
    /// </summary>
    /// <remarks>
    /// This class provides resiliency in the following ways:
    ///   1) A buffer of all received messages since the last fully committed checkpoint is preserved.
    ///   2) A checkpoint may not be fully comitted while there are unprocessed messages (or failures).
    ///   3) A message is considered processed as soon as it is either committed to the underlying ICommandLifecycleCheckpointProcessor or inserted into a retry queue.
    /// </remarks>
    internal class CommandLifecycleReceiverSink
    {
        private readonly Func<ICommandLifecycleCheckpointProcessor> checkpointProcessorFactory;
        private ICommandLifecycleCheckpointProcessor checkpointProcessor;

        private readonly IAzureWorkItemQueuePublisher<JsonEventData[]> retryPublisher;
        private readonly string identifier;

        // State tracking for messages that we either failed to parse
        // or the checkpointProcessor failed to handle correctly.
        private readonly HashSet<JsonEventData> failedMessages;
        private long failedMessageLength;

        // The set of all messages that we have yet to successfully "commit" with the checkpointProcessor.
        // We can't move the event hub cursor forward until all of these have been acknowedged as committed by the
        // underlying processor.
        private readonly HashSet<JsonEventData> allMessages;

        // Properties needed to create EventHubClient and PartitionSender
        public string EventHubConnectionString { get; set; }
        public string EventHubPartnerId { get; set; }

        private ExponentialBackoff backoff;

        public CommandLifecycleReceiverSink(
            Func<ICommandLifecycleCheckpointProcessor> checkpointProcessorFactory,
            IAzureWorkItemQueuePublisher<JsonEventData[]> retryPublisher,
            string identifier)
        {
            this.identifier = identifier;
            this.checkpointProcessorFactory = checkpointProcessorFactory;
            this.checkpointProcessor = this.checkpointProcessorFactory();

            this.retryPublisher = retryPublisher;

            this.allMessages = new HashSet<JsonEventData>();

            this.failedMessages = new HashSet<JsonEventData>();
            this.failedMessageLength = 0;
        }

        /// <summary>
        /// Handles a batch of events from Event Hub. Checks for consecutive failures and restarts if needed.
        /// </summary>
        /// <param name="messages">The event hub messages.</param>
        /// <param name="commitCallback">The event hub checkpoint callback.</param>
        public async Task HandleEventBatchAsync(
            IEnumerable<JsonEventData> messages,
            Func<Task> commitCallback)
        {
            var incomingEvent = IncomingEvent.Current;
            incomingEvent?.SetProperty("EventHubId", this.identifier);

            // Next: Attempt to process one-by-one. If there are exceptions, add those raw messages to the queue of retries.
            foreach (var message in messages)
            {
                this.allMessages.Add(message);

                try
                {
                    IEnumerable<CommandLifecycleEvent> events = CommandLifecycleEventParser.ParseEvents(message);
                    foreach (var @event in events)
                    {
                        @event?.Process(this.checkpointProcessor);
                    }
                }
                catch (Exception ex)
                {
                    incomingEvent?.SetForceReportAsFailed(true);
                    incomingEvent?.SetException(ex);
                    incomingEvent?.SetProperty("FailedParse", "true");

                    this.CopyToFailedBuffer(message);
                }
            }

            incomingEvent?.SetProperty("FailedMessageDepth", this.failedMessages.Count.ToString());

            // Finally, ask our processor if it's time to actually do a checkpoint.
            if (this.checkpointProcessor.ShouldCheckpoint())
            {
                // Ask the processor to checkpoint itself. If this fails, then
                // we need to replay the entire batch since the last checkpoint.
                // This is a more serious failure, since the processor failed to do its job.
                // It's possible that the error is transient, in which case a retry will help.
                // However, if the error is more severe, such as an unhandled exception, 
                // then we need to replay from the last checkpoint time.
                try
                {
                    DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(CommandLifecycleReceiverSink), $"Checkpointing initiated for {this.identifier}");

                    await this.CheckpointProcessorCheckpointAsync().TimeoutAfter(TimeSpan.FromMinutes(FlightingUtilities.GetConfigValue<int>(ConfigNames.PCF.MaxWaitTimeForCheckpointInMinutes, defaultValue: 15)));

                    DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(CommandLifecycleReceiverSink), $"Checkpointing completed for {this.identifier}");
                }
                catch (TimeoutException ex)
                {
                    DualLogger.Instance.LogErrorForCommandLifeCycle(nameof(CommandLifecycleReceiverSink), ex, "Timeout exception in CheckpointProcessorCheckpointAsync");
                    throw;
                }
                catch (Exception ex)
                {
                    incomingEvent?.SetProperty("CheckpointFailed", "true");
                    incomingEvent?.SetForceReportAsFailed(true);
                    incomingEvent?.SetException(ex);

                    DualLogger.Instance.LogErrorForCommandLifeCycle(nameof(CommandLifecycleReceiverSink), ex, "Failed to checkpoint, will try retry queue or re-publish to eventhub");
                    // Assume state is corrupted and just create a brand new processor object.
                    this.checkpointProcessor = this.checkpointProcessorFactory();

                    // Make sure the failed message buffer contains all the messages since we last succeeded, 
                    // since the whole checkpoint failed. If we fail to publish the retry work item, we want to keep failed messages full.
                    // So that future checkpoints don't occur until the failed message buffer is flushed.
                    foreach (var message in this.allMessages)
                    {
                        this.CopyToFailedBuffer(message);
                    }

                    // All of the messages have been moved into the 'failed' buffer.
                    // This means that those must be committed to the retry queue before
                    // we can make further progress.
                    this.allMessages.Clear();
                }

                DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(CommandLifecycleReceiverSink), $"awaiting on PublishRetriesAsync for {this.identifier}");

                // Next, publish anything in the failed buffer. If this fails, it throws an exception,
                // which will keep the buffer in place, which will prevent future checkpoints on the event hub.
                await this.PublishRetriesAsync().ConfigureAwait(false);

                // We have affirmatively committed the entries. We can now clear the "pending" messages.
                this.allMessages.Clear();

                DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(CommandLifecycleReceiverSink), $"awaiting on EventHubCheckpointAsync for {this.identifier}");

                // Everything is either in a retry queue or has been handled successfully. Let's advance the eventhub cursor.
                await EventHubCheckpointAsync(commitCallback);
            }
            else if (this.failedMessageLength > 100 * 1024)
            {
                DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(CommandLifecycleReceiverSink), $"Publishing retries started for {this.identifier}");
                await this.PublishRetriesAsync().ConfigureAwait(false);
                DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(CommandLifecycleReceiverSink), $"Publishing retries completed for {this.identifier}");
            }

            // Would have thrown by now if we failed.
            if (incomingEvent != null)
            {
                incomingEvent.StatusCode = System.Net.HttpStatusCode.OK;
            }
        }

        internal async Task PublishRetriesAsync()
        {
            if (this.failedMessages.Count > 0)
            {
                try
                {
                    int publishToQueueAttempts = 0;
                    var backoff = new ExponentialBackoff
                        (delay: TimeSpan.FromSeconds(1), 
                        maxDelay: TimeSpan.FromSeconds(13),
                        delayStartsWith: TimeSpan.FromSeconds(5));

                    while (true)
                    {
                        publishToQueueAttempts++;
                        try
                        {   
                            await PublishRetriesAsync(this.retryPublisher, this.failedMessages).ConfigureAwait(false);

                            // successfully makes it here, exits the loop
                            break;
                        }
                        catch (Exception e)
                        {
                            if (publishToQueueAttempts >= 4)
                            {
                                // Exceptions can be swallowed here. If the publish to the retry fails, then we don't remove from our internal list and we don't
                                // proceed with checkpointing the event hub.
                                IncomingEvent.Current?.SetProperty("RetryPublishFailed", "true");
                                DualLogger.Instance.Error(nameof(CommandLifecycleReceiverSink), e, $"Reached the azure queue publishing retry limits. Publish attempts={publishToQueueAttempts}");
                                throw;
                            }

                            // wait for some time and retry
                            DualLogger.Instance.Warning(nameof(CommandLifecycleReceiverSink), $"Failed to publish to azure queue. Sleeping for a couple seconds and retry. ");
                            await backoff.Delay().ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    DualLogger.Instance.Error(nameof(CommandLifecycleReceiverSink), ex, $"Published retries to azure queue failed. Publishing to EventHub.");
                    await PublishRetriesToEventHubAsync().ConfigureAwait(false);
                }
                finally
                {
                    this.failedMessageLength = 0;
                    this.failedMessages.Clear();
                }
            }
        }

        internal Task CheckpointProcessorCheckpointAsync()
        {
            return Logger.InstrumentAsync(new OutgoingEvent(SourceLocation.Here()), ev => this.checkpointProcessor.CheckpointAsync());
        }

        internal static Task EventHubCheckpointAsync(Func<Task> commitCallback)
        {
            return Logger.InstrumentAsync(new OutgoingEvent(SourceLocation.Here()), ev => commitCallback());
        }

        internal static Task PublishRetriesAsync(IAzureWorkItemQueuePublisher<JsonEventData[]> publisher, IEnumerable<JsonEventData> items)
        {
            return Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                ev =>
                {
                    return publisher.PublishWithSplitAsync(
                        items,
                        x => x.ToArray(),
                        x => TimeSpan.Zero);
                });
        }

        private void CopyToFailedBuffer(JsonEventData message)
        {
            if (this.failedMessages.Add(message))
            {
                this.failedMessageLength += message?.Data?.Length ?? 0;
            }
        }

        internal async Task PublishRetriesToEventHubAsync()
        {
            EventHubClient client = EventHubClient.CreateFromConnectionString(this.EventHubConnectionString);
            var partitionSender = client.CreatePartitionSender(this.EventHubPartnerId);
            var batch = partitionSender.CreateBatch();

            var idx = 0;
            while (idx < this.failedMessages.Count)
            {
                var eventData = this.failedMessages.ElementAt(idx).ToEventData();
                if (!batch.TryAdd(eventData))
                {
                    DualLogger.Instance.Warning(nameof(CommandLifecycleReceiverSink), "The current batch is full.");
                    await SendBatchToEventHub(partitionSender, batch);
                    batch = partitionSender.CreateBatch();
                }
                else
                {
                    idx++;
                }
            }

            // take care of the last batch
            if (batch.Count > 0)
            {
                await SendBatchToEventHub(partitionSender, batch);
            }

            await partitionSender.CloseAsync();
            await client.CloseAsync();
        }

        internal async Task SendBatchToEventHub(PartitionSender partitionSender, EventDataBatch batch)
        {
            try
            {
                await partitionSender.SendAsync(batch);
                DualLogger.Instance.Verbose(nameof(CommandLifecycleReceiverSink), $"Sent batch of eventData successfully.");
            }
            catch (MessageSizeExceededException ex)
            {
                // We shouldn't see this exception anymore since we do a check when trying to add into a batch
                DualLogger.Instance.Error(nameof(CommandLifecycleReceiverSink), $"Total size of the EventData exceeds a pre-defined limit set by the service. {ex}");
            }
            catch (EventHubsException ex)
            {
                DualLogger.Instance.Error(nameof(CommandLifecycleReceiverSink), $"Event Hubs service encountered problems during the operation. {ex}");
            }
            catch (Exception ex)
            {
                DualLogger.Instance.Error(nameof(CommandLifecycleReceiverSink), $"Failed to publish to EventHub. {ex}");
            }
        }
    }
}
