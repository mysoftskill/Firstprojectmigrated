namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using Microsoft.PrivacyServices.CommandFeed.Client;

    /// <summary>
    /// Schema of queue statistics data from storage.
    /// </summary>
    public class AgentQueueStatistics
    {
        /// <summary>
        /// The date of the query.
        /// </summary>
        public DateTime QueryDate { get; set; }
        
        /// <summary>
        /// The moniker of the database that was queries.
        /// </summary>
        public string DbMoniker { get; set; }
        
        /// <summary>
        /// The type of subject.
        /// </summary>
        public SubjectType SubjectType { get; set; }

        /// <summary>
        /// The agent ID.
        /// </summary>
        public AgentId AgentId { get; set; }
        
        /// <summary>
        /// The asset group ID.
        /// </summary>
        public AssetGroupId AssetGroupId { get; set; }

        /// <summary>
        /// The type of command.
        /// </summary>
        public PrivacyCommandType? CommandType { get; set; }

        /// <summary>
        /// The earliest time at which a lease is available.
        /// </summary>
        public DateTimeOffset? MinLeaseAvailableTime { get; set; }

        /// <summary>
        /// This is the minimum creation time for the command that is still pending
        /// </summary>
        public DateTimeOffset? MinPendingCommandCreationTime { get; set; }

        /// <summary>
        /// The number of pending commands that are not leased.
        /// </summary>
        public long? UnleasedCommandCount { get; set; }

        /// <summary>
        /// The total number of pending commands.
        /// </summary>
        public long? PendingCommandCount { get; set; }
    }
}