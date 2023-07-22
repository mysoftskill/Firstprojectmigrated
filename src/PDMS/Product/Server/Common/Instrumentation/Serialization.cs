namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Helper methods for serializing data for instrumentation purposes.
    /// </summary>
    public static class Serialization
    {
        /// <summary>
        /// The maximum size of the string builder.
        /// </summary>
        internal static readonly int BuilderLength = MaximumLength + TruncatedPrefix.Length;
        private const int MaximumLength = 2000;
        private const string TruncatedPrefix = "[TRUNCATED]";

        /// <summary>
        /// JSON serialization settings. Designed to minimize the payload as much as possible.
        /// </summary>
        private static readonly JsonSerializerSettings SerializationSettings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        /// <summary>
        /// Given an object, serialize it to JSON in such a way that it is safe to log in an SLL event.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The string.</returns>
        public static string Serialize(object data)
        {
            var str = JsonConvert.SerializeObject(data, SerializationSettings);

            if (str.Length > MaximumLength)
            {
                var sb = new StringBuilder(BuilderLength);
                sb.Append(TruncatedPrefix);
                sb.Append(str.Substring(0, MaximumLength));

                return sb.ToString();
            }
            else
            {
                return str;
            }
        }
    }
}