// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Azure.Storage.RetryPolicies;

    /// <summary>
    ///     partitioned queue
    /// </summary>
    /// <typeparam name="TData">type of queue data object</typeparam>
    /// <typeparam name="TPartitionId">type of id used to partitionId data</typeparam>
    /// <remarks>
    ///     when first created, the PartitionedQueue is in initialization mode which allows adding new queues to the collection
    ///      but does not provide access to enqueue or dequeue items. 
    ///     when all queues are added, SetQueueMode() should be called to allow enqueue and dequeue of items, but permanantly
    ///      prevents additional queues from being added.
    /// </remarks>
    public class PartitionedQueue<TData, TPartitionId> : IPartitionedQueue<TData, TPartitionId>
    {
        private readonly IDictionary<TPartitionId, IQueue<TData>> queues;

        private bool initMode = true;

        /// <summary>
        ///     Initializes a new instance of the PartitionedQueue class
        /// </summary>
        /// <param name="name">partitioned queue name</param>
        /// <param name="comparer">TPartition comparer</param>
        public PartitionedQueue(
            string name,
            IEqualityComparer<TPartitionId> comparer)
        {
            this.Name = ArgumentCheck.ReturnIfNotNullEmptyOrWhiteSpace(name, nameof(name));

            this.queues = new Dictionary<TPartitionId, IQueue<TData>>(comparer);
        }

        /// <summary>
        ///     Initializes a new instance of the PartitionedQueue class
        /// </summary>
        /// <param name="name">partitioned queue name</param>
        public PartitionedQueue(string name) :
            this(name, null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the PartitionedQueue class
        /// </summary>
        public PartitionedQueue() :
            this(typeof(TData).Name.ToLowerInvariant() + "-partitioned", null)
        {
        }

        /// <summary>
        ///     Gets the set of partitions
        /// </summary>
        public IReadOnlyList<QueuePartition<TPartitionId>> Partitions =>
            new ReadOnlyCollection<QueuePartition<TPartitionId>>(
                this.queues.Select(o => new QueuePartition<TPartitionId>(o.Key, o.Value.Name)).ToList());

        /// <summary>
        ///     Gets the partitioned queue's name
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///      Adds a partitionId to the partitioned queue
        /// </summary>
        /// <param name="partitionId">partitionId</param>
        /// <param name="queue">queue</param>
        public void AddPartition(
            TPartitionId partitionId,
            IQueue<TData> queue)
        {
            if (this.initMode == false)
            {
                throw new InvalidOperationException("Partition queue " + this.Name + " is not in initialize mode");
            }

            ArgumentCheck.ThrowIfNull(queue, nameof(queue));

            if (this.queues.ContainsKey(partitionId))
            {
                throw new ArgumentException(
                    "Partition queue already contains a queue with a partitionId of " + partitionId.ToString());
            }

            this.queues.Add(partitionId, queue);
        }

        /// <summary>
        ///     Places the partitionId queue into queue mode
        /// </summary>
        public void SetQueueMode()
        {
            if (this.initMode == false)
            {
                throw new InvalidOperationException("Partition queue " + this.Name + " is not in initialize mode");
            }

            this.initMode = false;
        }

        /// <summary>
        ///     Gets the total size of all partitions
        /// </summary>
        /// <param name="cancelToken">cancellation token</param>
        /// <returns>the queue size</returns>
        public async Task<ulong> GetSizeAsync(CancellationToken cancelToken)
        {
            if (this.initMode)
            {
                throw new InvalidOperationException("Partition queue " + this.Name + " is not in queue mode");
            }

            return (await this.GetQueueSizesAsync(cancelToken))
                .Select(o => o.Count)
                .Aggregate<ulong, ulong>(0, (current, v) => current + v);
        }

        /// <summary>
        ///     Gets the approximate sizes of the individual partitionIds
        /// </summary>
        /// <param name="cancelToken">cancellation token</param>
        /// <returns>list of partitions plus queue sizes</returns>
        public async Task<IReadOnlyList<QueuePartitionSize<TPartitionId>>> GetPartitionSizesAsync(CancellationToken cancelToken)
        {
            if (this.initMode)
            {
                throw new InvalidOperationException("Partition queue " + this.Name + " is not in queue mode");
            }

            return new ReadOnlyCollection<QueuePartitionSize<TPartitionId>>(
                (await this.GetQueueSizesAsync(cancelToken))
                    .Select(o => new QueuePartitionSize<TPartitionId>(o.Id, o.Name, o.Count))
                    .ToList());
        }

        /// <summary>
        ///     Dequeues a queue items
        /// </summary>
        /// <param name="partitionIds">ordered list of ids of partitions that Dequeue is allowed to dequeue from</param>
        /// <param name="leaseTime">amount of time to hold the items</param>
        /// <param name="timeout">timeout for call to queue</param>
        /// <param name="retryPolicy">retry policy</param>
        /// <param name="cancelToken">cancellation token</param>
        /// <returns>resulting value</returns>
        public async Task<PartitionedQueueItem<TData, TPartitionId>> DequeueAsync(
            IReadOnlyList<TPartitionId> partitionIds, 
            TimeSpan leaseTime,
            TimeSpan timeout, 
            IRetryPolicy retryPolicy, 
            CancellationToken cancelToken)
        {
            if (this.initMode)
            {
                throw new InvalidOperationException("Partition queue " + this.Name + " is not in queue mode");
            }

            for (int i = 0; i < partitionIds.Count; ++i)
            {
                IQueueItem<TData> result;
                TPartitionId id = partitionIds[i];
                IQueue<TData> queue;

                if (this.queues.TryGetValue(id, out queue) == false)
                {
                    throw new ArgumentException(
                        $"The specified partitionId id {id} is not valid for the {this.Name} partitioned queue");
                }

                result = await queue.DequeueAsync(leaseTime, timeout, retryPolicy, cancelToken).ConfigureAwait(false);
                if (result != null)
                {
                    return new PartitionedQueueItem<TData, TPartitionId>(result, id);
                }
            }

            return null;
        }

        /// <summary>
        ///     Dequeues a batch of queue items
        /// </summary>
        /// <param name="partitionIds">ordered list of ids of partitions that Dequeue is allowed to dequeue from</param>
        /// <param name="leaseTime">amount of time to hold the items</param>
        /// <param name="timeout">timeout for call to queue</param>
        /// <param name="maxCount">max count of items to dequeue</param>
        /// <param name="retryPolicy">retrypolicy</param>
        /// <param name="cancelToken">cancellation token</param>
        /// <returns>resulting value</returns>
        /// <remarks>
        ///     the method will attempt to dequeue from each of the partitions whose id is listed in the partitionId parameter 
        ///      until a non-empty result is found.
        ///     note that any non-empty result will result in immediate return even if maxCount items were not dequeued from
        ///      the dequeue.
        /// </remarks>
        public async Task<IList<PartitionedQueueItem<TData, TPartitionId>>> DequeueBatchAsync(
            IReadOnlyList<TPartitionId> partitionIds, 
            TimeSpan leaseTime, 
            TimeSpan timeout, 
            int maxCount, 
            IRetryPolicy retryPolicy, 
            CancellationToken cancelToken)
        {
            if (this.initMode)
            {
                throw new InvalidOperationException("Partition queue " + this.Name + " is not in queue mode");
            }

            for (int i = 0; i < partitionIds.Count; ++i)
            {
                IList<IQueueItem<TData>> result;
                TPartitionId id = partitionIds[i];
                IQueue<TData> queue;

                if (this.queues.TryGetValue(id, out queue) == false)
                {
                    throw new ArgumentException(
                        $"The specified partitionId id {id} is not valid for the {this.Name} partitioned queue");
                }

                result = await queue
                    .DequeueBatchAsync(leaseTime, timeout, maxCount, retryPolicy, cancelToken)
                    .ConfigureAwait(false);
                if (result?.Count > 0)
                {
                    return result.Select(o => new PartitionedQueueItem<TData, TPartitionId>(o, id)).ToList();
                }
            }

            return null;
        }

        /// <summary>
        ///     Enqueues an item to a queue
        /// </summary>
        /// <param name="partitionId">id of partition to enqueue to</param>
        /// <param name="item">item to enqueue</param>
        /// <param name="cancelToken">cancellation token</param>
        /// <returns>resulting value</returns>
        public async Task EnqueueAsync(
            TPartitionId partitionId, 
            TData item, 
            CancellationToken cancelToken)
        {
            IQueue<TData> queue;

            if (this.initMode)
            {
                throw new InvalidOperationException("Partition queue " + this.Name + " is not in queue mode");
            }

            if (this.queues.TryGetValue(partitionId, out queue) == false)
            {
                throw new ArgumentException(
                    $"The specified partitionId id {partitionId} is not valid for the {this.Name} partitioned queue");
            }

            await queue.EnqueueAsync(item, cancelToken);
        }

        /// <summary>
        ///      Gets the size of each of the partition queues
        /// </summary>
        /// <param name="cancelToken">cancel token</param>
        /// <returns>resulting value</returns>
        private async Task<IEnumerable<(TPartitionId Id, string Name, ulong Count)>> GetQueueSizesAsync(
            CancellationToken cancelToken)
        {
            async Task<(TPartitionId Partition, string Name, ulong Count)> GetQueueCountAsync(
                TPartitionId partition,
                IQueue queue)
            {
                return (partition, queue.Name, await queue.GetQueueSizeAsync(cancelToken));
            }

            IList<Task<(TPartitionId Part, string Name, ulong Count)>> countTasks;

            if (this.initMode)
            {
                throw new InvalidOperationException("Partition queue " + this.Name + " is not in queue mode");
            }

            countTasks = this.queues.Select(o => GetQueueCountAsync(o.Key, o.Value)).ToList();
            await Task.WhenAll(countTasks).ConfigureAwait(false);

            return countTasks.Select(o => o.Result);
        }
    }
}
