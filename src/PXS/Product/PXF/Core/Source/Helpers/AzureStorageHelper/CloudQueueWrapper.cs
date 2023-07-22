// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Queue;
    using Microsoft.Azure.Storage.RetryPolicies;

    /// <summary>
    ///     implements the ICloudQueue contact to expose a Cloud queue 
    /// </summary>
    /// <remarks>These are intended to be very thin wrappers around CloudQueue methods</remarks>
    public class CloudQueueWrapper : ICloudQueue
    {
        private const string AzureStorageQueue = "AzureStorageQueue.";

        private const int MaxDequeueItems = 32;

        private readonly string completeOp = "DeleteMessage.";

        private readonly string completeByIdOp = "DeleteMessageById.";

        private readonly string updateOp = "UpdateMessage.";

        private readonly string enqOp = "AddMessage.";

        private readonly string deqOp = "GetMessage.";

        private readonly CloudQueue queue;

        /// <summary>
        ///     Initializes a new instance of the CloudQueueWrapper class
        /// </summary>
        /// <param name="queue">Cloud queue to wrap</param>
        public CloudQueueWrapper(CloudQueue queue)
        {
            ArgumentCheck.ThrowIfNull(queue, nameof(queue));

            this.queue = queue;

            this.completeOp += queue.Name;
            this.updateOp += queue.Name;
            this.enqOp += queue.Name;
            this.deqOp += queue.Name;
        }

        /// <summary>
        ///     Gets the queue name
        /// </summary>
        public string Name => this.queue.Name;

        /// <summary>
        ///     Gets the approximate queue size
        /// </summary>
        /// <returns>resulting value</returns>
        public async Task<int> GetQueueSizeAsync()
        {
            await this.queue.FetchAttributesAsync().ConfigureAwait(false);
            return this.queue.ApproximateMessageCount ?? 0;
        }

        /// <summary>
        ///     Enqueues a message to the queue
        /// </summary>
        /// <param name="message">message to insert</param>
        /// <returns>resulting value</returns>
        public async Task EnqueueAsync(CloudQueueMessage message)
        {
            OutgoingApiEventWrapper apiEvent = this.GetApiEvent(this.enqOp);

            apiEvent.Start();

            try
            {
                await this.queue.AddMessageAsync(message).ConfigureAwait(false);
                apiEvent.Success = true;
            }
            catch (Exception e)
            {
                apiEvent.ErrorMessage = e.ToString();
                apiEvent.ProtocolStatusCode = (e as StorageException)?.RequestInformation.HttpStatusCode.ToStringInvariant();
                throw;
            }
            finally
            {
                apiEvent.Finish();
            }
        }

        /// <summary>
        ///     Enqueues a message to the queue with a visibility delay. The message cannot be dequeued until it becomes visible.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="timeToLive">Optional time to live in the queue.</param>
        /// <param name="initialVisibilityDelay">The initial visibility delay until the message can be dequeued.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A task</returns>
        public async Task EnqueueAsync(CloudQueueMessage message, TimeSpan? timeToLive, TimeSpan? initialVisibilityDelay, CancellationToken cancellationToken)
        {
            OutgoingApiEventWrapper apiEvent = this.GetApiEvent(this.enqOp);
            apiEvent.Start();

            try
            {
                // Azure API is very strange here in my opinion. It requires infinite to be -1 seconds. However framework values like
                // Timeout.InfiniteTimeSpan are set to -1 millisecond. So, to normalize this, anything less than zero is modified to -1 second.
                timeToLive = timeToLive.HasValue && timeToLive.Value < TimeSpan.Zero ? TimeSpan.FromSeconds(-1) : timeToLive;

                await this.queue.AddMessageAsync(message, timeToLive, initialVisibilityDelay, null, null, cancellationToken).ConfigureAwait(false);
                apiEvent.Success = true;
            }
            catch (Exception e)
            {
                apiEvent.ErrorMessage = e.ToString();
                apiEvent.ProtocolStatusCode = (e as StorageException)?.RequestInformation.HttpStatusCode.ToStringInvariant();
                throw;
            }
            finally
            {
                apiEvent.Finish();
            }
        }

        /// <summary>
        ///     Dequeues one or more message from the queue
        /// </summary>
        /// <param name="maxItems">maximum items</param>
        /// <param name="leaseDuration">lease duration</param>
        /// <param name="timeout">timeout</param>
        /// <param name="retrypolicy">retryPolicy</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>list of dequeued items</returns>
        public async Task<IEnumerable<CloudQueueMessage>> DequeueAsync(
            int maxItems,
            TimeSpan leaseDuration,
            TimeSpan timeout,
            IRetryPolicy retrypolicy,
            CancellationToken cancellationToken)
        {
            OutgoingApiEventWrapper apiEvent = this.GetApiEvent(this.deqOp);

            QueueRequestOptions options = new QueueRequestOptions
            {
                MaximumExecutionTime = timeout,
                ServerTimeout = timeout
            };

            if (retrypolicy != null)
            {
                options.RetryPolicy = retrypolicy;
            }

            maxItems = Math.Min(maxItems, CloudQueueWrapper.MaxDequeueItems);

            IEnumerable<CloudQueueMessage> result;

            apiEvent.Start();

            try
            {
                result = await this.queue.GetMessagesAsync(maxItems, leaseDuration, options, null, cancellationToken).ConfigureAwait(false);
                apiEvent.Success = true;
            }
            catch (Exception e)
            {
                apiEvent.ErrorMessage = e.ToString();
                apiEvent.ProtocolStatusCode = (e as StorageException)?.RequestInformation.HttpStatusCode.ToStringInvariant();
                throw;
            }
            finally
            {
                apiEvent.Finish();
            }

            return result;
        }

        /// <summary>
        ///     Extends the least of a queue item
        /// </summary>
        /// <returns>true if the lease was extended or false if it cloud not be because the caller no longer owned it</returns>
        public async Task<bool> SetLeaseTimeoutAsync(
            CloudQueueMessage message,
            TimeSpan leaseDuration)
        {
            OutgoingApiEventWrapper apiEvent = this.GetApiEvent(this.updateOp);

            apiEvent.Start();

            try
            {
                await this.queue.UpdateMessageAsync(message, leaseDuration, MessageUpdateFields.Visibility).ConfigureAwait(false);
                apiEvent.Success = true;
            }
            catch (StorageException e)
            {
                // the update API returns NotFound if the lease could not be renewed because it's been dequeued by another caller
                if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    apiEvent.Success = true;
                    return false;
                }
                else
                {
                    apiEvent.ProtocolStatusCode = e.RequestInformation.HttpStatusCode.ToStringInvariant();
                    apiEvent.ErrorMessage = e.ToString();
                    throw;
                }
            }
            catch (Exception e)
            {
                apiEvent.ErrorMessage = e.ToString();
                throw;
            }
            finally
            {
                apiEvent.Finish();
            }

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> UpdateAsync(CloudQueueMessage message, TimeSpan leaseDuration)
        {
            OutgoingApiEventWrapper apiEvent = this.GetApiEvent(this.updateOp);

            apiEvent.Start();

            try
            {
                await this.queue.UpdateMessageAsync(message, leaseDuration, MessageUpdateFields.Content | MessageUpdateFields.Visibility).ConfigureAwait(false);
                apiEvent.Success = true;
            }
            catch (StorageException e)
            {
                // the update API returns NotFound if the lease could not be renewed because it's been dequeued by another caller
                if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    apiEvent.Success = true;
                    return false;
                }
                else
                {
                    apiEvent.ProtocolStatusCode = e.RequestInformation.HttpStatusCode.ToStringInvariant();
                    apiEvent.ErrorMessage = e.ToString();
                    throw;
                }
            }
            catch (Exception e)
            {
                apiEvent.ErrorMessage = e.ToString();
                throw;
            }
            finally
            {
                apiEvent.Finish();
            }

            return true;
        }

        /// <summary>
        ///     Completes the queue item
        /// </summary>
        /// <returns>true if the item was completed, or false if it could not be because the caller no longer owned it</returns>
        public async Task<bool> CompleteItemAsync(CloudQueueMessage message)
        {
            OutgoingApiEventWrapper apiEvent = this.GetApiEvent(this.completeOp);

            apiEvent.Start();

            try
            {
                await this.queue.DeleteMessageAsync(message).ConfigureAwait(false);
                apiEvent.Success = true;
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    apiEvent.Success = true;
                    return false;
                }
                else
                {
                    apiEvent.ProtocolStatusCode = e.RequestInformation.HttpStatusCode.ToStringInvariant();
                    apiEvent.ErrorMessage = e.ToString();
                    throw;
                }
            }
            catch (Exception e)
            {
                apiEvent.ErrorMessage = e.ToString();
                throw;
            }
            finally
            {
                apiEvent.Finish();
            }

            return true;
        }

        public async Task<bool> CompleteItemAsync(string id, string receipt)
        {
            OutgoingApiEventWrapper apiEvent = this.GetApiEvent(this.completeByIdOp);

            apiEvent.Start();

            try
            {
                await this.queue.DeleteMessageAsync(id, receipt).ConfigureAwait(false);
                apiEvent.Success = true;
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    apiEvent.Success = true;
                    return false;
                }
                else
                {
                    apiEvent.ProtocolStatusCode = e.RequestInformation.HttpStatusCode.ToStringInvariant();
                    apiEvent.ErrorMessage = e.ToString();
                    throw;
                }
            }
            catch (Exception e)
            {
                apiEvent.ErrorMessage = e.ToString();
                throw;
            }
            finally
            {
                apiEvent.Finish();
            }

            return true;
        }

        /// <inheritdoc />
        public async Task<CloudQueueMessage> PeekMessageAsync()
        {
            return await this.queue.PeekMessageAsync().ConfigureAwait(false);
        }

        /// <summary>
        ///     Generates an ApiEventWrapper for the queue calls
        /// </summary>
        /// <param name="operation">operation tag</param>
        /// <returns>resulting value</returns>
        private OutgoingApiEventWrapper GetApiEvent(string operation)
        {
            return new OutgoingApiEventWrapper
            {
                DependencyOperationVersion = string.Empty,
                DependencyOperationName = operation,
                DependencyName = CloudQueueWrapper.AzureStorageQueue,
                DependencyType = "WebService",
                PartnerId = CloudQueueWrapper.AzureStorageQueue,
                Success = false,
            };
        }
    }
}
