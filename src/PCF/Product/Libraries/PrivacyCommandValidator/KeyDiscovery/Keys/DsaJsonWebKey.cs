namespace Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery.Keys
{
    using Newtonsoft.Json;

    /// <inheritdoc />
    public class DsaJsonWebKey : JsonWebKey
    {
        /// <summary>
        /// Gets or sets the starting x coordinate.
        /// </summary>
        [JsonProperty(PropertyName = "x")]
        public string XCoordinate { get; set; }

        /// <summary>
        /// Gets or sets the starting y coordinate.
        /// </summary>
        [JsonProperty(PropertyName = "y")]
        public string YCoordinate { get; set; }
    }
}
