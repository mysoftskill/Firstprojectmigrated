namespace Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery.Keys
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// The document containing the <see cref="JsonWebKey" />.
    /// </summary>
    public class JwkDocument
    {
        /// <summary>
        /// Gets or sets the list of keys in this document
        /// </summary>
        [JsonProperty(PropertyName = "keys", Required = Required.Always)]
        public IList<JsonWebKey> Keys { get; set; }
    }
}
