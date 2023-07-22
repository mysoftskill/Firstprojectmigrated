namespace Microsoft.PrivacyServices.DataManagement.DataAccess.AzureQueue
{
    using System.Threading.Tasks;
    using System;

    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.Azure.Storage.Queue;

    /// <summary>
    /// Defines methods for interacting with Azure Queues.
    /// </summary>
    public interface ICloudQueue : IInitializer
    {
        /// <summary>
        /// Add a message to the queue.
        /// </summary>
        /// <param name="message">The message to add.</param>
        /// <param name="timeToLive">Optional TimeSpan indicating how long message can stay in queue; if null, default is 7 days</param>
        /// <returns>A task to execute asynchronously.</returns>
        Task AddMessageAsync(CloudQueueMessage message, TimeSpan? timeToLive = null);

        /// <summary>
        /// Retrieve the next message in the queue.
        /// </summary>
        /// <returns>The next message in the queue.</returns>
        Task<CloudQueueMessage> GetMessageAsync();

        /// <summary>
        /// Delete a message in the queue.
        /// </summary>
        /// <param name="message">The message to delete.</param>
        /// <returns>A task to execute the action asynchronously.</returns>
        Task DeleteMessageAsync(CloudQueueMessage message);

        /// <summary>
        /// Get the approximate message count on the queue.
        /// </summary>
        /// <returns>The approximate count.</returns>
        Task<int> GetMessageCountAsync();

        /// <summary>
        /// Delete the queue if it exists.
        /// </summary>
        /// <returns>A task to execute the action asynchronously.</returns>
        Task DeleteQueueAsync();

        /// <summary>
        /// Create an access token for the queue.
        /// </summary>
        /// <returns>The access token.</returns>
        string CreateAccessToken();

        /// <summary>
        /// Get storage URI of the queue.
        /// </summary>
        /// <returns>Storage URI of the queue.</returns>
        string GetStorageUri();
    }
}
