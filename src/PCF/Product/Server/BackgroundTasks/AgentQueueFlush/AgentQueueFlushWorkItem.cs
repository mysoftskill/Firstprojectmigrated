namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks
{
    using System;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    public class AgentQueueFlushWorkItem
    {
        /// <summary>
        /// AgentId for flushing the queues
        /// </summary>
        public AgentId AgentId { get; set; }

        /// <summary>
        /// All commands need to be flushed that are before the FlushDate
        /// </summary>
        public DateTimeOffset FlushDate { get; set; }
    }
}
