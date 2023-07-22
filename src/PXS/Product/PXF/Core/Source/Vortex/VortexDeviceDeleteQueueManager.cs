// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Vortex
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Azure.Storage.RetryPolicies;

    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.CommonSchema.Services.Logging;

    /// <summary>
    ///     VortexDeviceDeleteQueueManager manages access to all account-close-queues
    ///     Access is done using a round-robin strategy
    /// </summary>
    public class VortexDeviceDeleteQueueManager : IVortexDeviceDeleteQueueManager
    {
        private readonly TimeSpan DefaultLeaseTime = TimeSpan.FromMinutes(15);

        private readonly TimeSpan DefaultQueueTimeout = TimeSpan.FromMinutes(2);

        private readonly IQueueSelectionStrategy<DeviceDeleteRequest> queueSelectionStrategy;

        private readonly IQueueSelectionStrategyFactory<DeviceDeleteRequest> queueSelectionStrategyFactory;

        private readonly TimeSpan QueueSizeLengthCounterRefresh = TimeSpan.FromSeconds(30);

        private readonly IRetryPolicy RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(2), 3);

        /// <summary>
        ///     Creates a new instance of VortexDeviceDelete Queue Writer
        /// </summary>
        /// <param name="queueStorageProviders">The VortexDeviceDelete queues</param>
        /// <param name="config"></param>
        /// <param name="logger">The logger</param>
        /// <param name="queueSelectionStrategyFactory">Generator for queue selector</param>
        /// <param name="counterFactory"></param>
        public VortexDeviceDeleteQueueManager(
            IList<IAzureStorageProvider> queueStorageProviders,
            ILogger logger,
            IVortexDeviceDeleteQueueProccessorConfiguration config,
            IQueueSelectionStrategyFactory<DeviceDeleteRequest> queueSelectionStrategyFactory,
            ICounterFactory counterFactory)
        {
            if (queueStorageProviders == null)
            {
                throw new ArgumentNullException(nameof(queueStorageProviders));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (counterFactory == null)
            {
                throw new ArgumentNullException(nameof(counterFactory));
            }

            IServicePointConfiguration servicePointConfig = config.ServicePointConfiguration;

            this.queueSelectionStrategyFactory = queueSelectionStrategyFactory ?? throw new ArgumentNullException(nameof(queueSelectionStrategyFactory));

            IList<IQueue<DeviceDeleteRequest>> queues = new List<IQueue<DeviceDeleteRequest>>();
            foreach (IAzureStorageProvider azureStorageProvider in queueStorageProviders)
            {
                // Configure Service Point for the Queue (both primary and secondary).
                this.ConfigureQueueServicePoint(servicePointConfig, azureStorageProvider.QueueStorageUri.PrimaryUri);
                this.ConfigureQueueServicePoint(servicePointConfig, azureStorageProvider.QueueStorageUri.SecondaryUri);
                var queue = new AzureQueue<DeviceDeleteRequest>(azureStorageProvider, logger, nameof(DeviceDeleteRequest).ToLowerInvariant());
                queues.Add(queue);
                this.StartQueueLengthMonitorBackgroundTaskAsync(logger, counterFactory, queue, azureStorageProvider);
            }

            if (queues.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(queueStorageProviders), queueStorageProviders, "Must initialize with at least 1 queue. There were 0.");
            }

            this.queueSelectionStrategy = this.queueSelectionStrategyFactory.CreateQueueSelectionStrategy(queues);
        }

        /// <inheritdoc />
        public async Task EnqueueAsync(IEnumerable<DeviceDeleteRequest> deviceDeleteRequests, CancellationToken cancellationToken)
        {
            await this.EnqueueAsync(deviceDeleteRequests, null, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task EnqueueAsync(IEnumerable<DeviceDeleteRequest> deviceDeleteRequests, TimeSpan? invisibilityDelay, CancellationToken cancellationToken)
        {
            var enqueueTasks = new List<Task>();
            foreach (DeviceDeleteRequest request in deviceDeleteRequests)
            {
                enqueueTasks.Add(this.EnqueueHelperAsync(request, invisibilityDelay, cancellationToken));

                // Since this method doesn't prevent the size of the collection from being large, await for tasks in sub-groups instead of doing it all at once
                if (enqueueTasks.Count % 10 == 0)
                {
                    await Task.WhenAll(enqueueTasks).ConfigureAwait(false);
                    enqueueTasks.Clear();
                }
            }

            if (enqueueTasks.Count > 0)
            {
                await Task.WhenAll(enqueueTasks).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task EnqueueAsync(DeviceDeleteRequest deviceDeleteRequest, CancellationToken cancellationToken)
        {
            await this.EnqueueAsync(deviceDeleteRequest, null, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task EnqueueAsync(DeviceDeleteRequest deviceDeleteRequest, TimeSpan? invisibilityDelay, CancellationToken cancellationToken)
        {
            await this.EnqueueAsync(new[] { deviceDeleteRequest }, invisibilityDelay, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IList<IQueueItem<DeviceDeleteRequest>>> GetMessagesAsync(int maxCount, CancellationToken cancellationToken)
        {
            IList<IQueueItem<DeviceDeleteRequest>> messages = new List<IQueueItem<DeviceDeleteRequest>>();

            // Creates a copy of the available queues so that when selecting new queues the state can be maintained local to this method.
            IQueueSelectionStrategy<DeviceDeleteRequest>
                queueSelector = this.queueSelectionStrategyFactory.CreateQueueSelectionStrategy(this.queueSelectionStrategy.GetAllQueues());

            // Try to get a queue until messages are found in a queue, or we run out of queues to get messages from.
            while (queueSelector.TryGetNextQueueAndRemove(out IQueue<DeviceDeleteRequest> queue) && (messages == null || messages.Count == 0))
            {
                cancellationToken.ThrowIfCancellationRequested();

                messages = await queue.DequeueBatchAsync(this.DefaultLeaseTime, this.DefaultQueueTimeout, maxCount, this.RetryPolicy, cancellationToken).ConfigureAwait(false);
            }

            return messages ?? new List<IQueueItem<DeviceDeleteRequest>>();
        }

        private void ConfigureQueueServicePoint(IServicePointConfiguration config, Uri queueStorageUri)
        {
            IServicePointConfiguration queueServicePointConfiguration = config;

            ServicePoint servicePoint = ServicePointManager.FindServicePoint(queueStorageUri);
            servicePoint.UseNagleAlgorithm = queueServicePointConfiguration.UseNagleAlgorithm;
            servicePoint.ConnectionLimit = queueServicePointConfiguration.ConnectionLimit;
            servicePoint.MaxIdleTime = queueServicePointConfiguration.MaxIdleTime;
            servicePoint.ConnectionLeaseTimeout = queueServicePointConfiguration.ConnectionLeaseTimeout;
        }

        /// <summary>
        ///     Tries to enqueue the request until it succeeds
        /// </summary>
        /// <param name="deviceDeleteRequest">Request to enqueue</param>
        /// <param name="invisibilityDelay">Delay before the request is visible in the queue</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task EnqueueHelperAsync(DeviceDeleteRequest deviceDeleteRequest, TimeSpan? invisibilityDelay, CancellationToken cancellationToken)
        {
            IQueueSelectionStrategy<DeviceDeleteRequest>
                queueSelector = this.queueSelectionStrategyFactory.CreateQueueSelectionStrategy(this.queueSelectionStrategy.GetAllQueues());
            var innerException = new Exception($"{nameof(VortexDeviceDeleteQueueManager)}: Failed to enqueue request");
            while (queueSelector.TryGetNextQueueAndRemove(out IQueue<DeviceDeleteRequest> queue))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await queue.EnqueueAsync(deviceDeleteRequest, Timeout.InfiniteTimeSpan, invisibilityDelay, cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (Exception e)
                {
                    innerException = e;
                }
            }

            throw innerException;
        }

        private Task StartQueueLengthMonitorBackgroundTaskAsync<T>(ILogger logger, ICounterFactory counterFactory, AzureQueue<T> queue, IAzureStorageProvider azureStorageProvider)
            where T : class
        {
            return Task.Run(
                async () =>
                {
                    do
                    {
                        ulong size = await queue.GetQueueSizeAsync(CancellationToken.None).ConfigureAwait(false);
                        int age = (size > 0) ? await queue.GetQueueAgeAsync(CancellationToken.None).ConfigureAwait(false) : 0;

                        string accountAndQueueName = $"{azureStorageProvider.AccountName}.{queue.Name}";
                        logger.Information(nameof(VortexDeviceDeleteQueueManager), $"Queue {accountAndQueueName} size: {size}, age: {age}");

                        var queueDepthAgeEvent = new QueueDepthAndAgeEvent
                        {
                            AccountName = azureStorageProvider.AccountName,
                            QueueName = queue.Name,
                            QueueDepth = size,
                            AgeInHours = age
                        };
                        queueDepthAgeEvent.LogInformational(LogOption.Realtime);

                        ICounter counter = counterFactory.GetCounter(CounterCategoryNames.AzureQueue, "Queue Length", CounterType.Number);
                        counter.SetValue(size, $"{accountAndQueueName}");
                        await Task.Delay(this.QueueSizeLengthCounterRefresh).ConfigureAwait(false);
                    } while (true);
                });
        }
    }
}
