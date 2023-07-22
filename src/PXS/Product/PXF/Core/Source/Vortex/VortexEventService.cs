// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Vortex
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AnaheimIdAdapter;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex.Event;
    using Microsoft.Membership.MemberServices.Privacy.Core.PrivacyCommand;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class DelayedDeviceDeleteRequest
    {
        internal DeviceDeleteRequest DeleteRequest { get; set; }

        internal TimeSpan? VisibilityDelay { get; set; }
    }

    internal class ThrottleTableRow
    {
        internal long Id { get; set; }

        internal bool AllowResend { get; set; } = true;

        internal DateTimeOffset InitialRequest { get; set; }
    }

    /// <inheritdoc />
    /// <summary>
    ///     Handles incoming vortex events
    /// </summary>
    /// <seealso cref="T:Microsoft.Membership.MemberServices.Privacy.Core.Vortex.IVortexEventService" />
    public class VortexEventService : IVortexEventService
    {
        /// <summary>
        ///     The component name
        /// </summary>
        private const string ComponentName = nameof(VortexEventService);

        /// <summary>
        ///     The device delete request queue
        /// </summary>
        private readonly IVortexDeviceDeleteQueueManager deviceDeleteRequestQueue;

        /// <summary>
        ///     The logger
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        ///     The msa identity service adapter instance
        /// </summary>
        private readonly IMsaIdentityServiceAdapter msaIdentityServiceAdapter;

        /// <summary>
        ///     The PCF adapter instance
        /// </summary>
        private readonly IPcfAdapter pcfAdapter;

        /// <summary>
        ///     The policy instance to get policy information from
        /// </summary>
        private readonly Policy policy;

        private readonly int timeoutLengthForUserRequests;

        private readonly int timeoutLengthForSystemRequests;

        private readonly ICounterFactory counterFactory;

        private readonly IAnaheimIdAdapter anaheimIdAdapter;

        private readonly IRedisClient redisClient;

        private readonly IAppConfiguration appConfiguration;

        /// <summary>
        ///     Initializes a new instance of the <see cref="VortexEventService" /> class.
        /// </summary>
        ///     The delete request writer.
        /// <param name="msaIdentityServiceAdapter">
        ///     The msa identity service adapter.
        /// </param>
        /// <param name="pcfAdapter">
        ///     The PCF Adapter.
        /// </param>
        /// <param name="deviceDeleteRequestQueue">The queue for device deletes</param>
        /// <param name="policy"><see cref="Policy" /> instance to use</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="config">Vortex specific configuration</param>
        /// <param name="counterFactory">Factory for creating perf counters</param>
        /// <param name="redisClient">RedisClient instance</param>
        /// <param name="anaheimIdAdapter"></param>
        /// <param name="appConfiguration"></param>
        public VortexEventService(
            IPcfAdapter pcfAdapter,
            IVortexDeviceDeleteQueueManager deviceDeleteRequestQueue,
            Policy policy,
            IMsaIdentityServiceAdapter msaIdentityServiceAdapter,
            ILogger logger,
            IPrivacyConfigurationManager config,
            ICounterFactory counterFactory,
            IRedisClient redisClient,
            IAnaheimIdAdapter anaheimIdAdapter,
            IAppConfiguration appConfiguration)
        {
            this.pcfAdapter = pcfAdapter ?? throw new ArgumentNullException(nameof(pcfAdapter));
            this.deviceDeleteRequestQueue = deviceDeleteRequestQueue ?? throw new ArgumentNullException(nameof(deviceDeleteRequestQueue));
            this.policy = policy;
            this.msaIdentityServiceAdapter = msaIdentityServiceAdapter ?? throw new ArgumentNullException(nameof(msaIdentityServiceAdapter));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            config = config ?? throw new ArgumentNullException(nameof(config));

            IVortexEndpointConfiguration vortexConfig = config.PrivacyExperienceServiceConfiguration?.VortexEndpointConfiguration ??
                                                        throw new ArgumentNullException(nameof(config.PrivacyExperienceServiceConfiguration.VortexEndpointConfiguration));
            this.timeoutLengthForUserRequests = vortexConfig.TimeBetweenUserRequestsLimitMinutes;
            this.timeoutLengthForSystemRequests = vortexConfig.TimeBetweenNonUserRequestsLimitMinutes;
            this.counterFactory = counterFactory ?? throw new ArgumentNullException(nameof(counterFactory));
            this.redisClient = redisClient ?? throw new ArgumentNullException(nameof(redisClient));
            this.anaheimIdAdapter = anaheimIdAdapter ?? throw new ArgumentNullException(nameof(anaheimIdAdapter));
            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
        }

        /// <inheritdoc />
        /// <summary>
        ///     Handles device delete requests in batch
        /// </summary>
        /// <param name="requests">The requests.</param>
        /// <returns>
        ///     A collection of service responses indicating success or failure of each batch item
        /// </returns>
        public async Task<IEnumerable<ServiceResponse<IQueueItem<DeviceDeleteRequest>>>> DeleteDevicesAsync(IEnumerable<IQueueItem<DeviceDeleteRequest>> requests)
        {
            var response = new List<ServiceResponse<IQueueItem<DeviceDeleteRequest>>>();
            foreach (IQueueItem<DeviceDeleteRequest> request in requests)
            {
                var res = new ServiceResponse<IQueueItem<DeviceDeleteRequest>>
                {
                    Result = request
                };

                try
                {
                    string json;
                    using (var stream = new StreamReader(new MemoryStream(request.Data.Data)))
                    {
                        json = await stream.ReadToEndAsync().ConfigureAwait(false);
                    }

                    JObject evt = JObject.Parse(json);

                    var eventProcessor =
                        new VortexEventProcessor(
                            evt.ToObject<VortexEvent>(),
                            request.Data.RequestInformation,
                            this.policy,
                            request.Data.RequestId,
                            this.msaIdentityServiceAdapter,
                            this.logger);

                    ServiceResponse<VortexEventProcessingResults> result = await eventProcessor.ProcessAsync().ConfigureAwait(false);
                    if (result.IsSuccess)
                    {
                        AdapterResponse adapterResponse =
                            await this.pcfAdapter.PostCommandsAsync(result.Result.PcfDeleteRequests.Cast<PrivacyRequest>().ToList()).ConfigureAwait(false);
                        if (!adapterResponse.IsSuccess)
                        {
                            res.Error = new Error(ErrorCode.PartnerError, adapterResponse.Error.Message);
                            response.Add(res);
                            continue;
                        }
                    }
                    else
                    {
                        res.Error = result.Error;
                    }
                }
                catch (Exception e)
                {
                    res.Error = new Error(ErrorCode.Unknown, e.Message);
                }

                response.Add(res);
            }

            return response;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Queues the valid events to be processed by worker
        /// </summary>
        /// <param name="vortexEventJson">The vortex event json.</param>
        /// <param name="info">The request information.</param>
        /// <returns>A service response indicating success or failure</returns>
        public async Task<ServiceResponse> QueueValidEventsAsync(byte[] vortexEventJson, VortexRequestInformation info)
        {
            this.logger.MethodEnter(ComponentName, nameof(this.QueueValidEventsAsync), info.ToMessage());
            try
            {
                string json;
                using (var stream = new StreamReader(new MemoryStream(vortexEventJson)))
                {
                    json = await stream.ReadToEndAsync().ConfigureAwait(false);
                }

                JObject events = JObject.Parse(json);

                JToken eventToken = events["Events"];

                if (eventToken == null)
                {
                    var response = new ServiceResponse { Error = new Error(ErrorCode.InvalidInput, "JSON missing Events data") };
                    this.logger.Error(ComponentName, response.Error.Message);

                    return response;
                }

                var requests = await this.FilterAllowedEventsAsync(eventToken, info).ConfigureAwait(false);
                if (!requests.Any())
                {
                    // Everything was filtered out
                    this.logger.MethodSuccess(ComponentName, nameof(this.QueueValidEventsAsync));
                    return new ServiceResponse();
                }

                await Task.WhenAll(requests.Select(r => this.deviceDeleteRequestQueue.EnqueueAsync(r.DeleteRequest, r.VisibilityDelay, CancellationToken.None)))
                    .ConfigureAwait(false);

                // Success
                IncrementRequestCounter(this.counterFactory, "Enqueued Requests", CounterType.Rate, (ulong)requests.Count);

                this.logger.MethodSuccess(ComponentName, nameof(this.QueueValidEventsAsync));
                return new ServiceResponse();
            }
            catch (Exception e)
            {
                this.logger.MethodException(ComponentName, nameof(this.QueueValidEventsAsync), e);

                return new ServiceResponse { Error = new Error(ErrorCode.Unknown, e.Message) };
            }
        }

        /// <summary>
        ///     Creates and sends DeleteDeviceIdRequest
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<bool> SendAnaheimDeviceDeleteIdRequestAsync(DeviceDeleteRequest request)
        {
            VortexEvent evt = JsonConvert.DeserializeObject<VortexEvent>(Encoding.UTF8.GetString(request.Data));
            DeleteDeviceIdRequest deleteDeviceIdRequest = PrivacyRequestConverter.CreateAnaheimDeleteDeviceIdRequest(evt, request.RequestId, request.RequestInformation.RequestTime, testSignal: false);

            var response = await this.anaheimIdAdapter.SendDeleteDeviceIdRequestAsync(deleteDeviceIdRequest).ConfigureAwait(false);
            if (response.Error != null)
            {
                this.logger.Error(ComponentName, $"Failed to send DeviceDeleteIdRequest to Anaheim EventHub. Error code={response.Error.Code}, Error Message={response.Error.Message}");
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Filters the allowed events.
        /// </summary>
        /// <param name="eventToken">The event token.</param>
        /// <param name="info">Additional request information.</param>
        /// <returns>A list of requests that are allowed to be enqueued with an optional delay</returns>
        private async Task<IList<DelayedDeviceDeleteRequest>> FilterAllowedEventsAsync(JToken eventToken, VortexRequestInformation info)
        {
            var allowedRequests = new List<DelayedDeviceDeleteRequest>();

            IList<VortexEvent> events = eventToken.Select(e => e.Value<JObject>().ToObject<VortexEvent>()).ToList();
            int maxVisibilityTimeout = this.appConfiguration.GetConfigValue<int>(ConfigNames.PXS.EvenlyDistributeDeviceDeleteRequestMaxTimeoutInMinutes, defaultValue: 60 * 24);
            var visibilityTimeout = TimeSpan.FromMinutes(RandomHelper.Next(0, maxVisibilityTimeout));

            foreach (VortexEvent evt in events)
            {
                // system-initiated events
                if (evt.Data != null && evt.Data.IsInitiatedByUser == 0)
                {
                    if (this.IsAllowedEvent(evt, DateTime.UtcNow, RedisDatabaseId.VortexSystemRequestDedup, this.timeoutLengthForSystemRequests))
                    {
                        allowedRequests.Add(
                            new DelayedDeviceDeleteRequest
                            {
                                DeleteRequest = new DeviceDeleteRequest
                                {
                                    Data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(evt)),
                                    RequestInformation = info
                                },
                                VisibilityDelay = visibilityTimeout
                            });
                    }
                }
                // user-initiated events and events haven't updated with IsInitiatedByUser attribute yet
                else
                {
                    if (this.IsAllowedEvent(evt, DateTime.UtcNow, RedisDatabaseId.VortexNonSystemRequestDedup, this.timeoutLengthForUserRequests))
                    {
                        allowedRequests.Add(
                            new DelayedDeviceDeleteRequest
                            {
                                DeleteRequest = new DeviceDeleteRequest
                                {
                                    Data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(evt)),
                                    RequestInformation = info
                                },
                                VisibilityDelay = visibilityTimeout
                            });
                    }
                }
            }

            IncrementRequestCounter(this.counterFactory, "Total Requests", CounterType.Rate, (ulong)events.Count);
            return allowedRequests;
        }

        /// <summary>
        ///     Determines whether the event is allowed to be queued/processed.
        /// </summary>
        /// <param name="evt">The event to check.</param>
        /// <param name="eventTimeStamp">The event time stamp in UTC.</param>
        /// <param name="redisDb">The Redis database to use for the check.</param>
        /// <param name="timeoutLength"></param>
        /// <returns>
        ///     <c>true</c> if the event is allowed to be queued; otherwise, <c>false</c>.
        /// </returns>
        private bool IsAllowedEvent(VortexEvent evt, DateTime eventTimeStamp, RedisDatabaseId redisDb, int timeoutLength)
        {
            var deviceIdString = evt.Ext?.Device?.Id ?? evt.LegacyDeviceId;
            if (!DeviceIdParser.TryParseDeviceIdAsInt64(deviceIdString, out _))
            {
                // Device ID will not work so cannot process
                IncrementRequestCounter(this.counterFactory, "Requests with Invalid Device Id", CounterType.Rate, 1);
                return false;
            }

            // Since this class is shared between two different processes, we can't set the Database id in the constructor. 
            // The workaround is to set it close to where it will be called. This is a very cheap operation.
            redisClient.SetDatabaseNumber(redisDb);

            var lastSeen = redisClient.GetDataTime(deviceIdString);
            if ((lastSeen == default) || (eventTimeStamp - lastSeen.ToUniversalTime() > TimeSpan.FromMinutes(timeoutLength)))
            {
                redisClient.SetDataTime(deviceIdString, eventTimeStamp, TimeSpan.FromMinutes(timeoutLength));
                return true;
            }
            else
            {
                IncrementRequestCounter(this.counterFactory, "Duplicated Requests", CounterType.Rate, 1);
                return false;
            }
        }

        private static void IncrementRequestCounter(ICounterFactory counterFactory, string counterName, CounterType type, ulong value)
        {
            ICounter counter = counterFactory.GetCounter(CounterCategoryNames.VortexDeviceDelete, counterName, type);
            counter.IncrementBy(value);
        }
    }
}
