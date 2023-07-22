namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// Converts a Service Tree Response Error JSON value into a concrete object.
    /// </summary>
    public class ServiceTreeResponseErrorConverter : JsonConverter
    {
        /// <summary>
        /// Gets a value indicating whether or not this can be used for writes.
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Whether or not the given type can be converted.
        /// Since this converter is applied as an attribute, this is always true.
        /// </summary>
        /// <param name="objectType">The parameter is not used.</param>
        /// <returns>Always returns true.</returns>
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        /// <summary>
        /// Deserializes the JSON data into a ServiceTreeResponseError.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="objectType">The parameter is not used.</param>
        /// <param name="existingValue">The parameter is not used.</param>
        /// <param name="serializer">The parameter is not used.</param>
        /// <returns>The parsed ServiceTreeResponseError.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var data = this.GetObject(reader) as IDictionary<string, object>;
            
            return new ServiceTreeResponseError(data);
        }

        /// <summary>
        /// Writing is not supported.
        /// </summary>
        /// <param name="writer">The parameter is not used.</param>
        /// <param name="value">The parameter is not used.</param>
        /// <param name="serializer">The parameter is not used.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new InvalidOperationException("This converter does not support writes.");
        }

        /// <summary>
        /// Read the value of a property into a generic data structure.
        /// The reader will be pointing to the current item when it returns.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <returns>The parsed value as an object.</returns>
        private object GetValue(JsonReader reader)
        {
            object value = null;
            do
            {
                switch (reader.TokenType)
                {
                    case JsonToken.Boolean:
                    case JsonToken.Bytes:
                    case JsonToken.Date:
                    case JsonToken.Float:
                    case JsonToken.Integer:
                    case JsonToken.String:
                        value = reader.Value;
                        break;
                    case JsonToken.StartObject:
                        value = this.GetObject(reader);
                        break;
                    case JsonToken.StartArray:
                        value = this.GetArray(reader);
                        break;
                    default:
                        break;
                }
            }
            while (value == null && reader.Read());

            return value;
        }

        /// <summary>
        /// Read an object into a generic key/value data structure.
        /// Each property is the key and each value is added as a generic object.
        /// The reader will be pointing to the end of the object when it returns.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <returns>The parsed object data as an IDictionary{string,object}.</returns>
        private IDictionary<string, object> GetObject(JsonReader reader)
        {
            var data = new Dictionary<string, object>();

            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                { 
                    data.Add(reader.Value as string, this.GetValue(reader));
                }
            }

            return data;
        }

        /// <summary>
        /// Read an array into a generic data structure.
        /// Each item is a generic object value.
        /// The reader will be pointing to the end of the array when it returns.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <returns>The parsed array data as an IEnumerable{object}.</returns>
        private IEnumerable<object> GetArray(JsonReader reader)
        {
            var data = new List<object>();

            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            { 
                data.Add(this.GetValue(reader));
            }                

            return data;
        }
    }
}