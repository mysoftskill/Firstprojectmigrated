namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;

    /// <summary>
    /// An implementation of ICommandQueue that round-robins between underlying queues in the PopAsync call.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public abstract class RoundRobinCommandQueue : ICommandQueue
    {
        private readonly object syncRoot = new object();

        // Maintain a list of linked lists we use to 
        private LinkedList<QueueItem> allHighPriorityCommandQueues;

        private LinkedList<QueueItem> allLowPriorityCommandQueues;

        private LinkedList<QueueItem> allCommandQueues;

        /// <summary>
        /// The minimum time to wait before querying the same inner queue again.
        /// </summary>
        protected abstract TimeSpan DelayBetweenItems { get; }

        /// <summary>
        /// Indicates if the queue supports queue flush by date
        /// </summary>
        /// <returns>bool indicating if this is supported</returns>
        public abstract bool SupportsQueueFlushByDate { get; }

        public CommandQueuePriority QueuePriority { get; protected set; } = CommandQueuePriority.Default;

        /// <inheritdoc />
        public Task<CommandQueuePopResult> PopAsync(int maxToPop, TimeSpan? requestedLeaseDuration, CommandQueuePriority queuePriority = CommandQueuePriority.Default)
        {
            switch (queuePriority)
            {
                case CommandQueuePriority.High:
                    this.InitializeHighPriorityQueues();
                    return this.PopAsync(maxToPop, requestedLeaseDuration, this.allHighPriorityCommandQueues);

                case CommandQueuePriority.Low:
                    this.InitializeLowPriorityQueues();
                    return this.PopAsync(maxToPop, requestedLeaseDuration, this.allLowPriorityCommandQueues);

                case CommandQueuePriority.Default:
                default:
                    this.InitializeDefaultPriorityQueues();
                    return this.PopAsync(maxToPop, requestedLeaseDuration, this.allCommandQueues);
            }
        }

        private void InitializeHighPriorityQueues()
        {
            // Would prefer to do this in the constructor, however 'this.GetInnerQueues' is virtual and cannot be called in a ctor.
            lock (this.syncRoot)
            {
                if (this.allHighPriorityCommandQueues == null)
                {
                    IList<ICommandQueue> innerQueues = this.GetInnerQueues();
                    this.allHighPriorityCommandQueues = new LinkedList<QueueItem>();
                    foreach (var innerQueue in innerQueues.Where(c => c != null && c.QueuePriority == CommandQueuePriority.High))
                    {
                        this.allHighPriorityCommandQueues.AddLast(new QueueItem { CommandQueue = innerQueue, NextTimeToPop = DateTimeOffset.UtcNow });
                    }

                    RotateRandom(this.allHighPriorityCommandQueues);
                }
            }
        }

        private void InitializeLowPriorityQueues()
        {
            // Would prefer to do this in the constructor, however 'this.GetInnerQueues' is virtual and cannot be called in a ctor.
            lock (this.syncRoot)
            {
                if (this.allLowPriorityCommandQueues == null)
                {
                    IList<ICommandQueue> innerQueues = this.GetInnerQueues();
                    this.allLowPriorityCommandQueues = new LinkedList<QueueItem>();
                    foreach (var innerQueue in innerQueues.Where(c => c != null && c.QueuePriority == CommandQueuePriority.Low))
                    {
                        this.allLowPriorityCommandQueues.AddLast(
                            new QueueItem
                            {
                                CommandQueue = innerQueue,
                                NextTimeToPop = DateTimeOffset.UtcNow
                            });
                    }

                    RotateRandom(this.allLowPriorityCommandQueues);
                }
            }
        }

        private void InitializeDefaultPriorityQueues()
        {
            lock (this.syncRoot)
            {
                if (this.allCommandQueues == null)
                {
                    IList<ICommandQueue> innerQueues = this.GetInnerQueues();
                    this.allCommandQueues = new LinkedList<QueueItem>();
                    foreach (var innerQueue in innerQueues.Where(c => c != null))
                    {
                        this.allCommandQueues.AddLast(
                            new QueueItem
                            {
                                CommandQueue = innerQueue,
                                NextTimeToPop = DateTimeOffset.UtcNow
                            });
                    }

                    RotateRandom(this.allCommandQueues);
                }
            }
        }

        private async Task<CommandQueuePopResult> PopAsync(int maxToPop, TimeSpan? requestedLeaseDuration, LinkedList<QueueItem> commandQueues)
        {
            // Remove the head node. This is the next node to query.
            LinkedListNode<QueueItem> headNode = null;
            lock (this.syncRoot)
            {
                if (commandQueues.Count > 0)
                {
                    headNode = commandQueues.First;
                    commandQueues.Remove(headNode);
                }
            }

            if (headNode == null)
            {
                // If we didn't have a head node, then wait for a second.
                // There are cases where there might literally be no queues to check.
                await Task.Delay(TimeSpan.FromSeconds(1));
                return new CommandQueuePopResult(null, null);
            }

            try
            {
                // Compute the amount of time to delay.
                TimeSpan timeToSleep = headNode.Value.NextTimeToPop - DateTimeOffset.UtcNow;
                if (timeToSleep > TimeSpan.Zero)
                {
                    PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "DataAgentCommandQueueDelay").Increment();
                    PerfCounterUtility.GetOrCreate(PerformanceCounterType.Number, "DataAgentCommandQueueDelayMs").Set((int)timeToSleep.TotalMilliseconds);

                    await Task.Delay(timeToSleep);
                }

                var value = await PopInnerQueue(headNode.Value.CommandQueue, maxToPop, requestedLeaseDuration);
                return value;
            }
            finally
            {
                headNode.Value.NextTimeToPop = DateTimeOffset.UtcNow + this.DelayBetweenItems;
                lock (this.syncRoot)
                {
                    commandQueues.AddLast(headNode);
                }
            }
        }

        private static async Task<CommandQueuePopResult> PopInnerQueue(ICommandQueue queue, int maxToPop, TimeSpan? requestedLeaseDuration)
        {
            try
            {
                return await queue.PopAsync(maxToPop, requestedLeaseDuration);
            }
            catch (Exception ex)
            {
                // Don't really care. Just return a new list in this case.
                Logger.Instance?.UnexpectedException(ex);
                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "PopInnerUnexpectedExceptions").Increment();

                // Return an error result.
                return new CommandQueuePopResult(null, new[] { ex });
            }
        }

        /// <inheritdoc />
        public abstract Task EnqueueAsync(string moniker, PrivacyCommand command);

        /// <inheritdoc />
        public abstract Task UpsertAsync(string moniker, PrivacyCommand command);

        /// <inheritdoc />
        public abstract bool SupportsLeaseReceipt(LeaseReceipt leaseReceipt);

        /// <inheritdoc />
        public abstract Task<LeaseReceipt> ReplaceAsync(LeaseReceipt leaseReceipt, PrivacyCommand command, CommandReplaceOperations commandReplaceOperations);

        /// <inheritdoc />
        public abstract Task DeleteAsync(LeaseReceipt leaseReceipt);

        /// <inheritdoc />
        public abstract Task<PrivacyCommand> QueryCommandAsync(LeaseReceipt leaseReceipt);

        /// <inheritdoc />
        public abstract Task AddQueueStatisticsAsync(ConcurrentBag<AgentQueueStatistics> resultBag, bool getDetailedStatistics, CancellationToken token);

        /// <inheritdoc />
        public abstract Task FlushAgentQueueAsync(DateTimeOffset flushDate, CancellationToken token);

        /// <summary>
        /// Get a list of inner queues. Only called at initialization time.
        /// </summary>
        protected abstract IList<ICommandQueue> GetInnerQueues();

        /// <summary>
        /// Randomly rotate the linked list so that we start from a different queue each time.
        /// </summary>
        private static void RotateRandom<T>(LinkedList<T> list)
        {
            if (list.Count == 0)
            {
                return;
            }

            int rotationAmount = RandomHelper.Next(0, list.Count);
            for (int i = 0; i < rotationAmount; ++i)
            {
                var first = list.First;
                list.RemoveFirst();
                list.AddLast(first);
            }
        }
        
        private class QueueItem
        {
            public ICommandQueue CommandQueue { get; set; }

            public DateTimeOffset NextTimeToPop { get; set; }
        }
    }
}
