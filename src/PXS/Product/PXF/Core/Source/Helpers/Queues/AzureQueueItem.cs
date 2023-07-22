// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Azure.Storage.Queue;

    /// <summary>
    ///     implements the AzureQueueItem interface for an Azure queue
    /// </summary>
    /// <typeparam name="TItem">type of the queue item</typeparam>
    public class AzureQueueItem<TItem> :
        IQueueItem<TItem>
        where TItem : class
    {
        private readonly ICloudQueue cloudQueue;

        private TItem data;

        private CloudQueueMessage msg;

        /// <summary>
        ///     Gets the queue item data
        /// </summary>
        public TItem Data => this.data;

        /// <inheritdoc />
        public int DequeueCount => this.msg?.DequeueCount ?? 0;

        /// <inheritdoc />
        public DateTimeOffset? ExpirationTime => this.msg?.ExpirationTime;

        /// <inheritdoc />
        public DateTimeOffset? InsertionTime => this.msg?.InsertionTime;

        /// <inheritdoc />
        public DateTimeOffset? NextVisibleTime => this.msg?.NextVisibleTime;

        public string Id => this.msg?.Id;

        public string PopReceipt => this.msg?.PopReceipt;

        /// <summary>
        ///     Initializes a new instance of the AzureQueueItem class
        /// </summary>
        /// <param name="msg">queue message object</param>
        /// <param name="cloudQueue">cloud queue</param>
        /// <param name="data">data in queue message object</param>
        public AzureQueueItem(
            CloudQueueMessage msg,
            ICloudQueue cloudQueue,
            TItem data)
        {
            this.cloudQueue = cloudQueue;
            this.data = data;
            this.msg = msg;
        }

        public async Task<bool> UpdateAsync(TimeSpan leaseDuration)
        {
            if (this.msg == null)
            {
                throw new InvalidOperationException("Lock was released or completed");
            }

            // since the function throws, use await even though we don't really need it
            return await this.cloudQueue.UpdateAsync(this.msg, leaseDuration).ConfigureAwait(false);
        }

        /// <summary>
        ///     Completes the work item
        /// </summary>
        /// <returns>resulting value</returns>
        public async Task CompleteAsync()
        {
            if (this.msg == null)
            {
                throw new InvalidOperationException("Lock was released or completed");
            }

            await this.cloudQueue.CompleteItemAsync(this.msg).ConfigureAwait(false);

            this.data = null;
            this.msg = null;
        }

        /// <summary>
        ///     Releases the work item
        /// </summary>
        /// <returns>resulting value</returns>
        public async Task ReleaseAsync()
        {
            if (this.msg == null)
            {
                throw new InvalidOperationException("Lock was released or completed");
            }

            // release just makes the queue item available much sooner than the original lease time
            await this.cloudQueue.SetLeaseTimeoutAsync(this.msg, TimeSpan.FromSeconds(5)).ConfigureAwait(false);

            this.data = null;
            this.msg = null;
        }

        /// <summary>
        ///     Renews the lease for the work item so it is not handed back to
        /// </summary>
        /// <returns>true if the lease was renewed and false if it could not be renewed</returns>
        public async Task<bool> RenewLeaseAsync(TimeSpan leaseDuration)
        {
            if (this.msg == null)
            {
                throw new InvalidOperationException("Lock was released or completed");
            }

            // since the function throws, use await even though we don't really need it
            // ReSharper disable once AsyncConverter.AsyncAwaitMayBeElidedHighlighting
            return await this.cloudQueue.SetLeaseTimeoutAsync(this.msg, leaseDuration).ConfigureAwait(false);
        }
    }
}
