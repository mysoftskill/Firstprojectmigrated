namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// A base class that provides a common set of properties across all service entities.
    /// </summary>
    public abstract class Entity
    {
        /// <summary>
        /// Gets or sets the unique Id for this entity.
        /// This information is service generated. 
        /// Setting or modifying this value will result in an error.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets ETag for the entity. 
        /// This information is service generated. 
        /// Setting or modifying this value will result in an error.
        /// This property must match the stored value for change operations to succeed.
        /// If the value has changed, then an error will occur.
        /// </summary>
        [JsonProperty(PropertyName = "eTag")]
        public string ETag { get; set; }

        /// <summary>
        /// Gets or sets the tracking details for the entity. 
        /// This information is service generated. 
        /// Setting or modifying these values will result in an error.
        /// </summary>
        /// <remarks>
        /// This value is only returned if it is explicitly requested in a $select statement.
        /// </remarks>
        [JsonProperty(PropertyName = "trackingDetails")]
        public TrackingDetails TrackingDetails { get; set; }
    }
}