namespace Microsoft.ComplianceServices.Common.Queues
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     Cloud queue interface.
    /// </summary>
    /// <typeparam name="T">type of queue item</typeparam>
    public interface ICloudQueueBase<T>
    {
        /// <summary>
        ///     Dequeues a queue items
        /// </summary>
        /// <param name="visibilityTimeout">amount of time to hold the items</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>resulting value</returns>
        Task<ICloudQueueItem<T>> DequeueAsync(
            TimeSpan? visibilityTimeout = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Dequeues a batch of queue items
        /// </summary>
        /// <param name="visibilityTimeout">amount of time to hold the items</param>
        /// <param name="maxCount">max count of items to dequeue</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>resulting value</returns>
        Task<IList<ICloudQueueItem<T>>> DequeueBatchAsync(
            TimeSpan? visibilityTimeout = null,
            int maxCount = 32,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Enqueues an item to a queue with a delay until it can be dequeued.
        /// </summary>
        /// <param name="data">The item.</param>
        /// <param name="timeToLive">Optional time to live in the queue.</param>
        /// <param name="invisibilityDelay">The invisibility delay until the item can be dequeued.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task EnqueueAsync(T data, TimeSpan? timeToLive = default, TimeSpan? invisibilityDelay = default, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets the approximate queue size.
        /// </summary>
        /// <returns>Approximate queue size.</returns>
        Task<int> GetQueueSizeAsync();

        /// <summary>
        /// Create if not exists.
        /// </summary>
        /// <returns></returns>
        Task CreateIfNotExistsAsync();
    }
}
