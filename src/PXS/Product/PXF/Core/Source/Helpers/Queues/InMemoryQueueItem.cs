// ---------------------------------------------------------------------------
// <copyright file="InMemoryQueueItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;

    /// <summary>
    ///     inplements an in memory verison of an IQueueItem
    /// </summary>
    public class InMemoryQueueItem<T> : IQueueItem<T>
    {
        private static readonly Task<bool> TrueTask = Task.FromResult(true);

        private readonly InMemoryQueue<T> queueOwner;

        /// <summary>
        ///     Initializes a new instance of the InMemoryQueueItem class
        /// </summary>
        /// <param name="data">data object</param>
        /// <param name="queueOwner">queue owner</param>
        public InMemoryQueueItem(
            InMemoryQueue<T> queueOwner,
            T data)
        {
            this.queueOwner = queueOwner ?? throw new ArgumentNullException(nameof(queueOwner));
            this.Data = data;
        }

        /// <summary>
        ///     Gets the queue item data
        /// </summary>
        public T Data { get; }

        /// <summary>
        ///     Gets the number of times this item has been dequeued
        /// </summary>
        public int DequeueCount { get; private set; } = 1;

        /// <summary>
        ///     Gets the time that the message expires
        /// </summary>
        public DateTimeOffset? ExpirationTime => null;

        /// <inheritdoc />
        public string Id => string.Empty;

        /// <summary>
        ///     Gets the time that the message was added to the queue
        /// </summary>
        public DateTimeOffset? InsertionTime => null;

        /// <summary>
        ///     Gets the time that the message will next be visible
        /// </summary>
        public DateTimeOffset? NextVisibleTime => null;

        /// <inheritdoc />
        public string PopReceipt => string.Empty;

        /// <summary>
        ///     Completes the work item
        /// </summary>
        /// <returns>resulting value</returns>
        public Task CompleteAsync() => Task.CompletedTask;

        /// <summary>
        ///     Releases the work item for reprocessing
        /// </summary>
        /// <returns>resulting value</returns>
        /// <remarks>Not all queues support releasing an item early</remarks>
        public Task ReleaseAsync()
        {
            // if it is being enqueued again, it is expected to be dequeued again
            this.DequeueCount += 1;

            this.queueOwner.ReleaseToQueue(this);
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Renews the lease for the work item so it is not handed back to
        /// </summary>
        /// <returns>true if the lease was renewed and false if it could not be renewed</returns>
        public Task<bool> RenewLeaseAsync(TimeSpan duration)
        {
            return InMemoryQueueItem<T>.TrueTask;
        }

        /// <inheritdoc />
        public Task<bool> UpdateAsync(TimeSpan leaseDuration)
        {
            return InMemoryQueueItem<T>.TrueTask;
        }
    }
}
