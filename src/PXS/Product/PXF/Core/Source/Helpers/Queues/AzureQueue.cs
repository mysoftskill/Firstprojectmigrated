// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Queue;
    using Microsoft.Azure.Storage.RetryPolicies;

    using Newtonsoft.Json;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     implementation of IQueue using Azure storage queue
    /// </summary>
    public sealed class AzureQueue<T> :
        IQueue<T>
        where T : class
    {
        private readonly ILogger logger;

        private readonly string name;

        private readonly IAzureStorageProvider storage;

        private ICloudQueue queue;

        /// <summary>
        ///     Gets the queue name
        /// </summary>
        public string Name => this.name;

        /// <summary>
        ///     Initializes a new instance of the AzureQueue class
        /// </summary>
        /// <param name="storage">storage provider</param>
        /// <param name="logger">Geneva trace logger</param>
        /// <param name="name">queue name</param>
        public AzureQueue(
            IAzureStorageProvider storage,
            ILogger logger,
            string name)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.name = (string.IsNullOrWhiteSpace(name) ? typeof(T).Name : name).ToLowerInvariant();
        }

        /// <summary>
        ///     Dequeues a queue item
        /// </summary>
        /// <param name="leaseDuration">amount of time to hold the items</param>
        /// <param name="timeout">timeout</param>
        /// <param name="retryPolicy"></param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>resulting value</returns>
        public async Task<IQueueItem<T>> DequeueAsync(
            TimeSpan leaseDuration,
            TimeSpan timeout,
            IRetryPolicy retryPolicy,
            CancellationToken cancellationToken)
        {
            IList<IQueueItem<T>> result =
                await this.DequeueBatchAsync(leaseDuration, timeout, 1, retryPolicy, cancellationToken).ConfigureAwait(false);

            return result?.FirstOrDefault();
        }

        /// <summary>
        ///     Dequeues a batch of queue items
        /// </summary>
        /// <param name="leaseTime">amount of time to hold the items</param>
        /// <param name="timeout">timeout</param>
        /// <param name="maxCount">max count of items to dequeue</param>
        /// <param name="retryPolicy"></param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>resulting value</returns>
        public async Task<IList<IQueueItem<T>>> DequeueBatchAsync(
            TimeSpan leaseTime,
            TimeSpan timeout,
            int maxCount,
            IRetryPolicy retryPolicy,
            CancellationToken cancellationToken)
        {
            IList<CloudQueueMessage> messages = new List<CloudQueueMessage>();

            await this.EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                messages = (await this.queue.DequeueAsync(maxCount, leaseTime, timeout, retryPolicy, cancellationToken)
                    .ConfigureAwait(false))
                    .ToList();

                List<IQueueItem<T>> azureQueueItemList = new List<IQueueItem<T>>();
                foreach (CloudQueueMessage msg in messages)
                {
                    azureQueueItemList.Add(new AzureQueueItem<T>(msg, this.queue, JsonConvert.DeserializeObject<T>(msg.AsString)));
                }

                return azureQueueItemList;
            }
            catch (StorageException e)
            {
                this.logger.Error(nameof(AzureQueue<T>), $"Failed to dequeue item from queue {this.name}: {e}");
            }
            catch (JsonSerializationException e)
            {
                // TODO: dead lettering?

                // ReSharper disable once PossibleNullReferenceException
                this.logger.Error(
                    nameof(AzureQueue<T>),
                    "Failed to deserialize item from queue {0} [item(s): {1}]: {2}",
                    this.name,
                    string.Join(", ", messages.Select(c => c?.AsString)),
                    e);
            }

            return null;
        }

        /// <summary>
        ///     Enqueues a item to one of the queue
        /// </summary>
        /// <param name="item">item to enqueue</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>resulting value</returns>
        public async Task EnqueueAsync(
            T item,
            CancellationToken cancellationToken)
        {
            await this.EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
            await this.queue.EnqueueAsync(new CloudQueueMessage(JsonConvert.SerializeObject(item))).ConfigureAwait(false);
        }

        /// <summary>
        ///     Enqueues an item to a queue with a delay until it can be dequeued.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="timeToLive">Optional time to live in the queue.</param>
        /// <param name="invisibilityDelay">The invisibility delay until the item can be dequeued.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task EnqueueAsync(T item, TimeSpan? timeToLive, TimeSpan? invisibilityDelay, CancellationToken cancellationToken)
        {
            await this.EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
            await this.queue.EnqueueAsync(new CloudQueueMessage(JsonConvert.SerializeObject(item)), timeToLive, invisibilityDelay, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        ///     Gets the approximate queue size
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>the queue size</returns>
        public async Task<ulong> GetQueueSizeAsync(CancellationToken cancellationToken)
        {
            await this.EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

            int length = await this.queue.GetQueueSizeAsync().ConfigureAwait(false);

            return (ulong)Math.Max(length, 0);
        }

        /// <inheritdoc />
        public async Task<int> GetQueueAgeAsync(CancellationToken cancellationToken)
        {
            await this.EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

            var message = await this.queue.PeekMessageAsync().ConfigureAwait(false);

            var age = (message?.InsertionTime != null) ?
                DateTimeOffset.UtcNow - message.InsertionTime.Value.ToUniversalTime() :
                TimeSpan.Zero;

            return (int)age.TotalHours;
        }

        /// <summary>
        ///     Initializes the object
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>resulting value</returns>
        private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
        {
            if (this.queue == null)
            {
                ICloudQueue queueLocal;

                try
                {
                    queueLocal = await this.storage.GetCloudQueueAsync(this.name, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    this.logger.Error(this.name, $"Failed to open queue {this.name}, queue storage uri: {this.storage?.QueueStorageUri}: {e}");
                    throw;
                }

                Interlocked.CompareExchange(ref this.queue, queueLocal, null);
            }
        }
    }
}
