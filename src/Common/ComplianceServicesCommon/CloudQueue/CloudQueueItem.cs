namespace Microsoft.ComplianceServices.Common.Queues
{
    using global::Azure.Storage.Queues;
    using global::Azure.Storage.Queues.Models;
    using Newtonsoft.Json;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Cloud queue item.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CloudQueueItem<T> : ICloudQueueItem<T>
    {
        private readonly QueueClient queueClient;
        private readonly QueueMessage queueMessage;

        /// <summary>
        /// Create Cloud Queue Item.
        /// </summary>
        /// <param name="cloudQueue">Cloud queue.</param>
        /// <param name="queueMessage">Queue message.</param>
        public CloudQueueItem(QueueClient queueClient, QueueMessage queueMessage)
        {
            this.queueClient = queueClient;
            this.queueMessage = queueMessage;

            this.Data = JsonConvert.DeserializeObject<T>(this.queueMessage.MessageText);
        }

        /// <inheritdoc />
        public T Data { get; set; }

        /// <inheritdoc />
        public long DequeueCount => this.queueMessage.DequeueCount;

        /// <inheritdoc />
        public DateTimeOffset? ExpirationTime => this.queueMessage.ExpiresOn;

        /// <inheritdoc />
        public string MessageId => this.queueMessage.MessageId;

        /// <inheritdoc />
        public DateTimeOffset? InsertionTime => this.queueMessage.InsertedOn;

        /// <inheritdoc />
        public DateTimeOffset? NextVisibleTime => this.queueMessage.NextVisibleOn;

        /// <inheritdoc />
        public string PopReceipt => this.queueMessage.PopReceipt;

        /// <inheritdoc />
        public Task DeleteAsync()
        {
            return this.queueClient.DeleteMessageAsync(this.MessageId, this.PopReceipt);
        }

        /// <inheritdoc />
        public Task UpdateAsync(TimeSpan visibilityTimeout)
        {
            string messageBody = JsonConvert.SerializeObject(this.Data);
            return this.queueClient.UpdateMessageAsync(this.MessageId, this.PopReceipt, messageBody, visibilityTimeout);
        }
    }
}
