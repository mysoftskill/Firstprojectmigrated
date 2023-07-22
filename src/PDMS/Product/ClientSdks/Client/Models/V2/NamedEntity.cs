namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// Defines an entity that has additional metadata that is used for display and search purposes.
    /// </summary>
    public class NamedEntity : Entity
    {
        /// <summary>
        /// Gets or sets the name. This is a human readable value for display purposes.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description. This value is an optional value. Use this to provide any additional information that can help explain the entity.
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
    }
}