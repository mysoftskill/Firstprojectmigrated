namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;

    /// <summary>
    /// The record capturing the response from the stored proc
    /// after a partial flush
    /// </summary>
    public class AgentQueueFlushResult
    {
        /// <summary>
        /// Total Items Deleted
        /// </summary>
        public int ItemsDeleted { get; set; }

        /// <summary>
        /// Number of items returned for the query
        /// </summary>
        public int TotalItems { get; set; }
    }
}