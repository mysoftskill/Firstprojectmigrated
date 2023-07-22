// ---------------------------------------------------------------------------
// <copyright file="InMemoryQueue.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Azure.Storage.RetryPolicies;

    /// <summary>
    ///     inplements an in memory verison of an IQueue
    /// </summary>
    public class InMemoryQueue<T> : IQueue<T>
    {
        private readonly ConcurrentQueue<InMemoryQueueItem<T>> queue = new ConcurrentQueue<InMemoryQueueItem<T>>();

        /// <summary>
        ///     Initializes a new instance of the InMemoryQueue class
        /// </summary>
        /// <param name="name">queue name</param>
        public InMemoryQueue(string name)
        {
            this.Name = ArgumentCheck.ReturnIfNotNullEmptyOrWhiteSpace(name, nameof(name));
        }

        /// <summary>
        ///     Gets the queue name
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Gets the approximate queue size
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>the queue size</returns>
        public Task<ulong> GetQueueSizeAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Convert.ToUInt64(this.queue.Count));
        }

        /// <summary>
        ///     Dequeues a queue items
        /// </summary>
        /// <param name="leaseTime">amount of time to hold the items</param>
        /// <param name="timeout">timeout for call to queue</param>
        /// <param name="retryPolicy">retry policy</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>resulting value</returns>
        public Task<IQueueItem<T>> DequeueAsync(
            TimeSpan leaseTime, 
            TimeSpan timeout, 
            IRetryPolicy retryPolicy,
            CancellationToken cancellationToken)
        {
            IQueueItem<T> result = this.queue.TryDequeue(out InMemoryQueueItem<T> item) ? item : null;
            return Task.FromResult(result);
        }

        /// <summary>
        ///     Dequeues a batch of queue items
        /// </summary>
        /// <param name="leaseDuration">amount of time to hold the items</param>
        /// <param name="timeout">timeout</param>
        /// <param name="maxCount">max count of items to dequeue</param>
        /// <param name="retrypolicy">retry policy</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>resulting value</returns>
        public Task<IList<IQueueItem<T>>> DequeueBatchAsync(
            TimeSpan leaseDuration,
            TimeSpan timeout, 
            int maxCount, 
            IRetryPolicy retrypolicy, 
            CancellationToken cancellationToken)
        {
            IList<IQueueItem<T>> result = new List<IQueueItem<T>>();

            if (maxCount >= 1 && this.queue.TryDequeue(out InMemoryQueueItem<T> item))
            {
                result.Add(item);
            }

            return Task.FromResult(result);
        }

        /// <summary>
        ///     Enqueues an item to a queue
        /// </summary>
        /// <param name="item">item to enqueue</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>resulting value</returns>
        public Task EnqueueAsync(
            T item, 
            CancellationToken cancellationToken)
        {
            this.queue.Enqueue(new InMemoryQueueItem<T>(this, item));
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Enqueues an item to a queue with a delay until it can be dequeued
        /// </summary>
        /// <param name="item">item to enqueue</param>
        /// <param name="timeToLive">Optional time to live in the queue.</param>
        /// <param name="invisibilityDelay">invisibility delay until the item can be dequeued</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns></returns>
        public Task EnqueueAsync(
            T item,
            TimeSpan? timeToLive,
            TimeSpan? invisibilityDelay,
            CancellationToken cancellationToken)
        {
            if (invisibilityDelay > TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(invisibilityDelay), 
                    "This queue implementation does not support delays");
            }

            if (timeToLive.HasValue && timeToLive >= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeToLive),
                    "This queue implementation does not support specific time to live");
            }

            this.queue.Enqueue(new InMemoryQueueItem<T>(this, item));

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Enqueues an item to a queue with a delay until it can be dequeued
        /// </summary>
        /// <param name="item">item to enqueue</param>
        /// <returns></returns>
        public void ReleaseToQueue(InMemoryQueueItem<T> item)
        {
            this.queue.Enqueue(item);
        }

        /// <inheritdoc />
        public Task<int> GetQueueAgeAsync(CancellationToken cancellationToken)
        {
            var age = (this.queue.TryPeek(out var message) && message.InsertionTime != null) ?
                DateTimeOffset.UtcNow - message.InsertionTime.Value.ToUniversalTime() :
                TimeSpan.Zero;

            return Task.FromResult((int)age.TotalHours);
        }
    }
}
