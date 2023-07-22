namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using Newtonsoft.Json;

    /// <summary>
    /// Detail class MUST contain a Code value and Message value.
    /// It MAY contain a Target value, but no other properties.
    /// </summary>
    /// <remarks>
    /// Based on the ODATA standard and Microsoft OneAPI Guidelines.
    /// <c>https://docs.oasis-open.org/odata/odata-json-format/v4.0/os/odata-json-format-v4.0-os.html#_Toc372793091</c>
    /// </remarks>
    public sealed class Detail
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Detail" /> class.
        /// </summary>
        /// <param name="code">The error code.</param>
        /// <param name="message">A friendly message.</param>
        public Detail(string code, string message)
        {
            this.Code = code;
            this.Message = message;
        }

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        [JsonProperty(PropertyName = "code")]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the friendly message.
        /// </summary>
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the target of the error. This is an optional value.
        /// </summary>
        [JsonProperty(PropertyName = "target", NullValueHandling = NullValueHandling.Ignore)]
        public string Target { get; set; }
    }
}
