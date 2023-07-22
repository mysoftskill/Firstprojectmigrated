namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Storage.Queue;

    /// <summary>
    /// A wrapper that contains some metadata as well as the raw Queue work item.
    /// </summary>
    public class QueueWorkItemWrapper<TWorkItem>
    {
        private readonly CloudQueueMessage queueMessage;
        private readonly IAzureCloudQueue cloudQueue;

        // A callback that indicates how to convert an instance of TWorkItem into a byte array.
        private readonly Func<TWorkItem, byte[]> packageCallback;

        internal QueueWorkItemWrapper(
            TWorkItem workItem, 
            IAzureCloudQueue queue, 
            CloudQueueMessage queueItem,
            Func<TWorkItem, byte[]> packageCallback)
        {
            this.WorkItem = workItem;
            this.cloudQueue = queue;
            this.queueMessage = queueItem;
            this.packageCallback = packageCallback;
        }

        /// <summary>
        /// The rough time at which the lease will expire.
        /// </summary>
        public DateTimeOffset LeaseExpirationTime => this.queueMessage.NextVisibleTime ?? DateTimeOffset.MinValue;

        /// <summary>
        /// The approximate amount of time remaining on the lease.
        /// </summary>
        public TimeSpan RemainingLeaseTime => this.queueMessage.NextVisibleTime != null ? this.queueMessage.NextVisibleTime.Value - DateTimeOffset.UtcNow : TimeSpan.Zero;

        /// <summary>
        /// The work item.
        /// </summary>
        public TWorkItem WorkItem { get; }

        /// <summary>
        /// Updates the work item.
        /// </summary>
        /// <param name="visibilityDelay">The new visibility delay.</param>
        /// <param name="updateContent">True to update the contents of the work item.</param>
        public Task UpdateAsync(TimeSpan? visibilityDelay, bool updateContent)
        {
            MessageUpdateFields updateFields = (MessageUpdateFields)0;

            if (updateContent)
            {
                updateFields |= MessageUpdateFields.Content;
                this.queueMessage.SetMessageContent2(this.packageCallback(this.WorkItem));
            }

            if (visibilityDelay != null)
            {
                updateFields |= MessageUpdateFields.Visibility;
            }
            else
            {
                visibilityDelay = TimeSpan.Zero;
            }

            if (updateFields == 0)
            {
                // Nothing to do, I guess.
                return Task.FromResult(true);
            }

            return this.cloudQueue.UpdateMessageAsync(this.queueMessage, visibilityDelay.Value, updateFields);
        }
    }
}
