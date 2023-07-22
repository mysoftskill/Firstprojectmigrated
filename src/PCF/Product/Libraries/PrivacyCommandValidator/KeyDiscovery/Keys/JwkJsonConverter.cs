namespace Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery.Keys
{
    using System;

    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <inheritdoc />
    /// <summary>
    /// Converts a <see cref="JsonWebKey" /> to and from its serialized form.
    /// </summary>
    public class JwkJsonConverter : JsonConverter
    {
        private const string KeyTypeParameter = "kty";

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return typeof(JsonWebKey).IsAssignableFrom(objectType);
        }

        /// <inheritdoc />
        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            JObject item = JObject.Load(reader);
            var keyType = item[KeyTypeParameter].ToObject<JwkKeyType>();

            switch (keyType)
            {
                case JwkKeyType.EC:
                    var dsaKey = new DsaJsonWebKey();
                    serializer.Populate(item.CreateReader(), dsaKey);
                    return dsaKey;
                case JwkKeyType.RSA:
                    var rsaKey = new RsaJsonWebKey();
                    serializer.Populate(item.CreateReader(), rsaKey);
                    return rsaKey;
                default:
                    throw new KeyDiscoveryException($"Invalid key type: '{keyType.ToString()}'");
            }
        }

        /// <inheritdoc />
        public override void WriteJson(
            JsonWriter writer,
            object value,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
