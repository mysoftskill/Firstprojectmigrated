namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// The model for a service search result. There are more properties in service tree than what is listed below.
    /// </summary>
    public class ServiceSearchResult
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [JsonProperty("ServiceOid")]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [JsonProperty("ServiceName")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [JsonProperty("ServiceDescription")]
        public string Description { get; set; }
    }
}