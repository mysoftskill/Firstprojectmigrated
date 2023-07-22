namespace Microsoft.PrivacyServices.DataManagement.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.PrivacyServices.Policy;

    using Newtonsoft.Json;

    /// <summary>
    /// A custom converter to handle serialization and deserialization of the CapabilityId list objects.
    /// </summary>
    public class CapabilityTypeConverter : JsonConverter
    {
        /// <summary>
        /// Serializes the CapabilityId list object into JSON string.
        /// </summary>
        /// <param name="writer">The LSON writer.</param>
        /// <param name="value">The object type that needs to be serialized.</param>
        /// <param name="serializer">The parameter is not used.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(string.Join(",", (value as IEnumerable<CapabilityId>).Select(t => t.Value)));
        }

        /// <summary>
        /// Reads the current property as a JSON string, and then deserializes it.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="objectType">The expected object's type.</param>
        /// <param name="existingValue">The parameter is not used.</param>
        /// <param name="serializer">The parameter is not used.</param>
        /// <returns>The deserialized object.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            IList<CapabilityId> result = new List<CapabilityId>();

            var stringValue = (string)reader.Value;

            if (string.IsNullOrEmpty(stringValue))
            {
                return result;
            }

            var parts = stringValue.Split(',');

            foreach (var part in parts)
            {
                if (Policies.Current.Capabilities.TryCreateId(part, out CapabilityId capabilityId))
                {
                    result.Add(capabilityId);
                }
            }

            return result;
        }

        /// <summary>
        /// This converter is only set on properties, so it is always assumed to be valid.
        /// </summary>
        /// <param name="objectType">The parameter is not used.</param>
        /// <returns>Always returns true.</returns>
        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}