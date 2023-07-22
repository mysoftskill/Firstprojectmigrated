namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using Microsoft.Azure.Storage.Queue;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A basic interface to allow mocking Azure queues.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public interface IAzureCloudQueue
    {
        /// <summary>
        /// The name of the storage account.
        /// </summary>
        string AccountName { get; }

        /// <summary>
        /// The name of the queue.
        /// </summary>
        string QueueName { get; }

        /// <summary>
        /// Ensure queues exists.
        /// </summary>
        bool QueueExists { get; }

        /// <summary>
        /// Adds a message with the given visiblity delay.
        /// </summary>
        Task AddMessageAsync(CloudQueueMessage message, TimeSpan? visibilityDelay, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Updates the indicated fields.
        /// </summary>
        Task UpdateMessageAsync(CloudQueueMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields);

        /// <summary>
        /// Removes a message from the queue.
        /// </summary>
        Task DeleteMessageAsync(CloudQueueMessage message);

        /// <summary>
        /// Fetches messages from the queue.
        /// </summary>
        Task<IEnumerable<CloudQueueMessage>> GetMessagesAsync(int batchSize, TimeSpan? visibilityTimeout = null);

        /// <summary>
        /// Ensures that the underlying queue exists.
        /// </summary>
        Task EnsureQueueExistsAsync();

        /// <summary>
        /// Gets an approximate count of the items in the queue.
        /// </summary>
        /// <param name="token">The cancellation token</param>
        Task<int> GetCountAsync(CancellationToken token);

        /// <summary>
        /// Clear all messages in the queue
        /// </summary>
        /// <param name="token">The cancellation token</param>
        Task ClearAsync(CancellationToken token);

        /// <summary>
        /// Initiates an asynchronous operation to check the existence of the queue.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task" /> object of type <c>bool</c> that represents the asynchronous operation.</returns>
        Task<bool> ExistsAsync(CancellationToken cancellationToken);
    }
}
