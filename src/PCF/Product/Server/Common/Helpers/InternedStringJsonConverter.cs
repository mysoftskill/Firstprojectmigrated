namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// A JSON converter for JSON.NET that uses string interning for returned values. Interning a string returns a global
    /// reference to the original string, so that the original can be discarded very quickly. This helps to keep redundant
    /// strings from leaking into gen2.
    /// </summary>
    public sealed class InternedStringJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string next = (string)reader.Value;

            if (next != null)
            {
                next = string.Intern(next);
            }

            return next;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((string)value);
        }
    }
}
