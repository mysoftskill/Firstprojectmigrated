namespace Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery.Keys
{
    using Newtonsoft.Json;

    /// <inheritdoc />
    public class RsaJsonWebKey : JsonWebKey
    {
        /// <summary>
        /// Gets or sets the RSA algorithm exponent.
        /// </summary>
        [JsonProperty(PropertyName = "e")]
        public string Exponent { get; set; }

        /// <summary>
        /// Gets or sets the RSA algorithm modulus.
        /// </summary>
        [JsonProperty(PropertyName = "n")]
        public string Modulus { get; set; }
    }
}
