namespace Microsoft.Azure.ComplianceServices.NonWindowsDeviceDeleteWorker
{
    using global::Azure.Messaging.EventHubs.Processor;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class EventHubProcessorHandler : IEventHubProcessorHandler
    {
        private readonly ConcurrentDictionary<string, ProcessEventArgs> partitionCheckpoints;
        private readonly IDeleteRequestsProcessor deleteRequestsProcessor;
        private readonly IEventHubConfig eventHubConfig;
        private int totalPartitionsOwned = 0;
        private DateTimeOffset nextCheckpointDateTime;
        private readonly TimeSpan eventHubCheckpointExpiration;

        public EventHubProcessorHandler(IDeleteRequestsProcessor deleteRequestsProcessor, IEventHubConfig eventHubConfig)
        {
            this.eventHubConfig = eventHubConfig;
            this.deleteRequestsProcessor = deleteRequestsProcessor;
            this.partitionCheckpoints = new ConcurrentDictionary<string, ProcessEventArgs>();
            this.eventHubCheckpointExpiration = TimeSpan.FromSeconds(Config.Instance.Worker.Tasks.NonWindowsDeviceWorker.EventHubCheckpointExpirationSeconds);
            this.nextCheckpointDateTime = DateTimeOffset.UtcNow.Add(this.eventHubCheckpointExpiration);
        }

        /// <inheritdoc />
        public Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
        {
            if (eventArgs.CancellationToken.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            // These errors are mostly internal noise so log them to trace logger. 
            DualLogger.Instance.Error(
                nameof(ProcessErrorHandler),
                eventArgs.Exception,
                $"EventHub process event error. Partition={eventArgs.PartitionId};Operation={eventArgs.Operation}");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task ProcessEventHandlerAsync(ProcessEventArgs eventArgs)
        {
            if (eventArgs.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            string partitionId = eventArgs.Partition.PartitionId;
            bool isCompressionEnabled = false;

            if (!eventArgs.HasEvent)
            {
                if (this.ShouldCheckPoint())
                {
                    await this.deleteRequestsProcessor.PublishDeleteRequests(partitionId);
                    await this.UpdateCheckpointAsync(partitionId);
                }

                return;
            }

            // Update checkpoint record
            this.partitionCheckpoints[partitionId] = eventArgs;

            await Logger.InstrumentAsync(
                new IncomingEvent(SourceLocation.Here()),
                async ev =>
                {
                    ev["EventHubName"] = this.eventHubConfig.EventHubName;
                    ev["EventHubMoniker"] = this.eventHubConfig.Moniker;
                    ev["ConsumerGroupName"] = this.eventHubConfig.ConsumerGroupName;
                    ev["PartitionId"] = partitionId;
                    ev["EnqueuedTime"] = eventArgs.Data.EnqueuedTime.ToString();
                    ev["BodyLength"] = eventArgs.Data.Body.Length.ToString();

                    object eventHubProp;
                    int numOfInvalidJsonEvents = 0;
                    if (eventArgs.Data.Properties.TryGetValue("User-Agent", out eventHubProp))
                    {
                        ev["User-Agent"] = (string)eventHubProp;
                    }
                    if (eventArgs.Data.Properties.TryGetValue("X-Served-By", out eventHubProp))
                    {
                        ev["X-Served-By"] = (string)eventHubProp;
                    }
                    if (eventArgs.Data.Properties.TryGetValue("Content-Encoding", out eventHubProp))
                    {
                        ev["Content-Encoding"] = (string)eventHubProp;
                        isCompressionEnabled = true;
                    }
                    else
                    {
                        ev["Content-Encoding"] = "uncompressed";
                    }

                    byte[] body;

                    if (isCompressionEnabled)
                    {
                        body = CompressionTools.Gzip.Decompress(eventArgs.Data.Body.ToArray());
                    }
                    else
                    {
                        body = eventArgs.Data.Body.ToArray();
                    }

                    string content = Encoding.UTF8.GetString(body);
                    JObject parsedJson = JObject.Parse(content);
                    var parsedEvents = (JArray)parsedJson["Events"];

                    List<string> jsonEvents = new List<string>();
                    // is it a single or multiply combined events?
                    if (parsedEvents == null)
                    {
                        // Should be detected in Incoming QoS
                        jsonEvents.Add(content);
                    }
                    else
                    {
                        jsonEvents.AddRange(parsedEvents.Select(e => (string)e));
                    }
                    ev["NumOfDeleteRequests"] = jsonEvents.Count().ToString();

                    foreach (var jsonEvent in jsonEvents)
                    {
                        try
                        {
                            if (!NonWindowsDeviceDeleteHelpers.IsJsonEventValid(jsonEvent, out var reason))
                            {
                                DualLogger.Instance.Error(nameof(ProcessEventHandlerAsync), $"Json event is not valid: {reason}.");
                                numOfInvalidJsonEvents++;
                                continue;
                            }

                            this.deleteRequestsProcessor.ProcessDeleteRequestsFromJson(partitionId, jsonEvent);
                        }
                        catch (Exception ex)
                        {
                            // Log exception and continue processing
                            DualLogger.Instance.Error(nameof(ProcessEventHandlerAsync), ex, $"Fail to process delete event: {jsonEvent}");
                        }
                    }

                    ev["NumOfInvalidJsonEvents"] = numOfInvalidJsonEvents.ToString();


                    if (this.ShouldCheckPoint())
                    {
                        await this.deleteRequestsProcessor.PublishDeleteRequests(partitionId);
                        await this.UpdateCheckpointAsync(partitionId);
                    }

                    ev.OperationStatus = OperationStatus.Succeeded;
                    ev.StatusCode = HttpStatusCode.OK;
                });

            return;
        }

        /// <inheritdoc />
        public async Task PartitionInitializingHandlerAsync(PartitionInitializingEventArgs eventArgs)
        {
            if (eventArgs.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            DualLogger.Instance.Information(nameof(PartitionInitializingHandlerAsync), $"PartitionInitializingHandlerAsync: Moniker={this.eventHubConfig.Moniker};EventHub={this.eventHubConfig.EventHubName};Partition={eventArgs.PartitionId}");

            await Logger.InstrumentAsync(
                new IncomingEvent(SourceLocation.Here()),
                async ev =>
                {
                    Interlocked.Increment(ref this.totalPartitionsOwned);

                    ev["EventHubName"] = this.eventHubConfig.EventHubName;
                    ev["EventHubMoniker"] = this.eventHubConfig.Moniker;
                    ev["ConsumerGroupName"] = this.eventHubConfig.ConsumerGroupName;
                    ev["PartitionId"] = eventArgs.PartitionId;
                    ev["TotalPartitionsOwned"] = this.totalPartitionsOwned.ToString();

                    await Task.Yield();
                    ev.OperationStatus = OperationStatus.Succeeded;
                    ev.StatusCode = HttpStatusCode.OK;
                });
        }

        /// <inheritdoc />
        public async Task PartitionClosingHandlerAsync(PartitionClosingEventArgs eventArgs)
        {
            if (eventArgs.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            DualLogger.Instance.Information(nameof(PartitionClosingHandlerAsync), $"PartitionClosingHandlerAsync: Moniker={this.eventHubConfig.Moniker};EventHub={this.eventHubConfig.EventHubName};Partition={eventArgs.PartitionId}");

            await Logger.InstrumentAsync(
                new IncomingEvent(SourceLocation.Here()),
                async ev =>
                {
                    Interlocked.Decrement(ref this.totalPartitionsOwned);

                    ev["EventHubName"] = this.eventHubConfig.EventHubName;
                    ev["EventHubMoniker"] = this.eventHubConfig.Moniker;
                    ev["ConsumerGroupName"] = this.eventHubConfig.ConsumerGroupName;
                    ev["PartitionId"] = eventArgs.PartitionId;
                    ev["TotalPartitionsOwned"] = this.totalPartitionsOwned.ToString();

                    await this.deleteRequestsProcessor.PublishDeleteRequests(eventArgs.PartitionId);
                    await this.UpdateCheckpointAsync(eventArgs.PartitionId);

                    ev.OperationStatus = OperationStatus.Succeeded;
                    ev.StatusCode = HttpStatusCode.OK;
                });
        }

        /// <inheritdoc />
        public async Task CompleteAsync()
        {
            // Check every second if all partitions closed
            while (this.totalPartitionsOwned > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private bool ShouldCheckPoint()
        {
            if (this.nextCheckpointDateTime <= DateTimeOffset.UtcNow)
            {
                this.nextCheckpointDateTime = DateTimeOffset.UtcNow.Add(this.eventHubCheckpointExpiration);
                return true;
            }

            return false;
        }

        private async Task UpdateCheckpointAsync(string partitionId)
        {
            if (!this.partitionCheckpoints.ContainsKey(partitionId))
            {
                return;
            }

            DualLogger.Instance.Information(nameof(UpdateCheckpointAsync), $"UpdateCheckpointAsync: EventHub={this.eventHubConfig.EventHubName};Partition={partitionId}");

            await Logger.InstrumentAsync(
                new IncomingEvent(SourceLocation.Here()),
                async ev =>
                {
                    ev["EventHubName"] = this.eventHubConfig.EventHubName;
                    ev["EventHubMoniker"] = this.eventHubConfig.Moniker;
                    ev["ConsumerGroupName"] = this.eventHubConfig.ConsumerGroupName;
                    ev["PartitionId"] = partitionId;
                    ev["TotalPartitionsOwned"] = this.totalPartitionsOwned.ToString();

                    await this.partitionCheckpoints[partitionId].UpdateCheckpointAsync();

                    // Remove partition from checkpoint list.
                    if (!this.partitionCheckpoints.TryRemove(partitionId, out _))
                    {
                        throw new InvalidOperationException($"{nameof(UpdateCheckpointAsync)}: Fail to remove partition checkpoint from dictionary.");
                    }

                    ev.OperationStatus = OperationStatus.Succeeded;
                    ev.StatusCode = HttpStatusCode.OK;
                });
        }
    }
}
