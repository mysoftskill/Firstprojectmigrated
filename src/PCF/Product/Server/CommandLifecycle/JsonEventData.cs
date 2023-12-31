namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.EventHubs;

    /// <summary>
    /// Provides the core constructs of EventHub's EventData class in a JSON-serializable package.
    /// </summary>
    internal class JsonEventData
    {
        /// <summary>
        /// Creates an empty JsonEventData object.
        /// </summary>
        public JsonEventData()
        {
        }

        /// <summary>
        /// Creates a new JsonEventData from the given event data object.
        /// </summary>
        public JsonEventData(EventData eventHubData)
        {
            this.Data = eventHubData.Body.Array;

            foreach (var item in eventHubData.Properties)
            {
                if (item.Value is string value)
                {
                    this.Properties[item.Key] = value;
                }
            }
        }

        /// <summary>
        /// The name of the event.
        /// </summary>
        public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// The raw data of the event.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Converts to Event Data.
        /// </summary>
        public EventData ToEventData()
        {
            var data = new EventData(this.Data);
            foreach (var property in this.Properties)
            {
                data.Properties[property.Key] = property.Value;
            }

            return data;
        }
    }
}
