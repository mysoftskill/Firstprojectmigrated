namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// Represents the results of a queue processing operation. Used to figure out what to do with the message.
    /// </summary>
    public class QueueProcessResult
    {
        private QueueProcessResult()
        {
        }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        public static QueueProcessResult Success()
        {
            return new QueueProcessResult { Complete = true };
        }

        /// <summary>
        /// Creates a result that indicates the work item should be replayed after a given delay.
        /// </summary>
        public static QueueProcessResult RetryAfter(TimeSpan delay)
        {
            return new QueueProcessResult { Complete = false, Delay = delay };
        }

        /// <summary>
        /// Creates a result that indicates the work item should be replayed after a short, random delay.
        /// </summary>
        public static QueueProcessResult TransientFailureRandomBackoff()
        {
            return RetryAfter(TimeSpan.FromSeconds(RandomHelper.Next(2, 180)));
        }
        
        /// <summary>
        /// Indicates if we should mark the work item as completed.
        /// </summary>
        public bool Complete { get; private set; }

        /// <summary>
        /// Indicates the processing delay.
        /// </summary>
        public TimeSpan Delay { get; private set; }
    }
}
