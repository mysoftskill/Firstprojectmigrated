namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A queue that stores privacy commands.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public interface ICommandQueue
    {
        /// <summary>
        /// Pops at most "maxToPop" items off of the queue.
        /// </summary>
        Task<CommandQueuePopResult> PopAsync(int maxToPop, TimeSpan? requestedLeaseDuration, CommandQueuePriority queuePriority = CommandQueuePriority.Default);

        /// <summary>
        /// Enqueues the given command into the shard represented by the given moniker.
        /// </summary>
        Task EnqueueAsync(string moniker, PrivacyCommand command);

        /// <summary>
        /// Upsert the given command into the shard represented by the given moniker.
        /// </summary>
        Task UpsertAsync(string moniker, PrivacyCommand command);

        /// <summary>
        /// Replaces the given command according to the specified <see cref="CommandReplaceOperations"/>.
        /// </summary>
        Task<LeaseReceipt> ReplaceAsync(LeaseReceipt leaseReceipt, PrivacyCommand command, CommandReplaceOperations commandReplaceOperations);

        /// <summary>
        /// Deletes the command identified by the given lease receipt.
        /// </summary>
        Task DeleteAsync(LeaseReceipt leaseReceipt);

        /// <summary>
        /// Queries for the given command, identified by its lease receipt.
        /// </summary>
        Task<PrivacyCommand> QueryCommandAsync(LeaseReceipt leaseReceipt);

        /// <summary>
        /// Populates the given bag with queue statistics events.
        /// </summary>
        /// <remarks>
        /// Normally, this API would return a list, but given the degree of fanout involved with reading queues,
        /// it seems more naturally from a performance perspective to just hand a collection and let all of the
        /// fanout items add to it, rather than trying to aggregate a set of lists together.
        /// </remarks>
        Task AddQueueStatisticsAsync(ConcurrentBag<AgentQueueStatistics> resultBag, bool getDetailedStatistics, CancellationToken token);

        /// <summary>
        /// Call the AgentQueueFlush storedProc to delete all the commands in the agent queues
        /// </summary>
        /// <param name="flushDate">The command creation date of the latest command that needs to be flushed</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Task</returns>
        Task FlushAgentQueueAsync(DateTimeOffset flushDate, CancellationToken token);

        /// <summary>
        /// Indicates if this queue supports the given lease receipt.
        /// </summary>
        bool SupportsLeaseReceipt(LeaseReceipt leaseReceipt);

        /// <summary>
        /// Indicates if the queue supports queue flush by date
        /// </summary>
        /// <returns></returns>
        bool SupportsQueueFlushByDate { get; }

        /// <summary>
        /// The command queue priority
        /// </summary>
        CommandQueuePriority QueuePriority { get; }
    }

    [Flags]
    public enum CommandReplaceOperations
    {
        None = 0x0,

        LeaseExtension = 0x1,

        CommandContent = 0x2,
    }

    public enum CommandQueuePriority
    {
        Default,

        High,

        Low
    }
}
