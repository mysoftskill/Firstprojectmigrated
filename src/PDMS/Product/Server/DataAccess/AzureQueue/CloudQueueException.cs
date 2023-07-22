namespace Microsoft.PrivacyServices.DataManagement.DataAccess.AzureQueue
{
    using System;

    /// <summary>
    /// Indicates an exception that occurred while accessing an Azure Queue.
    /// </summary>
    public class CloudQueueException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudQueueException" /> class.
        /// </summary>
        /// <param name="eventName">The action that caused the exception.</param>
        /// <param name="eventData">The event data associated with the exception.</param>
        /// <param name="ex">The exception.</param>
        public CloudQueueException(string eventName, CloudQueueEvent eventData, Exception ex) : base(ex.Message, ex)
        {
            this.EventName = eventName;
            this.EventData = eventData;
        }

        /// <summary>
        /// Gets the name to log.
        /// </summary>
        public string EventName { get; private set; }

        /// <summary>
        /// Gets the event data to log.
        /// </summary>
        public CloudQueueEvent EventData { get; private set; }
    }
}