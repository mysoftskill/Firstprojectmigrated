// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.ComplianceServices.NonWindowsDeviceDeleteWorker
{
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Delete requests processor.
    /// </summary>
    public class DeleteRequestsProcessor : IDeleteRequestsProcessor
    {
        /// <summary>
        /// Not published device delete events map:
        /// partitionId -> Dictionary<DeviceId, JsonEvent>
        /// </summary>
        private ConcurrentDictionary<string, Dictionary<string, string>> partitionDeviceIdEventsMap;
        /// <summary>
        /// Not published delete requests map:
        /// partitionId -> List<PrivacyRequest>
        /// </summary>
        private ConcurrentDictionary<string, List<PrivacyRequest>> partitionDeleteRequestsMap;

        private readonly IAzureWorkItemQueuePublisher<PublishCommandBatchWorkItem> commandBatchWorkItemPublisher;
        private readonly IDataAgentMapFactory dataAgentMapFactory;
        private readonly ICommandLifecycleEventPublisher lifecycleEventPublisher;
        private readonly IDeviceIdCache deviceIdCache;

        private const int MaxPublishBatchSize = 10;

        /// <summary>
        /// Create Delete Requests Processor.
        /// </summary>
        public DeleteRequestsProcessor(
            IAzureWorkItemQueuePublisher<PublishCommandBatchWorkItem> commandBatchWorkItemPublisher,
            IDataAgentMapFactory dataAgentMapFactory,
            ICommandLifecycleEventPublisher lifecycleEventPublisher,
            IDeviceIdCache deviceIdCache)
        {
            this.commandBatchWorkItemPublisher = commandBatchWorkItemPublisher;
            this.dataAgentMapFactory = dataAgentMapFactory;
            this.lifecycleEventPublisher = lifecycleEventPublisher;
            this.deviceIdCache = deviceIdCache;
            this.partitionDeleteRequestsMap = new ConcurrentDictionary<string, List<PrivacyRequest>>();
            this.partitionDeviceIdEventsMap = new ConcurrentDictionary<string, Dictionary<string, string>>();
        }

        /// <inheritdoc />
        public async Task PublishDeleteRequests(string partitionId)
        {
            if (!this.partitionDeleteRequestsMap.ContainsKey(partitionId)
                || !this.partitionDeleteRequestsMap[partitionId].Any())
            {
                // nothing to publish
                return;
            }

            // Deferred requests visibility delay.
            // The delay has same value as a lease expiration in deviceIdCache.
            // This value is different for NON-PROD and PROD environments so it can be tested and verified.
            var visibilityDelay = TimeSpan.FromMinutes(Config.Instance.Worker.Tasks.NonWindowsDeviceWorker.DeviceIdExpirationMinutes);

            await Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async ev =>
                {
                    IEnumerable<PrivacyRequest> deleteRequests = this.partitionDeleteRequestsMap[partitionId];

                    ev["PartitionId"] = partitionId;
                    ev["VisibilityDelayMinutes"] = Config.Instance.Worker.Tasks.NonWindowsDeviceWorker.DeviceIdExpirationMinutes.ToString();
                    ev["EventsToPublish"] = deleteRequests.Count().ToString();

                    List<string> commandIds = new List<string>();

                    // publish with splits to PCF in batches
                    while (deleteRequests.Any())
                    {
                        var requestsToPublish = deleteRequests.Take(MaxPublishBatchSize);
                        IEnumerable<JObject> parsedJsonRequests = JsonConvert.DeserializeObject<JObject[]>(
                            JsonConvert.SerializeObject(requestsToPublish));

                        await this.commandBatchWorkItemPublisher
                            .PublishWithSplitAsync(parsedJsonRequests, this.CreateWorkItem, x => visibilityDelay);

                        deleteRequests = deleteRequests.Skip(MaxPublishBatchSize);

                        // life cycle publisher
                        await this.lifecycleEventPublisher.PublishCommandRawDataAsync(parsedJsonRequests.ToArray());

                        commandIds.AddRange(requestsToPublish.Select(r => r.RequestId.ToString("N")));
                    }

                    // Add published events to the cache
                    foreach (var item in this.partitionDeviceIdEventsMap[partitionId])
                    {
                        this.deviceIdCache.Add(item.Key, item.Value);
                    }

                    ev["DeviceIdsAddedToTheCache"] = this.partitionDeviceIdEventsMap[partitionId].Count().ToString();
                    string commandIdsStr = string.Join(";", commandIds);
                    ev["CommandIds"] = commandIdsStr;
                    DualLogger.Instance.Information(nameof(PublishDeleteRequests), $"CommandIds: {commandIdsStr}");

                    // Reset partition maps.
                    this.partitionDeviceIdEventsMap[partitionId] = new Dictionary<string, string>();
                    this.partitionDeleteRequestsMap[partitionId] = new List<PrivacyRequest>();
                });
        }

        /// <inheritdoc />
        public void ProcessDeleteRequestsFromJson(string partitionId, string jsonDeleteRequest)
        {
            Logger.InstrumentSynchronous(
                new IncomingEvent(SourceLocation.Here()),
                ev =>
                {
                    ev["Event"] = jsonDeleteRequest;

                    string deviceId = NonWindowsDeviceDeleteHelpers.GetDeviceIdFromJsonEvent(jsonDeleteRequest);
                    if (string.IsNullOrEmpty(deviceId))
                    {
                        // Should be detected in Incoming QoS
                        throw new InvalidOperationException($"Cannot get device id from event.");
                    }

                    ev["DeviceId"] = deviceId;

                    // Skip duplicates for cached request
                    if (this.IsCachedDeviceId(deviceId))
                    {
                        // We can skip this request because we already have deferred one 
                        // for the same device id in PCF queue or in the local cache.
                        // This flag can be used to create alerts or monitoring view 
                        // to identify spam attacks or tracking duplicates.
                        ev["Deferred"] = true.ToString();
                        ev.OperationStatus = OperationStatus.Succeeded;
                        ev.StatusCode = HttpStatusCode.OK;
                        return;
                    }

                    var requestGuid = Guid.NewGuid();
                    List<PrivacyRequest> newRequests = new List<PrivacyRequest>();

                    // Delete Request Guid
                    ev["RequestGuid"] = requestGuid.ToString();

                    Array.ForEach(
                        NonWindowsDeviceDeleteHelpers.SupportedDataTypeIds,
                        d => newRequests.Add(
                            NonWindowsDeviceDeleteHelpers.CreateDeleteRequestFromJson(jsonDeleteRequest, requestGuid, d)));

                    ev["CommandIds"] = string.Join(";", newRequests.Select(r => r.RequestId.ToString("N")));

                    // Add to delete requests
                    if (!this.partitionDeleteRequestsMap.ContainsKey(partitionId))
                    {
                        this.partitionDeleteRequestsMap[partitionId] = new List<PrivacyRequest>();
                    }
                    if (!this.partitionDeviceIdEventsMap.ContainsKey(partitionId))
                    {
                        this.partitionDeviceIdEventsMap[partitionId] = new Dictionary<string, string>();
                    }

                    this.partitionDeleteRequestsMap[partitionId].AddRange(newRequests);
                    // Add to local cache. We will add to global memory cache once it is published.
                    this.partitionDeviceIdEventsMap[partitionId].Add(deviceId, jsonDeleteRequest);

                    ev.OperationStatus = OperationStatus.Succeeded;
                    ev.StatusCode = HttpStatusCode.OK;
                });

            return;
        }

        private bool IsCachedDeviceId(string deviceId)
        {
            // Check global cache for already published requests
            if (this.deviceIdCache.Contains(deviceId))
            {
                return true;
            }

            // Check local cache for not published requests. 
            // This is relatively small list and cleaned up on every PublishDeleteRequests call (EventHub checkpoint).
            foreach (var partition in this.partitionDeviceIdEventsMap.Values)
            {
                if (partition.ContainsKey(deviceId))
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerable<PrivacyRequest> ProcessPrivacyRequestsFromJson(string jsonRequest)
        {
            var requestGuid = Guid.NewGuid();
            List<PrivacyRequest> privacyRequests = new List<PrivacyRequest>();

            Array.ForEach(
                NonWindowsDeviceDeleteHelpers.SupportedDataTypeIds,
                d => privacyRequests.Add(
                    NonWindowsDeviceDeleteHelpers.CreateDeleteRequestFromJson(jsonRequest, requestGuid, d)));

            return privacyRequests;
        }

        private PublishCommandBatchWorkItem CreateWorkItem(IEnumerable<JObject> requestsToSend)
        {
            return new PublishCommandBatchWorkItem
            {
                DataSetVersion = this.dataAgentMapFactory.GetDataAgentMap().Version,
                PxsCommands = requestsToSend.ToList(),
            };
        }
    }
}
