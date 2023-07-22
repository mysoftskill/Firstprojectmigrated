// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.QueueProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     VortexDeviceDeleteQueueProcessor
    /// </summary>
    public class VortexDeviceDeleteQueueProcessor : BackgroundWorker
    {
        private const string componentName = nameof(VortexDeviceDeleteQueueProcessor);

        private readonly IVortexDeviceDeleteQueueProccessorConfiguration config;

        private readonly ICounterFactory counterFactory;

        private readonly ILogger logger;

        private readonly TimeSpan MaxTimeoutGetMessages = TimeSpan.FromSeconds(10);

        private readonly IVortexDeviceDeleteQueueManager queueManager;

        private readonly IVortexEventService vortexEventService;

        private readonly IAppConfiguration appConfiguration;

        private readonly TimeSpan WaitOnQueueEmpty;

        /// <summary>
        ///     Creates a new instance of <see cref="VortexDeviceDeleteQueueProcessor" />
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="configuration">The queue processor configuration</param>
        /// <param name="queueManager">The queue</param>
        /// <param name="vortexService">The vortex service</param>
        /// <param name="counterFactory">The counter factory for making perf counters</param>
        /// <param name="appConfiguration">The Azure App Configuration instance</param>
        public VortexDeviceDeleteQueueProcessor(
            ILogger logger,
            IVortexDeviceDeleteQueueProccessorConfiguration configuration,
            IVortexDeviceDeleteQueueManager queueManager,
            IVortexEventService vortexService,
            ICounterFactory counterFactory,
            IAppConfiguration appConfiguration)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.queueManager = queueManager ?? throw new ArgumentNullException(nameof(queueManager));
            this.vortexEventService = vortexService ?? throw new ArgumentNullException(nameof(vortexService));
            this.config = configuration;
            this.WaitOnQueueEmpty = TimeSpan.FromMilliseconds(this.config.WaitOnQueueEmptyMilliseconds);
            this.counterFactory = counterFactory ?? throw new ArgumentNullException(nameof(counterFactory));
            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
        }

        /// <summary>
        ///     Grabs work from the queue and processes them async
        /// </summary>
        /// <returns>
        ///     <c>true</c> if there were events to process, otherwise <c>false</c>
        /// </returns>
        public override async Task<bool> DoWorkAsync()
        {
            try
            {
                if (!appConfiguration.GetConfigValue<bool>(ConfigNames.PXS.VortexDeviceDeleteWorker_EnableDequeuing, defaultValue: true))
                {
                    this.logger.Warning(componentName, $"DoWorkAsync. {ConfigNames.PXS.VortexDeviceDeleteWorker_EnableDequeuing} is disabled.");
                    return false;
                }

                IList<IQueueItem<DeviceDeleteRequest>> deviceDeleteRequests = new List<IQueueItem<DeviceDeleteRequest>>();
                using (var cancellationTokenSource = new CancellationTokenSource(this.MaxTimeoutGetMessages))
                {
                    CancellationToken cancellationToken = cancellationTokenSource.Token;
                    cancellationToken.ThrowIfCancellationRequested();

                    int queueMessageDequeueCount = appConfiguration.GetConfigValue<int>(ConfigNames.PXS.VortexDeviceDeleteWorker_DequeueCount, defaultValue: 1);
                    deviceDeleteRequests = await this.queueManager.GetMessagesAsync(queueMessageDequeueCount, cancellationToken).ConfigureAwait(false);
                }

                if (deviceDeleteRequests == null || deviceDeleteRequests.Count == 0)
                {
                    // Did not receive work from the queue
                    this.logger.Information(componentName, $"Zero messages found in queue: {nameof(DeviceDeleteRequest)}");
                    return false;
                }

                this.logger.Information(componentName,
                    $"Successfully retrieved {deviceDeleteRequests.Count} messages from queue: {nameof(DeviceDeleteRequest)}");

                var taskList = new List<Task>();

                // Feature flag PXS.DeleteDeviceRequestEnabled is configured with percentage filter used for throttling.
                // Allows about X% of traffic goes through and send the rest back to queue if we set its value to be X.
                // Each time returns true/false according to value comparsion between a random value and value X.
                if (await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PXS.DeleteDeviceRequestEnabled, useCached: false).ConfigureAwait(false))
                {
                    var deviceDeleteRequestsNeedsSendToPCF = deviceDeleteRequests.Where(d => d.Data.IsSentToPCF == false).ToList();
                    var deviceDeleteRequestsSentToPCF = deviceDeleteRequests.Where(d => d.Data.IsSentToPCF == true).ToList();

                    // Send to PCF
                    IEnumerable<ServiceResponse<IQueueItem<DeviceDeleteRequest>>> responses =
                        await this.vortexEventService.DeleteDevicesAsync(deviceDeleteRequestsNeedsSendToPCF).ConfigureAwait(false);

                    foreach (ServiceResponse<IQueueItem<DeviceDeleteRequest>> serviceResponse in responses)
                    {
                        IQueueItem<DeviceDeleteRequest> deviceDeleteRequestItem = serviceResponse.Result;
                        this.UpdateQueueItemAgePerformanceCounter(deviceDeleteRequestItem);
                        if (serviceResponse.IsSuccess)
                        {
                            deviceDeleteRequestItem.Data.IsSentToPCF = true;
                            // Feature flag PXS.AnaheimIdEventsPublishEnabled is configured with percentage filter used for throttling.
                            // The percentage value will be set to 0% util we start rollout with Anaheim team
                            // TODO: Will consider refactor sending anaheim traffic part into an reusable method
                            if (await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdEventsPublishEnabled, useCached: false).ConfigureAwait(false))
                            {
                                // Send to Anaheim Successfully
                                if (await this.vortexEventService.SendAnaheimDeviceDeleteIdRequestAsync(deviceDeleteRequestItem.Data).ConfigureAwait(false))
                                {
                                    taskList.Add(this.DeleteMessageAsync(deviceDeleteRequestItem, "Successfully deleted message from queue."));
                                }
                                else
                                {
                                    // retry with updated field IsSentToPCF
                                    taskList.Add(this.UpdateMessageAsync(deviceDeleteRequestItem, GetLeaseTime(deviceDeleteRequestItem.DequeueCount), "Failed to send to Anaheim"));
                                }
                            }
                            else
                            {
                                this.logger.Warning(componentName, $"Outgoing traffic (sending to Anaheim) got throttled. Extending lease of throttled requests.");
                                int maxVisibilityTimeout = appConfiguration.GetConfigValue<int>(ConfigNames.PXS.DeviceDeleteMaxVisibilityTimeoutInMinutes, defaultValue: 60 * 24);
                                var visibilityTimeout = TimeSpan.FromMinutes(RandomHelper.Next(0, maxVisibilityTimeout));
                                taskList.Add(this.RenewMessageLeaseAsync(deviceDeleteRequestItem, visibilityTimeout, "Request sending to Anaheim got throttled"));
                            }
                        }
                        else
                        {
                            this.logger.Error(componentName, $"Service response is not success. Error message is: {serviceResponse.Error?.Message}");
                            // retry
                            taskList.Add(this.RenewMessageLeaseAsync(deviceDeleteRequestItem, GetLeaseTime(deviceDeleteRequestItem.DequeueCount), "Failed to send to PCF"));
                        }
                    }

                    // Send to Anaheim for requests already sent to PCF but failed to send to Anaheim in last run
                    foreach (var deviceDeleteRequestSentToPCF in deviceDeleteRequestsSentToPCF)
                    {
                        // Feature flag PXS.AnaheimIdEventsPublishEnabled is configured with percentage filter used for throttling.
                        // The percentage value will be set to 0% util we start rollout with Anaheim team
                        // TODO: Will consider refactor sending anaheim traffic part into an reusable method
                        if (await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdEventsPublishEnabled, useCached: false).ConfigureAwait(false))
                        {
                            // Send to Anaheim Successfully
                            if (await this.vortexEventService.SendAnaheimDeviceDeleteIdRequestAsync(deviceDeleteRequestSentToPCF.Data).ConfigureAwait(false))
                            {
                                taskList.Add(this.DeleteMessageAsync(deviceDeleteRequestSentToPCF, "Successfully deleted message from queue."));
                            }
                            else
                            {
                                // retry
                                taskList.Add(this.RenewMessageLeaseAsync(deviceDeleteRequestSentToPCF, GetLeaseTime(deviceDeleteRequestSentToPCF.DequeueCount), "Failed to send to Anaheim"));
                            }
                        }
                        else
                        {
                            this.logger.Warning(componentName, $"Outgoing traffic (sending to Anaheim) got throttled. Extending lease of throttled requests.");
                            int maxVisibilityTimeout = appConfiguration.GetConfigValue<int>(ConfigNames.PXS.DeviceDeleteMaxVisibilityTimeoutInMinutes, defaultValue: 60 * 24);
                            var visibilityTimeout = TimeSpan.FromMinutes(RandomHelper.Next(0, maxVisibilityTimeout));
                            taskList.Add(this.RenewMessageLeaseAsync(deviceDeleteRequestSentToPCF, visibilityTimeout, "Request sending to Anaheim got throttled"));
                        }
                    }

                    // 06/18/2021: due to the high rate of delete requests adding this delay 
                    // to slow down device delete traffic in NGP pipelines.
                    // Previously the normal rate was around 20 requests/min,
                    // PXS has ProcessorCount = 2 for each worker VM, times 10 VMs we deployed, so the rate is about one message per minute per processor.
                    // The dealy time is configurable via Azure App configuration under below key
                    //
                    int delayPerMessageInMilliSeconds = appConfiguration.GetConfigValue<int>(ConfigNames.PXS.VortexDeviceDeleteWorker_DelayPerMessageInMilliSeconds, defaultValue: 1000);
                    if (delayPerMessageInMilliSeconds > 0)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(delayPerMessageInMilliSeconds)).ConfigureAwait(false);
                    }
                }
                else
                {
                    this.logger.Warning(componentName, $"Outgoing traffic (sending to PCF) got throttled. Extending lease of throttled requests.");
                    // messages will be available for pick-up after visibilityTimeout time
                    int maxVisibilityTimeout = appConfiguration.GetConfigValue<int>(ConfigNames.PXS.DeviceDeleteMaxVisibilityTimeoutInMinutes, defaultValue: 60 * 24);
                    var visibilityTimeout = TimeSpan.FromMinutes(RandomHelper.Next(0, maxVisibilityTimeout));
                    taskList.AddRange(deviceDeleteRequests.Select(r => this.RenewMessageLeaseAsync(r, visibilityTimeout, "Request got throttled")));
                }

                await Task.WhenAll(taskList).ConfigureAwait(false);

                return true;
            }
            catch (OperationCanceledException e)
            {
                this.logger.Error(componentName, e, $"{nameof(this.DoWorkAsync)} was canceled.");
                return false;
            }

            catch (Exception e)
            {
                this.logger.Error(componentName, e, "An unhandled exception occurred.");
                return false;
            }
        }

        /// <summary>
        ///     Starts this instance.
        /// </summary>
        public override void Start()
        {
            base.Start(this.WaitOnQueueEmpty);
        }

        private async Task DeleteMessageAsync(IQueueItem<DeviceDeleteRequest> queueItem, string logMessagePrefix)
        {
            this.logger.Information(componentName, $"{logMessagePrefix}. RequestId: {queueItem.Data.RequestId}, IsSentToPCF={queueItem.Data.IsSentToPCF}");
            IncrementCounter(this.counterFactory, "Successful Requests", 1);
            await queueItem.CompleteAsync().ConfigureAwait(false);
        }

        private async Task RenewMessageLeaseAsync(IQueueItem<DeviceDeleteRequest> queueItem, TimeSpan lease, string logMessagePrefix)
        {
            this.logger.Information(componentName, $"{logMessagePrefix}. RequestId: {queueItem.Data.RequestId}. DequeueCount: {queueItem.DequeueCount}, IsSentToPCF={queueItem.Data.IsSentToPCF}");
            IncrementCounter(this.counterFactory, "Failed Requests", 1);
            await queueItem.RenewLeaseAsync(lease).ConfigureAwait(false);
        }

        private async Task UpdateMessageAsync(IQueueItem<DeviceDeleteRequest> queueItem, TimeSpan lease, string logMessagePrefix)
        {
            this.logger.Information(componentName, $"{logMessagePrefix}. RequestId: {queueItem.Data.RequestId}. DequeueCount: {queueItem.DequeueCount}, IsSentToPCF={queueItem.Data.IsSentToPCF}");
            await queueItem.UpdateAsync(lease).ConfigureAwait(false);
        }

        /// <summary>
        /// Exponentially increase the lease time for the item based on how many times it's been dequeued up to a max of a day
        /// </summary>
        /// <returns></returns>
        private TimeSpan GetLeaseTime(int DequeueCount)
        {
            TimeSpan leaseTime = TimeSpan.FromSeconds(Math.Pow(2, Math.Min(DequeueCount, 16)));
            leaseTime = leaseTime > TimeSpan.FromDays(1.0) ? TimeSpan.FromDays(1.0) : leaseTime;
            return leaseTime;
        }

        /// <summary>
        ///     Updates the queue item age performance counter.
        ///     TODO: CounterFactory Not working. Can be refactored and removed
        /// </summary>
        /// <param name="queueItem">The queue item.</param>
        private void UpdateQueueItemAgePerformanceCounter(IQueueItem<DeviceDeleteRequest> queueItem)
        {
            if (queueItem?.InsertionTime != null)
            {
                int itemHoursInQueue = (DateTimeOffset.UtcNow - (DateTimeOffset)queueItem.InsertionTime).Hours;
                ICounter counter = this.counterFactory.GetCounter(CounterCategoryNames.AzureQueue, "VortexDeviceDeleteQueueItemAge", CounterType.Number);
                counter.SetValue((ulong)itemHoursInQueue);
            }
        }

        // TODO: CounterFactory Not working. Can be refactored and removed
        private static void IncrementCounter(ICounterFactory counterFactory, string counterName, ulong value)
        {
            var counter = counterFactory.GetCounter(CounterCategoryNames.VortexDeviceDelete, counterName, CounterType.Rate);
            counter.IncrementBy(value);
        }

    }
}
