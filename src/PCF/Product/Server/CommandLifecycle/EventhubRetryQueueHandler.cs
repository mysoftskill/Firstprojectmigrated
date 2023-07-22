namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Queue;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json;

    /// <summary>
    /// EventHub retry queue handler.
    /// Note: this is temporary solution to drain and process audit and raw commands retry queues.
    /// </summary>
    public class EventHubRetryQueueHandler
    {
        private readonly string eventHubRetryQueueName;
        private readonly string retryQueueName;
        private readonly string consummerGroup;

        private readonly IAzureCloudQueue retryQueueClient;
        private readonly string queueInfoString;

        public EventHubRetryQueueHandler(string consummerGroup)
        {
            this.consummerGroup = consummerGroup;

            // name formatting used in the EventProcessorFactory
            this.eventHubRetryQueueName = $"eh-rq-commandlifecycle-{consummerGroup}";
            this.retryQueueName = $"qn-retry-{consummerGroup}";

            var defaultLeasePeriod = TimeSpan.FromMinutes(5);
            this.retryQueueClient =
                Config.Instance.AzureStorageAccounts
                .Select(CloudStorageAccount.Parse)
                .Select(x => x.CreateCloudQueueClient())
                .Select(x => x.GetQueueReference(retryQueueName))
                .Select(x => new AzureQueueCloudQueue(x, defaultLeasePeriod))
                .First();

            this.retryQueueClient.EnsureQueueExistsAsync().Wait();
            this.queueInfoString = $"QName={this.retryQueueClient.QueueName}, Account={this.retryQueueClient.AccountName}";

            DualLogger.Instance.Information($"{nameof(EventHubRetryQueueHandler)}", $"{this.queueInfoString}.");
        }

        /// <summary>
        /// Copy queue items into backup queue.
        /// </summary>
        /// <returns></returns>
        public async Task RunEventHubRetryQueueBackupAsync(CancellationToken cancellationToken)
        {
            DualLogger.Instance.Information($"{nameof(RunEventHubRetryQueueBackupAsync)}", $"Starting {SourceLocation.Here().MemberName}.");

            var queue = new AzureWorkItemQueue<JsonEventData[]>(queueName: this.eventHubRetryQueueName);
            var handler = new InnerRetryQueueHandler(this);

            await queue.BeginProcessAsync(handler, cancellationToken);
        }

        /// <summary>
        /// Process records from backup queue.
        /// </summary>
        /// <param name="processor">Retry records processor</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        public async Task ProcessEventHubRetryQueueAsync(ICommandLifecycleCheckpointProcessor processor, CancellationToken cancellationToken)
        {
            List<CloudQueueMessage> processedItems = new List<CloudQueueMessage>();
            var defaultLeasePeriod = TimeSpan.FromMinutes(5);

            DualLogger.Instance.Information($"{nameof(ProcessEventHubRetryQueueAsync)}", $"Starting {SourceLocation.Here().MemberName}.");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Logger.InstrumentAsync(
                        new IncomingEvent(SourceLocation.Here()),
                        async ev =>
                        {
                            bool eventHubRetryQueueHandlerEnabled =
                                FlightingUtilities.IsStringValueEnabled(FlightingNames.EventHubRetryQueueHandlerEnabled, this.consummerGroup);

                            ev["QueueInfo"] = this.queueInfoString;
                            ev["QueueDepth"] = this.retryQueueClient.GetCountAsync(CancellationToken.None).Result.ToString();
                            ev[FlightingNames.EventHubRetryQueueHandlerEnabled] = eventHubRetryQueueHandlerEnabled.ToString();

                            if (!eventHubRetryQueueHandlerEnabled)
                            {
                                await Task.Delay(TimeSpan.FromSeconds(30));
                                ev.StatusCode = HttpStatusCode.OK;
                                return;
                            }

                            // Delay before calling GetMessagesAsync, min = 100 ms
                            // we have 218 PCF worker instances so lets also make sure they are not calling Azure Queue at the same time
                            int delay = FlightingUtilities.GetConfigValue<int>(ConfigNames.PCF.ProcessAuditLogQueueAsyncDelayInMs, defaultValue: 100);
                            delay = RandomHelper.Next(100, delay);

                            ev["DelayMs"] = delay.ToString();
                            await Task.Delay(TimeSpan.FromMilliseconds(delay));

                            // max = 32, default 10
                            int maxMessagesCount = FlightingUtilities.GetConfigValue<int>(ConfigNames.PCF.ProcessAuditLogQueueAsyncMaxMessageCount, defaultValue: 10);
                            var messages = await this.retryQueueClient.GetMessagesAsync(maxMessagesCount, defaultLeasePeriod);
                            ev["MaxMessagesCount"] = maxMessagesCount.ToString();
                            ev["MessagesCount"] = messages.Count().ToString();

                            if (!messages.Any())
                            {
                                await Task.Delay(TimeSpan.FromSeconds(30));
                                await this.CheckpointEventHubRetryProcessor(processor, processedItems).ConfigureAwait(false);
                                ev.StatusCode = HttpStatusCode.OK;
                                return;
                            }

                            foreach (var message in messages)
                            {
                                // Parse and collect all valid lifeCycle events from each message
                                var validEvents = this.ParseEventsFromEachQueueMessage(message);
                                foreach (var validEvent in validEvents)
                                {
                                    // Process each event
                                    validEvent?.Process(processor);
                                }

                                processedItems.Add(message);

                                // Calling it at higher frequency to avoid concurrent append error.
                                // Processor internally has size related checks to avoid calling to cosmos unnecessarily.
                                await this.CheckpointEventHubRetryProcessor(processor, processedItems).ConfigureAwait(false);
                            }
                            ev.StatusCode = HttpStatusCode.OK;
                        });
                }
                // catch, log and swallow
                catch (Exception ex)
                {
                    DualLogger.Instance.Error($"{nameof(this.ProcessEventHubRetryQueueAsync)}", ex, $"Error to process eventhub retry queue. {this.queueInfoString}.");
                    processedItems.Clear();
                }
            }
        }

        private async Task CheckpointEventHubRetryProcessor(ICommandLifecycleCheckpointProcessor processor, List<CloudQueueMessage> processedItems)
        {
            DualLogger.Instance.Information(nameof(EventHubRetryQueueHandler), $"Starting {SourceLocation.Here().MemberName}.");

            if (!processor.ShouldCheckpoint())
            {
                return;
            }

            try
            {
                await processor.CheckpointAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StringBuilder eventsInfo = new StringBuilder();
                // re-generate events for logging purpose
                foreach (var processItem in processedItems)
                {
                    var validEvents = this.ParseEventsFromEachQueueMessage(processItem);
                    validEvents.ForEach(e => eventsInfo.Append($"EventName={e.EventName},CommandId={e.CommandId},CommandType={e.CommandType},AgentId={e.AgentId},AssetGroupId={e.AssetGroupId}"));
                }
                
                DualLogger.Instance.Error(nameof(EventHubRetryQueueHandler), ex, $"Failed to checkpoint queue message. CommandLifecycleEvent Info={eventsInfo}");

                // don't continue to cleanup. Just return
                return;
            }

            // cleanup
            foreach (var message in processedItems)
            {
                try
                {
                    await this.retryQueueClient.DeleteMessageAsync(message);
                }
                catch (StorageException ex)
                {
                    // Most likely message does not exist - means processed by another worker, so continue processing.
                    DualLogger.Instance.Warning(nameof(EventHubRetryQueueHandler), ex, $"Warning: fail to delete queue message.");
                }
            }

            processedItems.Clear();
        }

        private List<CommandLifecycleEvent> ParseEventsFromEachQueueMessage(CloudQueueMessage message)
        {
            List<CommandLifecycleEvent> allValidEvents = new List<CommandLifecycleEvent>();
            JsonEventData[] data = Unpackage(message.AsBytes);
            foreach (var item in data)
            {
                try
                {
                    IEnumerable<CommandLifecycleEvent> events = CommandLifecycleEventParser.ParseEvents(item);
                    allValidEvents.AddRange(events);
                }
                catch (JsonReaderException)
                {
                    // If we have any issue with json deserialization during parsing, dump data properties here for troubleshooting
                    StringBuilder itemLog = new StringBuilder("Dumping item properties ->");
                    foreach (var property in item.Properties)
                    {
                        itemLog.Append($"key={property.Key},value={property.Value} ");
                    }
                    DualLogger.Instance.Information(nameof(EventHubRetryQueueHandler), $"{itemLog}");
                }
                catch (Exception ex)
                {
                    DualLogger.Instance.Error(nameof(EventHubRetryQueueHandler), ex, $"Unexpected exception while processing events.");
                }
            }

            return allValidEvents;
        }

        private class InnerRetryQueueHandler : IAzureWorkItemQueueHandler<JsonEventData[]>
        {
            private readonly string queueInfoString;
            private readonly IAzureCloudQueue retryQueue;
            private readonly string eventHubRetryQueueName;
            private readonly EventHubRetryQueueHandler eventHubRetryQueueHandler;

            public InnerRetryQueueHandler(EventHubRetryQueueHandler eventHubRetryQueueHandler)
            {
                this.eventHubRetryQueueHandler = eventHubRetryQueueHandler;
                this.eventHubRetryQueueName = eventHubRetryQueueHandler.eventHubRetryQueueName;
                this.retryQueue = eventHubRetryQueueHandler.retryQueueClient;
                this.queueInfoString = $"QName={this.retryQueue.QueueName}, Account={this.retryQueue.AccountName}";

                DualLogger.Instance.Information($"{SourceLocation.Here().MemberName}", $"Starting {SourceLocation.Here().MemberName}. {this.queueInfoString}");
            }

            /// <inheritdoc/>
            public SemaphorePriority WorkItemPriority => SemaphorePriority.Background;

            /// <inheritdoc/>
            public async Task<QueueProcessResult> ProcessWorkItemAsync(QueueWorkItemWrapper<JsonEventData[]> wrapper)
            {
                QueueProcessResult result = QueueProcessResult.Success();

                await Logger.InstrumentAsync(
                    new IncomingEvent(SourceLocation.Here()),
                    async ev =>
                    {
                        bool eventHubRetryQueueHandlerEnabled =
                            FlightingUtilities.IsStringValueEnabled(FlightingNames.EventHubRetryQueueHandlerEnabled, this.eventHubRetryQueueHandler.consummerGroup);

                        ev["EventHubRetryQueueName"] = this.eventHubRetryQueueName;
                        ev["EventDataCount"] = wrapper.WorkItem.Count().ToString();
                        ev["RetryQueue"] = this.queueInfoString;
                        ev["RetryQueueDepth"] = this.retryQueue.GetCountAsync(CancellationToken.None).Result.ToString();
                        ev[FlightingNames.EventHubRetryQueueHandlerEnabled] = eventHubRetryQueueHandlerEnabled.ToString();

                        if (!eventHubRetryQueueHandlerEnabled)
                        {
                            ev.StatusCode = HttpStatusCode.OK;

                            // keep the item in the queue and retry later
                            result = QueueProcessResult.TransientFailureRandomBackoff();
                            return;
                        }

                        var data = Package(wrapper.WorkItem);
                        // copy to internal queue
                        await this.retryQueue.AddMessageAsync(new CloudQueueMessage(data), TimeSpan.Zero);
                        ev.StatusCode = HttpStatusCode.OK;
                    });

                return result;
            }
        }

        private static JsonEventData[] Unpackage(byte[] body)
        {
            byte[] decompressedBytes = CompressionTools.Gzip.Decompress(body);
            string text = Encoding.UTF8.GetString(decompressedBytes);
            return JsonConvert.DeserializeObject<JsonEventData[]>(text);
        }

        private static byte[] Package(JsonEventData[] workItem)
        {
            string text = JsonConvert.SerializeObject(workItem);
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            byte[] compressedBytes = CompressionTools.Gzip.Compress(bytes);

            return compressedBytes;
        }
    }
}
