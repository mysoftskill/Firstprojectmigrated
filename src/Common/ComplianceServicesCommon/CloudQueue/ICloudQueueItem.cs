// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ComplianceServices.Common.Queues
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    ///     contract for queue items
    /// </summary>
    public interface ICloudQueueItem<out T>
    {
        /// <summary>
        ///     Gets the queue item data
        /// </summary>
        T Data { get; }

        /// <summary>
        ///     Gets the number of times this item has been dequeued
        /// </summary>
        long DequeueCount { get; }

        /// <summary>
        ///     Gets the time that the message expires
        /// </summary>
        DateTimeOffset? ExpirationTime { get; }

        /// <summary>
        ///     Gets the message id
        /// </summary>
        string MessageId { get; }

        /// <summary>
        ///     Gets the time that the message was added to the queue
        /// </summary>
        DateTimeOffset? InsertionTime { get; }

        /// <summary>
        ///     Gets the time that the message will next be visible
        /// </summary>
        DateTimeOffset? NextVisibleTime { get; }

        /// <summary>
        ///     Gets the pop receipt of the message
        /// </summary>
        string PopReceipt { get; }

        /// <summary>
        ///     Delete from the queue.
        /// </summary>
        Task DeleteAsync();

        /// <summary>
        ///     Updates the message content and renews the lease for the work item
        /// </summary>
        /// <param name="visibilityTimeout">Visibility timeout.</param>
        /// <returns></returns>
        Task UpdateAsync(TimeSpan visibilityTimeout);
    }
}
