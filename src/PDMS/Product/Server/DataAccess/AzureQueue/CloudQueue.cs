namespace Microsoft.PrivacyServices.DataManagement.DataAccess.AzureQueue
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.FileSystem.KeyVault;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.Azure.Storage.Queue.Protocol;

    using Azure = Microsoft.Azure.Storage.Queue;

    /// <summary>
    /// An adapter for the Azure Queues so that we have an interface for unit test purposes.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CloudQueue : ICloudQueue
    {
        private readonly Azure.CloudQueue queue;
        private readonly ISessionFactory sessionFactory;
        private readonly ICloudQueueConfig config;
        private readonly IDateFactory dateFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudQueue"/> class.
        /// </summary>
        /// <param name="queue">The Azure queue to adapt.</param>
        /// <param name="sessionFactory">The session factory.</param>
        /// <param name="config">The queue specific config.</param>
        /// <param name="dateFactory">The data factory instance.</param>
        public CloudQueue(Azure.CloudQueue queue, ISessionFactory sessionFactory, ICloudQueueConfig config, IDateFactory dateFactory)
        {
            this.queue = queue;
            this.sessionFactory = sessionFactory;
            this.config = config;
            this.dateFactory = dateFactory;
        }

        /// <summary>
        /// Add a message to the queue.
        /// </summary>
        /// <param name="message">The message to add.</param>
        /// <param name="timeToLive">Optional TimeSpan indicating how long message can stay in queue; if null, default is 7 days.</param>
        /// <returns>A task to execute asynchronously.</returns>
        public Task AddMessageAsync(Azure.CloudQueueMessage message, TimeSpan? timeToLive = null)
        {
            return this.Instrument(
                "AddMessageAsync",
                eventData =>
                {
                    eventData.Message = message;

                    return this.queue.AddMessageAsync(message, timeToLive, null, null, null);
                });
        }

        /// <summary>
        /// Retrieve the next message in the queue.
        /// </summary>
        /// <returns>The next message in the queue.</returns>
        public async Task<Azure.CloudQueueMessage> GetMessageAsync()
        {
            var e = await this.Instrument(
                "GetMessageAsync",
                async eventData =>
                {
                    var message = await this.queue.GetMessageAsync().ConfigureAwait(false);
                    eventData.Message = message;
                    return eventData;
                }).ConfigureAwait(false);

            return e.Message;
        }

        /// <summary>
        /// Delete a message in the queue.
        /// </summary>
        /// <param name="message">The message to delete.</param>
        /// <returns>A task to execute the action asynchronously.</returns>
        public Task DeleteMessageAsync(Azure.CloudQueueMessage message)
        {
            return this.Instrument(
                "DeleteMessageAsync",
                eventData =>
                {
                    eventData.Message = message;
                    return this.queue.DeleteMessageAsync(message);
                });
        }

        /// <summary>
        /// Get the approximate message count on the queue.
        /// </summary>
        /// <returns>The approximate count.</returns>
        public async Task<int> GetMessageCountAsync()
        {
            var e = await this.Instrument(
                "GetMessageCountAsync",
                async eventData =>
                {
                    await this.queue.FetchAttributesAsync().ConfigureAwait(false);
                    eventData.MessageCount = this.queue.ApproximateMessageCount;
                    return eventData;
                }).ConfigureAwait(false);

            return e.MessageCount ?? 0;
        }

        /// <summary>
        /// Delete the queue if it exists.
        /// </summary>
        /// <returns>A task to execute the action asynchronously.</returns>
        public Task DeleteQueueAsync()
        {
            return this.Instrument("DeleteQueueAsync", _ => this.queue.DeleteIfExistsAsync());
        }

        /// <summary>
        /// Create the queue if it does not exist.
        /// </summary>
        /// <returns>A task to execute the action asynchronously.</returns>
        public async Task InitializeAsync()
        {
            await this.Instrument("CreateIfNotExistsAsync", _ => this.queue.CreateIfNotExistsAsync()).ConfigureAwait(false);
        }

        /// <summary>
        /// Create an access token for the queue.
        /// </summary>
        /// <returns>The access token.</returns>
        public string CreateAccessToken()
        {
            throw new InvalidOperationException("CreateAccessToken is not supported");
        }

        /// <summary>
        /// Get storage URI of the queue.
        /// </summary>
        /// <returns>Storage URI of the queue.</returns>
        public string GetStorageUri()
        {
            return this.queue.StorageUri.ToString();
        }

        private Task<CloudQueueEvent> Instrument(string methodName, Func<CloudQueueEvent, Task> action)
        {
            return this.Instrument(
                methodName,
                async e =>
                {
                    await action(e).ConfigureAwait(false);
                    return e;
                });
        }

        private Task<CloudQueueEvent> Instrument(string methodName, Func<CloudQueueEvent, Task<CloudQueueEvent>> action)
        {
            return this.sessionFactory.InstrumentAsync<CloudQueueEvent, CloudQueueException>(
                $"AzureQueue.{methodName}",
                SessionType.Outgoing,
                async () =>
                {
                    var eventData = new CloudQueueEvent
                    {
                        QueueName = this.queue.Name,
                        PrimaryUri = this.queue.StorageUri.PrimaryUri?.ToString(),
                        SecondaryUri = this.queue.StorageUri.SecondaryUri?.ToString()
                    };

                    CloudQueueEvent result = null;
                    try
                    {
                        result = await action(eventData).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        throw new CloudQueueException($"AzureQueue.{methodName}", eventData, ex);
                    }
                    return result;
                });
        }
    }
}
