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

    /// <summary>
    ///     contract for partitioned queues
    /// </summary>
    /// <typeparam name="TData">type of queue data object</typeparam>
    /// <typeparam name="TPartitionId">partitionId identifier type</typeparam>
    public interface IPartitionedQueue<TData, TPartitionId>
    {
        /// <summary>
        ///     Gets the set of partitions
        /// </summary>
        IReadOnlyList<QueuePartition<TPartitionId>> Partitions { get; }

        /// <summary>
        ///     Gets the partitioned queue's name
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Gets the total size of all partitions
        /// </summary>
        /// <param name="cancelToken">cancellation token</param>
        /// <returns>the queue size</returns>
        Task<ulong> GetSizeAsync(CancellationToken cancelToken);

        /// <summary>
        ///     Gets the approximate sizes of the individual partitionIds
        /// </summary>
        /// <param name="cancelToken">cancellation token</param>
        /// <returns>the queue size</returns>
        Task<IReadOnlyList<QueuePartitionSize<TPartitionId>>> GetPartitionSizesAsync(CancellationToken cancelToken);

        /// <summary>
        ///     Dequeues a queue items
        /// </summary>
        /// <param name="partitionIds">ordered list of ids of partitions that Dequeue is allowed to dequeue from</param>
        /// <param name="leaseTime">amount of time to hold the items</param>
        /// <param name="timeout">timeout for call to queue</param>
        /// <param name="retrypolicy">retrypolicy</param>
        /// <param name="cancelToken">cancellation token</param>
        /// <returns>resulting value</returns>
        Task<PartitionedQueueItem<TData, TPartitionId>> DequeueAsync(
            IReadOnlyList<TPartitionId> partitionIds,
            TimeSpan leaseTime,
            TimeSpan timeout,
            IRetryPolicy retrypolicy,
            CancellationToken cancelToken);

        /// <summary>
        ///     Dequeues a batch of queue items
        /// </summary>
        /// <param name="partitionIds">ordered list of ids of partitions that Dequeue is allowed to dequeue from</param>
        /// <param name="leaseTime">amount of time to hold the items</param>
        /// <param name="timeout">timeout for call to queue</param>
        /// <param name="maxCount">max count of items to dequeue</param>
        /// <param name="retryPolicy">retry policy</param>
        /// <param name="cancelToken">cancellation token</param>
        /// <returns>resulting value</returns>
        /// <remarks>
        ///     the method will attempt to dequeue from each of the partitionIds listed in the partitionIds parameter until a non-
        ///      empty result is found.
        ///     note that any non-empty result will result in immediate return even if maxCount items were not dequeued from
        ///      the dequeue.
        /// </remarks>
        Task<IList<PartitionedQueueItem<TData, TPartitionId>>> DequeueBatchAsync(
            IReadOnlyList<TPartitionId> partitionIds,
            TimeSpan leaseTime,
            TimeSpan timeout,
            int maxCount,
            IRetryPolicy retryPolicy,
            CancellationToken cancelToken);

        /// <summary>
        ///     Enqueues an item to a queue
        /// </summary>
        /// <param name="partitionId">id of partition to enqueue to</param>
        /// <param name="item">item to enqueue</param>
        /// <param name="cancelToken">cancellation token</param>
        /// <returns>resulting value</returns>
        Task EnqueueAsync(
            TPartitionId partitionId,
            TData item,
            CancellationToken cancelToken);
    }
}
