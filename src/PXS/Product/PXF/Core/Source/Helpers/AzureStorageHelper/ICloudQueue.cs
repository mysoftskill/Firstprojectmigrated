// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Storage.Queue;
    using Microsoft.Azure.Storage.RetryPolicies;

    /// <summary>
    ///     contract for Azure based queues
    /// </summary>
    public interface ICloudQueue
    {
        /// <summary>
        ///     Gets the queue name
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Completes the queue item
        /// </summary>
        /// <returns>true if the item was completed, or false if it could not be because the caller no longer owned it</returns>
        Task<bool> CompleteItemAsync(CloudQueueMessage message);

        /// <summary>
        ///     Completes the item.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="receipt">The receipt.</param>
        /// <returns><c>true</c> if success, otherwise <c>false</c></returns>
        Task<bool> CompleteItemAsync(string id, string receipt);

        /// <summary>
        ///     Dequeues one or more message from the queue
        /// </summary>
        /// <param name="maxItems">maximum items</param>
        /// <param name="leaseDuration">lease duration</param>
        /// <param name="timeout">dequeue timeout</param>
        /// <param name="retrypolicy">The retry policy</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>list of dequeued items</returns>
        Task<IEnumerable<CloudQueueMessage>> DequeueAsync(
            int maxItems,
            TimeSpan leaseDuration,
            TimeSpan timeout,
            IRetryPolicy retrypolicy,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Enqueues a message to the queue
        /// </summary>
        /// <param name="message">message to insert</param>
        /// <returns>resulting value</returns>
        Task EnqueueAsync(CloudQueueMessage message);

        /// <summary>
        ///     Enqueues a message to the queue with a visibility delay. The message cannot be dequeued until it becomes visible.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="timeToLive">Optional time to live in the queue.</param>
        /// <param name="initialVisibilityDelay">The initial visibility delay until the message can be dequeued.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task EnqueueAsync(CloudQueueMessage message, TimeSpan? timeToLive, TimeSpan? initialVisibilityDelay, CancellationToken cancellationToken);

        /// <summary>
        ///     Gets the approximate queue size
        /// </summary>
        /// <returns>resulting value</returns>
        Task<int> GetQueueSizeAsync();

        /// <summary>
        ///     Extends the least of a queue item
        /// </summary>
        /// <returns>true if the lease was extended or false if it could not be because the caller no longer owned it</returns>
        Task<bool> SetLeaseTimeoutAsync(
            CloudQueueMessage message,
            TimeSpan leaseDuration);

        /// <summary>
        ///     Updates the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="leaseDuration">The requested lease duration</param>
        /// <returns>true if the message and lease was extended or false if it could not be because the caller no longer owned it</returns>
        Task<bool> UpdateAsync(
            CloudQueueMessage message,
            TimeSpan leaseDuration);

        /// <summary>
        ///     Peek the first message in the queue.
        /// </summary>
        /// <returns>The message in the queue</returns>
        Task<CloudQueueMessage> PeekMessageAsync();
    }
}
