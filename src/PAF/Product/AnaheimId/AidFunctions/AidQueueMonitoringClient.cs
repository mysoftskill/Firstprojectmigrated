namespace Microsoft.PrivacyServices.AnaheimId.AidFunctions
{
    using System;
    using System.Threading.Tasks;

    using global::Azure.Core;
    using global::Azure.Storage.Queues;
    using global::Azure.Storage.Queues.Models;

    /// <summary>
    /// Client for monitoring queue size.
    /// </summary>
    public class AidQueueMonitoringClient : IAidQueueMonitoringClient
    {
        private readonly QueueClient queueClient;
        private readonly string storageAccountName;
        private readonly string queueName;

        /// <summary>
        /// Initializes a new instance of the <see cref="AidQueueMonitoringClient"/> class.
        /// </summary>
        /// <param name="accountName">Account name.</param>
        /// <param name="queueName">Queue name.</param>
        /// <param name="credential">Client credential.</param>
        /// <param name="messageEncoding">Apparently Azure Functions assumes by default the message should be base64 encoded.</param>
        public AidQueueMonitoringClient(string accountName, string queueName, TokenCredential credential, bool messageEncoding = true)
        {
            this.storageAccountName = accountName ?? throw new ArgumentException(nameof(accountName));
            this.queueName = queueName ?? throw new ArgumentException(nameof(queueName));
            if (credential == null)
            {
                throw new ArgumentException(nameof(credential));
            }

            Uri queueUri = new Uri($"https://{this.storageAccountName}.queue.core.windows.net/{this.queueName}");

            QueueClientOptions options = new QueueClientOptions();
            if (messageEncoding)
            {
                options.MessageEncoding = QueueMessageEncoding.Base64;
            }
            else
            {
                options.MessageEncoding = QueueMessageEncoding.None;
            }

            this.queueClient = new QueueClient(queueUri, credential, options);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AidQueueMonitoringClient"/> class for emulator.
        /// </summary>
        /// <param name="queueName">Queue name.</param>
        /// <param name="messageEncoding">Apparently Azure Functions assumes by default the message should be base64 encoded.</param>
        public AidQueueMonitoringClient(string queueName, bool messageEncoding = true)
        {
            this.storageAccountName = "UseDevelopmentStorage=true";
            this.queueName = queueName;
            QueueClientOptions options = new QueueClientOptions();
            if (messageEncoding)
            {
                options.MessageEncoding = QueueMessageEncoding.Base64;
            }
            else
            {
                options.MessageEncoding = QueueMessageEncoding.None;
            }

            this.queueClient = new QueueClient("UseDevelopmentStorage=true", queueName, options);
        }

        /// <inheritdoc/>
        public string GetQueueName() => this.queueName;

        /// <inheritdoc/>
        public string GetStorageAccountName() => this.storageAccountName;

        /// <inheritdoc/>
        public async Task<int> GetQueueSizeAsync()
        {
            QueueProperties properties = this.queueClient.GetProperties();
            await Task.Yield();
            return properties.ApproximateMessagesCount;
        }
    }
}
