// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    ///     contract for queue items
    /// </summary>
    public interface IQueueItem<out T>
    {
        /// <summary>
        ///     Gets the queue item data
        /// </summary>
        T Data { get; }

        /// <summary>
        ///     Gets the number of times this item has been dequeued
        /// </summary>
        int DequeueCount { get; }

        /// <summary>
        ///     Gets the time that the message expires
        /// </summary>
        DateTimeOffset? ExpirationTime { get; }

        /// <summary>
        ///     Gets the message id
        /// </summary>
        string Id { get; }

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
        ///     Completes the work item
        /// </summary>
        /// <returns>resulting value</returns>
        Task CompleteAsync();

        /// <summary>
        ///     Releases the work item for reprocessing
        /// </summary>
        /// <returns>resulting value</returns>
        /// <remarks>Not all queues support releasing an item early</remarks>
        Task ReleaseAsync();

        /// <summary>
        ///     Renews the lease for the work item so it is not handed back to
        /// </summary>
        /// <returns>true if the lease was renewed and false if it could not be renewed</returns>
        Task<bool> RenewLeaseAsync(TimeSpan duration);

        /// <summary>
        ///     Updates the message content and renews the lease for the work item
        /// </summary>
        /// <returns>true if the update succeeded and false if it could not be updated</returns>
        Task<bool> UpdateAsync(TimeSpan leaseDuration);
    }
}
