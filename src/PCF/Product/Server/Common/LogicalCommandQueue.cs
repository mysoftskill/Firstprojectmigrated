namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Wraps a set of related queues for a single subject type / asset group into one logical ICommandQueue.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public class LogicalCommandQueue : RoundRobinCommandQueue
    {
        private readonly List<ICommandQueue> innerQueues;
        private readonly Dictionary<string, ICommandQueue> monikerQueueMap;
        
        /// <summary>
        /// Creates a logical command queue. Accepts a list of { Queue, Weight, DatabaseMoniker } tuples. The weight determines what share of new commands
        /// are sent to that queue.
        /// </summary>
        public LogicalCommandQueue(List<(ICommandQueue commandQueue, int weight, string dbMoniker)> innerQueuesAndWeights, CommandQueuePriority queuePriority)
        {
            this.innerQueues = innerQueuesAndWeights.Select(x => x.commandQueue).ToList();
            this.monikerQueueMap = innerQueuesAndWeights.ToDictionary(x => x.dbMoniker, x => x.commandQueue);
            this.QueuePriority = queuePriority;
        }

        /// <summary>
        /// Don't check each individual subqueue more than once per 5 seconds.
        /// </summary>
        protected override TimeSpan DelayBetweenItems
        {
            get
            {
                for (int i = 10; i >= 6; --i)
                { 
                    if (FlightingUtilities.IsIntegerValueEnabled(FlightingNames.LogicalCommandQueueDelayBetweenItems, i))
                    {
                        return TimeSpan.FromSeconds(i);
                    }
                }

                return TimeSpan.FromSeconds(5);
            }
        }

        public override Task EnqueueAsync(string moniker, PrivacyCommand command)
        {
            ICommandQueue queue = this.monikerQueueMap[moniker];
            return queue.EnqueueAsync(moniker, command);
        }

        public override Task UpsertAsync(string moniker, PrivacyCommand command)
        {
            ICommandQueue queue = this.monikerQueueMap[moniker];
            return queue.UpsertAsync(moniker, command);
        }

        public override bool SupportsLeaseReceipt(LeaseReceipt leaseReceipt)
        {
            return this.innerQueues.Any(x => x.SupportsLeaseReceipt(leaseReceipt));
        }

        /// <summary>
        /// Indicates if the queue supports queue flush by date
        /// </summary>
        /// <returns>bool indicating if this is supported</returns>
        public override bool SupportsQueueFlushByDate => this.GetInnerQueues().All(c => c.SupportsQueueFlushByDate);

        public override Task<LeaseReceipt> ReplaceAsync(LeaseReceipt leaseReceipt, PrivacyCommand command, CommandReplaceOperations commandReplaceOperations)
        {
            var innerQueue = this.innerQueues.Single(x => x.SupportsLeaseReceipt(leaseReceipt));
            return innerQueue.ReplaceAsync(leaseReceipt, command, commandReplaceOperations);
        }

        public override Task DeleteAsync(LeaseReceipt leaseReceipt)
        {
            var innerQueue = this.innerQueues.Single(x => x.SupportsLeaseReceipt(leaseReceipt));
            return innerQueue.DeleteAsync(leaseReceipt);
        }

        public override Task<PrivacyCommand> QueryCommandAsync(LeaseReceipt leaseReceipt)
        {
            var innerQueue = this.innerQueues.Single(x => x.SupportsLeaseReceipt(leaseReceipt));
            return innerQueue.QueryCommandAsync(leaseReceipt);
        }

        public override Task AddQueueStatisticsAsync(ConcurrentBag<AgentQueueStatistics> resultBag, bool getDetailedStatistics, CancellationToken token)
        {
            return Task.WhenAll(this.innerQueues.Select(x => x.AddQueueStatisticsAsync(resultBag, getDetailedStatistics, token)));
        }

        /// <summary>
        /// Call the AgentQueueFlush storedProc to delete all the commands in the agent queues
        /// </summary>
        /// <param name="flushDate">The command creation date of the latest command that needs to be flushed</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Task</returns>
        public override async Task FlushAgentQueueAsync(DateTimeOffset flushDate, CancellationToken token)
        {
            await Task.WhenAll(this.innerQueues.Select(x => x.FlushAgentQueueAsync(flushDate, token)));
        }

        protected override IList<ICommandQueue> GetInnerQueues()
        {
            return this.innerQueues;
        }
    }
}
