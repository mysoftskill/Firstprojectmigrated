namespace Microsoft.PrivacyServices.DataManagement.Client
{
    using System;
    using System.Globalization;
    using System.Net;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Defines a constant set of serialization settings for the client.
    /// </summary>
    public static class SerializerSettings
    {
        private static readonly Lazy<JsonSerializerSettings> Value = new Lazy<JsonSerializerSettings>(
            () =>
            {
                var value = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    Culture = CultureInfo.InvariantCulture,
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                };

                value.Converters.Add(new StringEnumConverter());

                return value;
            },
            true);

        /// <summary>
        /// Gets the serialization settings.
        /// </summary>
        public static JsonSerializerSettings Instance
        {
            get
            {
                return Value.Value;
            }
        }

        /// <summary>
        /// Escape data so that it is valid for use in OData query parameters.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <returns>The escaped value.</returns>
        public static string EscapeForODataQuery(string value)
        {
            return WebUtility.UrlEncode(value);
        }
    }
}