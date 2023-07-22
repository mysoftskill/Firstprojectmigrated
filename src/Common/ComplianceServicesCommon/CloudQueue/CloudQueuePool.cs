namespace Microsoft.ComplianceServices.Common.Queues
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;

    /// <summary>
    /// Cloud queue. Implementation based on Azure.Storage.Queues.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CloudQueuePool<T> : ICloudQueueBase<T>
    {
        private readonly ICloudQueue<T>[] cloudQueues;

        /// <summary>
        /// CloudQueuePool.
        /// </summary>
        /// <param name="cloudQueues">Cloud queues array.</param>
        public CloudQueuePool(ICloudQueue<T>[] cloudQueues)
        {
            this.cloudQueues = cloudQueues ?? throw new ArgumentNullException(nameof(cloudQueues));
        }

        /// <inheritdoc />
        public async Task CreateIfNotExistsAsync()
        {
            foreach (var cloud in this.cloudQueues)
            {
                await cloud.CreateIfNotExistsAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<ICloudQueueItem<T>> DequeueAsync(TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default)
        {
            ICloudQueueItem<T> cloudQueueItem = null;

            await this.RunAsync(
                async cloudQueue => cloudQueueItem = await cloudQueue.DequeueAsync(visibilityTimeout, cancellationToken),
                () => cloudQueueItem != null).ConfigureAwait(false);

            return cloudQueueItem;
        }

        /// <inheritdoc />
        public async Task<IList<ICloudQueueItem<T>>> DequeueBatchAsync(TimeSpan? visibilityTimeout = null, int maxCount = 32, CancellationToken cancellationToken = default)
        {
            IList<ICloudQueueItem<T>> cloudQueueItems = null;

            await this.RunAsync(
                async cloudQueue => cloudQueueItems = await cloudQueue.DequeueBatchAsync(visibilityTimeout, maxCount, cancellationToken),
                () => cloudQueueItems != null).ConfigureAwait(false);

            return cloudQueueItems;
        }

        /// <inheritdoc />
        public Task EnqueueAsync(T data, TimeSpan? timeToLive = null, TimeSpan? invisibilityDelay = null, CancellationToken cancellationToken = default)
        {
            bool succeded = false;

            return this.RunAsync(
                async cloudQueue =>
                {
                    await cloudQueue.EnqueueAsync(data, timeToLive, invisibilityDelay, cancellationToken);
                    succeded = true;
                },
                () => succeded);
        }

        /// <inheritdoc />
        public async Task<int> GetQueueSizeAsync()
        {
            int queueSize = 0;

            foreach (var queue in this.cloudQueues)
            {
                queueSize += await queue.GetQueueSizeAsync().ConfigureAwait(false);
            }

            return queueSize;
        }

        /// <summary>
        /// Get Random Cloud Queue.
        /// </summary>
        /// <returns></returns>
        private ICloudQueue<T> GetRandomCloudQueue(IEnumerable<ICloudQueue<T>> excludeCloudQueues = null)
        {
            IEnumerable<ICloudQueue<T>> localCloudQueues = this.cloudQueues;
            if (excludeCloudQueues != null)
            {
                localCloudQueues = localCloudQueues.Except(excludeCloudQueues);
            }

            if (!localCloudQueues.Any())
            { 
                return null;
            }

            int numOfQueues = localCloudQueues.Count();
            int queueIndex = RandomHelper.Next(0, numOfQueues);

            return localCloudQueues.ElementAt(queueIndex);
        }

        private async Task RunAsync(Func<ICloudQueue<T>, Task> actionAsync, Func<bool> succeded)
        {
            List<ICloudQueue<T>> excludeCloudQueues = new List<ICloudQueue<T>>();
            ICloudQueue<T> cloudQueue = this.GetRandomCloudQueue();
            List<Exception> exceptions = new List<Exception>();
            bool actionAsyncSucceded = false;

            while (cloudQueue != null)
            {
                try
                {
                    await actionAsync(cloudQueue).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }

                actionAsyncSucceded = succeded();
                if (actionAsyncSucceded)
                {
                    break;
                }

                excludeCloudQueues.Add(cloudQueue);
                cloudQueue = this.GetRandomCloudQueue(excludeCloudQueues);
            }

            if (!actionAsyncSucceded && exceptions.Any())
            {
                throw new AggregateException(exceptions);
            }
        }
    }
}
