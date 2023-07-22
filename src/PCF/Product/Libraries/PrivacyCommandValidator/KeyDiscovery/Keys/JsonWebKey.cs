namespace Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery.Keys
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// JSON Web Key (JWK)
    /// </summary>
    /// <remarks>
    ///     <see ref="https://tools.ietf.org/html/rfc7517" />
    /// </remarks>
    [JsonConverter(typeof(JwkJsonConverter))]
    public class JsonWebKey
    {
        /// <summary>
        /// Creates a new instance from a serialized JWK.
        /// </summary>
        /// <param name="serializedJwk">JSON Serialized JWK</param>
        /// <returns>A new JWK instance</returns>
        public static JsonWebKey ParseKey(string serializedJwk)
        {
            return JsonConvert.DeserializeObject<JsonWebKey>(serializedJwk, new JwkJsonConverter());
        }

        /// <summary>
        /// Gets or sets the key identifier to match a specific key.
        /// </summary>
        [JsonProperty(PropertyName = "kid", Required = Required.Always)]
        public string KeyId { get; set; }

        /// <summary>
        /// Gets or sets the key type.
        /// </summary>
        [JsonProperty(PropertyName = "kty", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public JwkKeyType KeyType { get; set; }

        /// <summary>
        /// Gets or sets the public key use.
        /// </summary>
        [JsonProperty(PropertyName = "use", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public JwkKeyUse? PublicKeyUse { get; set; }

        /// <summary>
        /// Gets or sets the X.509 certificate chain.
        /// Each chain element is the base64-encoded DER PKIX certificate value.
        /// The PKIX certificate containing the key value MUST be the first certificate.
        /// </summary>
        [JsonProperty(PropertyName = "x5c", Required = Required.Always)]
        public string[] X509Chain { get; set; }

        /// <summary>
        /// Gets or sets the X.509 certificate SHA-1 thumbprint.
        /// </summary>
        [JsonProperty(PropertyName = "x5t", Required = Required.Always)]
        public string X509Thumbprint { get; set; }
    }
}
