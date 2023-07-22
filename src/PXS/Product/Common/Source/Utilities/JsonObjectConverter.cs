// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;

    [Flags]
    public enum JsonConvertFlags
    {
        /// <summary>
        ///     no special behavior should be used
        /// </summary>
        None = 0,

        /// <summary>
        ///     examine strings to see if the have the format {datetime&lt;ISO 8601 date&gt;}'} and interpret it as a date if so
        /// </summary>
        UseDateTimeSyntax = 0x1,
    }

    /// <summary>
    ///    parses a JSON object into a nested set of string to object maps
    /// </summary>
    public class JsonObjectConverter : JsonConverter
    {
        private readonly bool parseDates;

        /// <summary>
        ///     Initializes a new instance of the JsonObjectConverter class
        /// </summary>
        /// <param name="flags">operatioin flags</param>
        internal JsonObjectConverter(JsonConvertFlags flags)
        {
            this.parseDates = (flags & JsonConvertFlags.UseDateTimeSyntax) != 0;
        }

        /// <summary>
        ///     Gets a value indicating whether this JsonConverter can write JSON
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        ///     Deserializes the object
        /// </summary>
        /// <typeparam name="T">type of t</typeparam>
        /// <param name="json">json to deserialize</param>
        /// <param name="operationFlags">
        ///     operation flags that control certain custom behaviors of the deserialization offered by this class
        /// </param>
        /// <param name="settings">JSON serializer settings</param>
        /// <returns>resulting value</returns>
        public static T Deserialize<T>(
            string json,
            JsonConvertFlags operationFlags,
            JsonSerializerSettings settings)
        {
            return JsonConvert.DeserializeObject<T>(json, JsonObjectConverter.SetupSettings(operationFlags, settings));
        }

        /// <summary>
        ///     Deserializes the object
        /// </summary>
        /// <typeparam name="T">type of t</typeparam>
        /// <param name="json">json to deserialize</param>
        /// <param name="operationFlags">
        ///     operation flags that control certain custom behaviors of the deserialization offered by this class
        /// </param>
        /// <returns>resulting value</returns>
        public static T Deserialize<T>(
            string json,
            JsonConvertFlags operationFlags)
        {
            return JsonObjectConverter.Deserialize<T>(json, operationFlags, null);
        }

        /// <summary>
        ///     Deserializes the object
        /// </summary>
        /// <param name="json">json to deserialize</param>
        /// <returns>resulting value</returns>
        public static T Deserialize<T>(string json)
        {
            return JsonObjectConverter.Deserialize<T>(json, JsonConvertFlags.None, null);
        }

        /// <summary>
        ///     Converts the JObject to the requested type
        /// </summary>
        /// <typeparam name="T">type of t</typeparam>
        /// <param name="obj">object to convert to the requested type</param>
        /// <param name="operationFlags">
        ///     operation flags that control certain custom behaviors of the deserialization offered by this class
        /// </param>
        /// <param name="settings">JSON serializer settings</param>
        /// <returns>resulting value</returns>
        public static T ToObject<T>(
            JToken obj,
            JsonConvertFlags operationFlags,
            JsonSerializerSettings settings)
        {
            JsonSerializer serializer = JsonSerializer.Create(JsonObjectConverter.SetupSettings(operationFlags, settings));
            return obj.ToObject<T>(serializer);
        }

        /// <summary>
        ///     Converts the JObject to the requested type
        /// </summary>
        /// <typeparam name="T">type of t</typeparam>
        /// <param name="obj">object to convert to the requested type</param>
        /// <param name="operationFlags">
        ///     operation flags that control certain custom behaviors of the deserialization offered by this class
        /// </param>
        /// <returns>resulting value</returns>
        public static T ToObject<T>(
            JToken obj,
            JsonConvertFlags operationFlags)
        {
            JsonSerializer serializer = JsonSerializer.Create(JsonObjectConverter.SetupSettings(operationFlags, null));
            return obj.ToObject<T>(serializer);
        }

        /// <summary>
        ///     Converts the JToken to the requested type
        /// </summary>
        /// <param name="obj">object to convert to the requested type</param>
        /// <returns>resulting value</returns>
        public static T ToObject<T>(JToken obj)
        {
            return JsonObjectConverter.ToObject<T>(obj, JsonConvertFlags.None, null);
        }

        /// <summary>
        ///     Writes the JSON representation of the object
        /// </summary>
        /// <param name="writer">JsonWriter to write to</param>
        /// <param name="value">value to write</param>
        /// <param name="serializer">calling serializer</param>
        public override void WriteJson(
            JsonWriter writer, 
            object value, 
            JsonSerializer serializer)
        {
            throw new JsonSerializationException("Write is not supported");
        }

        /// <summary>
        ///     Reads the JSON representation of the object
        /// </summary>
        /// <param name="reader">JsonReader to read from</param>
        /// <param name="objectType">object type</param>
        /// <param name="existingValue">existing value of object being read</param>
        /// <param name="serializer">calling serializer</param>
        /// <returns>object value</returns>
        public override object ReadJson(
            JsonReader reader, 
            Type objectType, 
            object existingValue, 
            JsonSerializer serializer)
        {
            return this.ProcessProperty(reader);
        }

        /// <summary>
        ///     Determines whether this instance can convert the specified object type
        /// </summary>
        /// <param name="objectType">object type</param>
        /// <returns>true if this instance can convert the specified object type; false otherwise</returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IDictionary<string, object>) ||
                   objectType == typeof(IReadOnlyDictionary<string, object>) ||
                   objectType == typeof(IList<object>) ||
                   objectType == typeof(ICollection<object>) ||
                   objectType == typeof(IEnumerable<object>);
        }

        /// <summary>
        ///     Setups the JsonSerializerSettings for the conversion
        /// </summary>
        /// <param name="flags">converter flags</param>
        /// <param name="settings">settings from caller</param>
        /// <returns>settings to use</returns>
        private static JsonSerializerSettings SetupSettings(
            JsonConvertFlags flags,
            JsonSerializerSettings settings)
        {
            if (settings == null)
            {
                settings = new JsonSerializerSettings();
            }

            if (settings.Converters == null ||
                settings.Converters.Any(o => o.GetType() == typeof(JsonObjectConverter)) == false)
            {
                List<JsonConverter> converters = new List<JsonConverter> { new JsonObjectConverter(flags) };

                if (settings.Converters != null)
                {
                    converters.AddRange(settings.Converters);
                }

                settings.Converters = converters;
            }

            return settings;
        }

        /// <summary>
        ///     determines whether the specified token is a primitive type or not
        /// </summary>
        /// <param name="token">token to test</param>
        /// <returns>true if primative; false otherwise</returns>
        private static bool IsPrimitiveToken(JsonToken token)
        {
            switch (token)
            {
                case JsonToken.Integer:
                case JsonToken.Float:
                case JsonToken.String:
                case JsonToken.Boolean:
                case JsonToken.Undefined:
                case JsonToken.Null:
                case JsonToken.Date:
                case JsonToken.Bytes:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        ///     Throws an error with the message prefix and info from the reader
        /// </summary>
        /// <param name="prefix">message prefix</param>
        /// <param name="reader">JsonReader with the error</param>
        private static object ThrowError(
            string prefix,
            JsonReader reader)
        {
            IJsonLineInfo lineInfo = reader as IJsonLineInfo;
            string path = reader.Path;

            string msg = (lineInfo?.HasLineInfo() ?? false) ?
                $"{prefix}: '{path}', line {lineInfo.LineNumber}, position {lineInfo.LinePosition}" :
                $"{prefix}: '{path}'";

            throw new JsonSerializationException(msg);
        }

        /// <summary>
        ///     Reads a JSON object
        /// </summary>
        /// <param name="reader">JsonReader to read from</param>
        /// <returns>resulting value</returns>
        private object ReadObject(JsonReader reader)
        {
            IDictionary<string, object> result = new Dictionary<string, object>();

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.Comment:
                        continue;

                    case JsonToken.PropertyName:
                    {
                        object valueRaw;
                        string name = reader.Value.ToString();

                        if (reader.Read() == false)
                        {
                            return JsonObjectConverter.ThrowError("Parse error reading name-value pair", reader);
                        }

                        valueRaw = this.ProcessProperty(reader);

                        if (this.parseDates && valueRaw is string value)
                        {
                            value = value.Trim();
                            if (value.Length > 0 && value[0] == '{' && value[value.Length - 1] == '}')
                            {
                                const string Prefix = "{datetime'";
                                const string Suffix = "'}";

                                if (value.StartsWith(Prefix) && value.EndsWith(Suffix))
                                {
                                    value = value.Substring(Prefix.Length, value.Length - (Prefix.Length + Suffix.Length));

                                    try
                                    {
                                        valueRaw = DateTimeOffset.Parse(value);
                                    }
                                    catch (FormatException)
                                    {
                                        return JsonObjectConverter.ThrowError("Failed to parse ", reader);
                                    }
                                }
                            }
                        }

                        result[name] = valueRaw;
                        break;
                    }

                    case JsonToken.EndObject:
                        return new ReadOnlyDictionary<string, object>(result);
                }
            }

            return JsonObjectConverter.ThrowError("Parse error reading object", reader);
        }

        /// <summary>
        ///     reads a JSON array
        /// </summary>
        /// <param name="reader">JsonReader to read from</param>
        /// <returns>resulting value</returns>
        private object ReadCollection(JsonReader reader)
        {
            List<object> result = new List<object>();

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.Comment:
                        break;

                    case JsonToken.EndArray:
                        return new ReadOnlyCollection<object>(result);

                    default:
                        result.Add(this.ProcessProperty(reader));
                        break;
                }
            }

            return JsonObjectConverter.ThrowError("Parse error reading collection", reader);
        }

        /// <summary>
        ///     Reads the next object out of the reader
        /// </summary>
        /// <param name="reader">JsonReader to read from</param>
        /// <returns>resulting value</returns>
        private object ProcessProperty(JsonReader reader)
        {
            while (reader.TokenType == JsonToken.Comment)
            {
                if (reader.Read() == false)
                {
                    return JsonObjectConverter.ThrowError("Parse error reading value", reader);
                }
            }

            switch (reader.TokenType)
            {
                case JsonToken.StartObject:
                    return this.ReadObject(reader);

                case JsonToken.StartArray:
                    return this.ReadCollection(reader);

                default:
                {
                    if (JsonObjectConverter.IsPrimitiveToken(reader.TokenType))
                    {
                        return reader.Value;
                    }

                    return JsonObjectConverter.ThrowError(
                        "Parse error reading value; unexpected token '" + reader.TokenType + "'",
                        reader);
                }
            }
        }
    }
}
