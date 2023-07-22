// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.ComplianceServices.NonWindowsDeviceDeleteWorker
{
    using global::Azure.Messaging.EventHubs;
    using global::Azure.Messaging.EventHubs.Producer;
    using global::Azure.Storage.Blobs;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen;
    using Microsoft.PrivacyServices.Common.Azure;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class EventHubProcessor : IEventHubProcessor
    {
        private readonly IEventHubConfig eventHubConfig;
        private IEventHubProcessorHandler eventHubProcessorHandler;

        public EventHubProcessor(IEventHubConfig eventHubConfig)
        {
            this.eventHubConfig = eventHubConfig;
        }

        /// <inheritdoc />
        public async Task SendAsync(string message)
        {
            List<EventData> events = new List<EventData>();
            EventData eventData = new EventData(Encoding.UTF8.GetBytes(message));

            eventData.Properties["User-Agent"] = "PCFWorker";
            eventData.Properties["X-Served-By"] = Environment.MachineName;

            events.Add(eventData);
            await this.SendBatchesAsync(events);
        }

        /// <inheritdoc />
        public async Task RunAsync(IEventHubProcessorHandler eventHubProcessorHandler, CancellationToken taskCancellationToken)
        {
            this.eventHubProcessorHandler = eventHubProcessorHandler;
            EventProcessorClient eventProcessorClient = null;

            try
            {
                // Create our Event Processor client, specifying the maximum wait time as an option to ensure that
                // our handler is invoked when no event was available.
                // Each machine performs this call, so the observed outgoing rate is a multiple of the rate below.
                const int maximumWaitTimeSecs = 60;
                EventProcessorClientOptions clientOptions = new EventProcessorClientOptions
                {
                    MaximumWaitTime = TimeSpan.FromSeconds(maximumWaitTimeSecs)
                };

                BlobContainerClient storageClient = new BlobContainerClient(
                    connectionString: this.eventHubConfig.StorageConnectionString,
                    blobContainerName: this.eventHubConfig.BlobContainerName);

                storageClient.CreateIfNotExists();

                eventProcessorClient = new EventProcessorClient(
                    checkpointStore: storageClient,
                    consumerGroup: this.eventHubConfig.ConsumerGroupName,
                    connectionString: this.eventHubConfig.ConnectionString,
                    eventHubName: this.eventHubConfig.EventHubName,
                    clientOptions: clientOptions);

                eventProcessorClient.ProcessEventAsync += this.eventHubProcessorHandler.ProcessEventHandlerAsync;
                eventProcessorClient.ProcessErrorAsync += this.eventHubProcessorHandler.ProcessErrorHandler;
                eventProcessorClient.PartitionInitializingAsync += this.eventHubProcessorHandler.PartitionInitializingHandlerAsync;
                eventProcessorClient.PartitionClosingAsync += this.eventHubProcessorHandler.PartitionClosingHandlerAsync;

                await eventProcessorClient.StartProcessingAsync(taskCancellationToken);

                // 1 sec loop until canceled
                while (!taskCancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1000));
                }

                // this one triggers PartitionClosingHandlerAsync
                await eventProcessorClient.StopProcessingAsync();
                await this.eventHubProcessorHandler.CompleteAsync();
            }
            catch (Exception ex)
            {
                DualLogger.Instance.Error(nameof(RunAsync), ex, "Fail to RunAsync NonWindowsDevice worker.");
                throw;
            }
            finally
            {
                if (eventProcessorClient != null)
                {
                    eventProcessorClient.ProcessEventAsync -= this.eventHubProcessorHandler.ProcessEventHandlerAsync;
                    eventProcessorClient.ProcessErrorAsync -= this.eventHubProcessorHandler.ProcessErrorHandler;
                    eventProcessorClient.PartitionInitializingAsync -= this.eventHubProcessorHandler.PartitionInitializingHandlerAsync;
                    eventProcessorClient.PartitionClosingAsync -= this.eventHubProcessorHandler.PartitionClosingHandlerAsync;
                }
            }
        }

        private async Task SendBatchesAsync(IEnumerable<EventData> eventDatas)
        {
            await Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async ev =>
                {
                    ev["EventHubName"] = this.eventHubConfig.EventHubName;
                    ev["EventHubMoniker"] = this.eventHubConfig.Moniker;
                    ev["BlobContainerName"] = this.eventHubConfig.BlobContainerName;
                    ev["BatchSize"] = eventDatas.Count().ToString();

                    await using (var producer = new EventHubProducerClient(this.eventHubConfig.ConnectionString, this.eventHubConfig.EventHubName))
                    {
                        using EventDataBatch eventDataBatch = await producer.CreateBatchAsync();
                        foreach (var eventData in eventDatas)
                        {
                            eventDataBatch.TryAdd(eventData);
                        }

                        await producer.SendAsync(eventDataBatch);
                    }
                });
        }
    }
}
