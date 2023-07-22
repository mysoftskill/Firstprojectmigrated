namespace Microsoft.ComplianceServices.Common.Queues
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using global::Azure.Core;
    using global::Azure.Storage.Queues;
    using global::Azure.Storage.Queues.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Cloud queue. Implementation based on Azure.Storage.Queues.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CloudQueue<T> : ICloudQueue<T>
    {
        private readonly QueueClient queueClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicCloudQueueClient{T}"/> class using <see cref="ClientCertificateCredential{T}"/>.
        /// </summary>
        /// <param name="accountName">Account name.</param>
        /// <param name="queueName">Queue name.</param>
        /// <param name="credential">Client credential.</param>
        /// <param name="messageEncoding">Apparently Azure Functions assumes by default the message should be base64 encoded.</param>
        public CloudQueue(
            string accountName,
            string queueName,
            TokenCredential credential,
            bool messageEncoding = true,
            int retryDelayTimeInSeconds = 2,
            int maxRetries = 5,
            RetryMode retryMode = RetryMode.Exponential,
            int retryMaxDelayTimeInSeconds = 10
            )
        {
            this.StorageAccountName = accountName;
            this.QueueName = queueName;

            Uri queueUri = new Uri($"https://{this.StorageAccountName}.queue.core.windows.net/{this.QueueName}");

            QueueClientOptions options = new QueueClientOptions()
            {
                Retry =
                {
                    Delay = TimeSpan.FromSeconds(retryDelayTimeInSeconds),
                    MaxRetries = maxRetries,
                    Mode = retryMode,
                    MaxDelay = TimeSpan.FromSeconds(retryMaxDelayTimeInSeconds),
                }
            };
            if (messageEncoding)
            {
                options.MessageEncoding = QueueMessageEncoding.Base64;
            }
            else
            {
                options.MessageEncoding = QueueMessageEncoding.None;
            };

            this.queueClient = new QueueClient(queueUri, credential, options);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicCloudQueueClient{T}"/> class.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <param name="messageEncoding">Apparently Azure Functions assumes by default the message should be base64 encoded.</param>
        public CloudQueue(string queueName, bool messageEncoding = true)
        {
            this.StorageAccountName = "UseDevelopmentStorage=true";
            this.QueueName = queueName;
            QueueClientOptions options = new QueueClientOptions();
            if (messageEncoding)
            {
                options.MessageEncoding = QueueMessageEncoding.Base64;
            }
            else
            {
                options.MessageEncoding = QueueMessageEncoding.None;
            };

            this.queueClient = new QueueClient("UseDevelopmentStorage=true", queueName, options);
        }

        /// <inheritdoc />
        public string StorageAccountName { get; }

        /// <inheritdoc />
        public string QueueName { get; }

        /// <inheritdoc />
        public async Task<ICloudQueueItem<T>> DequeueAsync(TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default)
        {
            var message = await this.queueClient.ReceiveMessageAsync(visibilityTimeout, cancellationToken).ConfigureAwait(false);
            if (message.Value == null)
            {
                return null;
            }

            return new CloudQueueItem<T>(this.queueClient, message);
        }

        /// <inheritdoc />
        public async Task<IList<ICloudQueueItem<T>>> DequeueBatchAsync(TimeSpan? visibilityTimeout = null, int maxCount = 32, CancellationToken cancellationToken = default)
        {
            IList<ICloudQueueItem<T>> queueItems = new List<ICloudQueueItem<T>>();

            var messages = await this.queueClient.ReceiveMessagesAsync(
                maxMessages: maxCount,
                visibilityTimeout: visibilityTimeout,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (var message in messages.Value)
            {
                queueItems.Add(new CloudQueueItem<T>(this.queueClient, message));
            }

            return queueItems;
        }

        /// <inheritdoc />
        public async Task EnqueueAsync(T data, TimeSpan? timeToLive = default, TimeSpan? invisibilityDelay = default, CancellationToken cancellationToken = default)
        {
            if (timeToLive == default)
            {
                // set default to 90 days
                timeToLive = TimeSpan.FromDays(90);
            }

            await this.queueClient.CreateIfNotExistsAsync();

            string messageText = JsonConvert.SerializeObject(data);
            await this.queueClient.SendMessageAsync(
                messageText: messageText,
                visibilityTimeout: invisibilityDelay,
                timeToLive: timeToLive,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<int> GetQueueSizeAsync()
        {
            QueueProperties properties = queueClient.GetProperties();
            await Task.Yield();
            return properties.ApproximateMessagesCount;
        }

        /// <inheritdoc />
        public Task CreateIfNotExistsAsync()
        {
            return this.queueClient.CreateIfNotExistsAsync();
        }
    }
}
