// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Storage.Queue;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.Storage.RetryPolicies;
    using Microsoft.PrivacyServices.Common.Azure;
    

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public class AzureQueueCloudQueue : IAzureCloudQueue
    {
        private static readonly TimeSpan InfiniteTTL = TimeSpan.FromSeconds(-1);

        protected readonly CloudQueue innerQueue;

        // default lease period for each of the messages in the queue
        protected readonly TimeSpan defaultLeasePeriod;

        private readonly QueueRequestOptions queueRequestOptions;

        public AzureQueueCloudQueue(CloudQueue innerQueue, TimeSpan defaultLeasePeriod, int retryDelayTimeInMilliseconds = 100, int retryMaxAttempts=3)
        {
            this.innerQueue = innerQueue;            
            this.defaultLeasePeriod = defaultLeasePeriod;
            this.queueRequestOptions = new QueueRequestOptions()
            {
                RetryPolicy = new ExponentialRetry(TimeSpan.FromMilliseconds(retryDelayTimeInMilliseconds), retryMaxAttempts)
            };
        }

        public virtual string AccountName => this.innerQueue.StorageUri.PrimaryUri.Host;

        public string QueueName => this.innerQueue.Name;

        public bool QueueExists { get; set; }

        public virtual Task AddMessageAsync(CloudQueueMessage message,
                                            TimeSpan? visibilityDelay,
                                            TimeSpan? timeToLive = null,
                                            CancellationToken cancellationToken = default(CancellationToken))
        {
            // By default, set timeToLive to infinite.
            timeToLive = timeToLive ?? InfiniteTTL;

            return this.innerQueue.AddMessageAsync(message: message, 
                timeToLive: timeToLive,
                initialVisibilityDelay: visibilityDelay,
                options: null,
                // TODO: remove this comment once fix is verified by 5/19/2023
                //options: this.queueRequestOptions,
                operationContext: null,
                cancellationToken: cancellationToken);
        }

        public virtual Task DeleteMessageAsync(CloudQueueMessage message)
        {
            return this.innerQueue.DeleteMessageAsync(message, this.queueRequestOptions, null);
        }

        public async Task<bool> ExistsAsync(CancellationToken cancellationToken)
        {
            if (!this.QueueExists)
            {
                this.QueueExists = await this.innerQueue.ExistsAsync(this.queueRequestOptions, null, cancellationToken);
            }

            return this.QueueExists;
        }

        public async Task EnsureQueueExistsAsync()
        {
            if (this.QueueExists)
            {
                return;
            }

            await Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async ev =>
                {
                    ev["AccountName"] = this.AccountName;
                    ev["QueueName"] = this.QueueName;

                    try
                    {
                        await this.innerQueue.CreateIfNotExistsAsync(this.queueRequestOptions, null);
                        this.QueueExists = true;
                    }
                    catch (Exception ex)
                    {
                        DualLogger.Instance.Error(nameof(AzureQueueCloudQueue), ex, $"Fail to check if queue Name={this.QueueName}, Account={this.AccountName} exists.");
                        throw;
                    }
                    finally
                    {
                        ev["QueueExists"] = this.QueueExists.ToString();
                    }
                });
        }

        public virtual async Task<int> GetCountAsync(CancellationToken token)
        {
            await this.innerQueue.FetchAttributesAsync(this.queueRequestOptions, null, token);
            return this.innerQueue.ApproximateMessageCount ?? -1;
        }

        /// <summary>
        /// Clear all messages in the queue
        /// </summary>
        /// <param name="token">The cancellation token</param>
        public virtual Task ClearAsync(CancellationToken token)
        {
            throw new NotSupportedException("Method is not supported. Override the implementation if you wish to use this.");
        }

        public virtual Task<IEnumerable<CloudQueueMessage>> GetMessagesAsync(int batchSize, TimeSpan? visibilityTimeout = null)
        {
            return this.innerQueue.GetMessagesAsync(batchSize, visibilityTimeout ?? this.defaultLeasePeriod, this.queueRequestOptions, null);
        }

        public virtual Task UpdateMessageAsync(CloudQueueMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields)
        {
            return this.innerQueue.UpdateMessageAsync(message, visibilityTimeout, updateFields, this.queueRequestOptions, null);
        }
    }
}
