namespace Microsoft.PrivacyServices.DataManagement.Client
{
    using System;

    using Microsoft.PrivacyServices.Identity;

    using Newtonsoft.Json;

    /// <summary>
    /// A converter for the AssetQualifier. AssetQualifier has a custom string representation.
    /// </summary>
    public class AssetQualifierConverter : JsonConverter
    {
        /// <summary>
        /// Determines whether or not the type can be converted.
        /// </summary>
        /// <param name="objectType">The type to convert.</param>
        /// <returns>True if it can be converted.</returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(AssetQualifier);
        }

        /// <summary>
        /// Deserializes the JSON to the derived type.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="objectType">The object type.</param>
        /// <param name="existingValue">This parameter is not used.</param>
        /// <param name="serializer">The serializer.</param>
        /// <returns>The deserialized derived object.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value is string value)
            {
                return AssetQualifier.Parse(value);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// This method is not implemented.
        /// </summary>
        /// <param name="writer">The parameter is not used.</param>
        /// <param name="value">The parameter is not used.</param>
        /// <param name="serializer">The parameter is not used.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((value as AssetQualifier)?.Value);
        }        
    }
}