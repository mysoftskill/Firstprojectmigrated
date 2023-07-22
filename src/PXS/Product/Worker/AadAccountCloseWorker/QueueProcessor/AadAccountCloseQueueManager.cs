// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.QueueProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Storage.RetryPolicies;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    /// <summary>
    ///     AccountCloseQueueManager manages access to all account-close-queues
    ///     Access is done using a round-robin strategy
    /// </summary>
    public class AadAccountCloseQueueManager : IAccountCloseQueueManager
    {
        private readonly TimeSpan DefaultLeaseTime = TimeSpan.FromMinutes(15);

        private readonly TimeSpan DefaultQueueTimeout = TimeSpan.FromMinutes(2);

        private readonly TimeSpan queueSizeLengthCounterRefresh;

        private readonly IRetryPolicy RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(2), 3);

        private readonly IQueueSelectionStrategy<AccountCloseRequest> roundRobinQueue;

        /// <summary>
        ///     Creates a new instance of Account Close Queue Writer
        /// </summary>
        /// <param name="queueStorageProviders">The account-close queues</param>
        /// <param name="config"></param>
        /// <param name="logger">The logger</param>
        /// <param name="counterFactory"></param>
        public AadAccountCloseQueueManager(
            IList<IAzureStorageProvider> queueStorageProviders,
            ILogger logger,
            IAadAccountCloseQueueProccessorConfiguration config,
            ICounterFactory counterFactory)
        {
            if (queueStorageProviders == null) throw new ArgumentNullException(nameof(queueStorageProviders));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (counterFactory == null) throw new ArgumentNullException(nameof(counterFactory));

            IServicePointConfiguration servicePointConfig = config.ServicePointConfiguration;

            this.queueSizeLengthCounterRefresh = TimeSpan.FromSeconds(config.QueueSizeLengthCounterRefreshSeconds);

            IList<IQueue<AccountCloseRequest>> queues = new List<IQueue<AccountCloseRequest>>();
            foreach (IAzureStorageProvider azureStorageProvider in queueStorageProviders)
            {
                // Configure Service Point for the Queue (both primary and secondary).
                this.ConfigureQueueServicePoint(servicePointConfig, azureStorageProvider.QueueStorageUri.PrimaryUri);
                this.ConfigureQueueServicePoint(servicePointConfig, azureStorageProvider.QueueStorageUri.SecondaryUri);
                var queue = new AzureQueue<AccountCloseRequest>(azureStorageProvider, logger, nameof(AccountCloseRequest).ToLowerInvariant());
                queues.Add(queue);
                this.StartQueueLengthMonitorBackgroundTaskAsync(logger, counterFactory, queue, azureStorageProvider);
            }

            if (queues.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(queueStorageProviders), queueStorageProviders, "Must initialize with at least 1 queue. There were 0.");
            }

            this.roundRobinQueue = new AadAccountCloseRoundRobinQueueStrategy(queues);
        }

        /// <inheritdoc />
        public async Task EnqueueAsync(IEnumerable<AccountCloseRequest> accountCloseRequests, CancellationToken cancellationToken)
        {
            await EnqueueInternalAsync(accountCloseRequests, TimeSpan.Zero, cancellationToken);
        }


        /// <inheritdoc />
        public async Task EnqueueAsync(IEnumerable<AccountCloseRequest> accountCloseRequests, TimeSpan invisibilityDelay, CancellationToken cancellationToken)
        {
            await EnqueueInternalAsync(accountCloseRequests, invisibilityDelay, cancellationToken);
        }

       /// <inheritdoc />
        public async Task<IList<IQueueItem<AccountCloseRequest>>> GetMessagesAsync(int maxCount, CancellationToken cancellationToken)
        {
            IList<IQueueItem<AccountCloseRequest>> messages = new List<IQueueItem<AccountCloseRequest>>();

            // Creates a copy of the available queues so that when selecting new queues the state can be maintained local to this method.
            IQueueSelectionStrategy<AccountCloseRequest> queueSelector = new AadAccountCloseRoundRobinQueueStrategy(this.roundRobinQueue.GetAllQueues());

            // Try to get a queue until messages are found in a queue, or we run out of queues to get messages from.
            while (queueSelector.TryGetNextQueueAndRemove(out IQueue<AccountCloseRequest> queue) && (messages == null || messages.Count == 0))
            {
                cancellationToken.ThrowIfCancellationRequested();

                messages = await queue.DequeueBatchAsync(this.DefaultLeaseTime, this.DefaultQueueTimeout, maxCount, this.RetryPolicy, cancellationToken).ConfigureAwait(false);
            }

            return messages;
        }
       
        private async Task EnqueueInternalAsync(IEnumerable<AccountCloseRequest> accountCloseRequests, TimeSpan invisibilityDelay, CancellationToken cancellationToken)
        {
            var enqueueTasks = new List<Task>();
            foreach (AccountCloseRequest request in accountCloseRequests)
            {
                // set TTL to max so they never expire
                enqueueTasks.Add(this.roundRobinQueue.GetRandomQueue().EnqueueAsync(request, TimeSpan.MaxValue, invisibilityDelay, cancellationToken));

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

        private void ConfigureQueueServicePoint(IServicePointConfiguration config, Uri queueStorageUri)
        {
            IServicePointConfiguration queueServicePointConfiguration = config;

            ServicePoint servicePoint = ServicePointManager.FindServicePoint(queueStorageUri);
            servicePoint.UseNagleAlgorithm = queueServicePointConfiguration.UseNagleAlgorithm;
            servicePoint.ConnectionLimit = queueServicePointConfiguration.ConnectionLimit;
            servicePoint.MaxIdleTime = queueServicePointConfiguration.MaxIdleTime;
            servicePoint.ConnectionLeaseTimeout = queueServicePointConfiguration.ConnectionLeaseTimeout;
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
                        logger.Information(nameof(AadAccountCloseQueueManager), $"Queue {accountAndQueueName} size: {size}, age: {age}");

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
                        await Task.Delay(this.queueSizeLengthCounterRefresh).ConfigureAwait(false);
                    } while (true);
                });
        }
    }
}
