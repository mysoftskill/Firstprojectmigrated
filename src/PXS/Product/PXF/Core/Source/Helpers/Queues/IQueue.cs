// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Storage.RetryPolicies;

    public interface IQueue
    {
        /// <summary>
        ///     Gets the queue name
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Gets the approximate queue size
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>the queue size</returns>
        Task<ulong> GetQueueSizeAsync(CancellationToken cancellationToken);

        /// <summary>
        ///     Gets the age of the first queue message (in hours)
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The age of the first queue message (in hours)</returns>
        Task<int> GetQueueAgeAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    ///     contract for queue items
    /// </summary>
    /// <typeparam name="T">type of queue item</typeparam>
    public interface IQueue<T> : IQueue
    {
        /// <summary>
        ///     Dequeues a queue items
        /// </summary>
        /// <param name="leaseTime">amount of time to hold the items</param>
        /// <param name="timeout">timeout for call to queue</param>
        /// <param name="retryPolicy"></param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>resulting value</returns>
        Task<IQueueItem<T>> DequeueAsync(
            TimeSpan leaseTime,
            TimeSpan timeout,
            IRetryPolicy retryPolicy,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Dequeues a batch of queue items
        /// </summary>
        /// <param name="leaseDuration">amount of time to hold the items</param>
        /// <param name="timeout">timeout</param>
        /// <param name="maxCount">max count of items to dequeue</param>
        /// <param name="retrypolicy"></param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>resulting value</returns>
        Task<IList<IQueueItem<T>>> DequeueBatchAsync(
            TimeSpan leaseDuration,
            TimeSpan timeout,
            int maxCount,
            IRetryPolicy retrypolicy,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Enqueues an item to a queue
        /// </summary>
        /// <param name="item">item to enqueue</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>resulting value</returns>
        Task EnqueueAsync(T item, CancellationToken cancellationToken);

        /// <summary>
        ///     Enqueues an item to a queue with a delay until it can be dequeued.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="timeToLive">Optional time to live in the queue.</param>
        /// <param name="invisibilityDelay">The invisibility delay until the item can be dequeued.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task EnqueueAsync(T item, TimeSpan? timeToLive, TimeSpan? invisibilityDelay, CancellationToken cancellationToken);
    }
}
