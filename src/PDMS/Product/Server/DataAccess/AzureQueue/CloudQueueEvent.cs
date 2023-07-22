namespace Microsoft.PrivacyServices.DataManagement.DataAccess.AzureQueue
{
    using Azure = Microsoft.Azure.Storage.Queue;

    /// <summary>
    /// An object for storing event information for passing to the instrumentation layer.
    /// </summary>
    public class CloudQueueEvent
    {
        /// <summary>
        /// Gets or sets the queue name.
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// Gets or sets the primary URI.
        /// </summary>
        public string PrimaryUri { get; set; }

        /// <summary>
        /// Gets or sets the secondary URI.
        /// </summary>
        public string SecondaryUri { get; set; }

        /// <summary>
        /// Gets or sets the message count.
        /// </summary>
        public int? MessageCount { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public Azure.CloudQueueMessage Message { get; set; }
    }
}
