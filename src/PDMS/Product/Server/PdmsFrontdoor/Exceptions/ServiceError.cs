namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System.Text;
    using Newtonsoft.Json;
    
    /// <summary>
    /// ServiceError class MUST contain a Code value and Message value.
    /// It MAY contain a target value, details value and inner error value.
    /// It MUST not contain any additional service defined values.
    /// The service SHOULD define a constant set of general root error messages.
    /// More specific errors SHOULD use inner error values to identify themselves.
    /// </summary>
    /// <remarks>
    /// Based on the ODATA standard and Microsoft OneAPI Guidelines.
    /// <c>https://docs.oasis-open.org/odata/odata-json-format/v4.0/os/odata-json-format-v4.0-os.html#_Toc372793091</c>
    /// </remarks>
    public sealed class ServiceError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceError" /> class.
        /// </summary>
        /// <param name="code">The general error code.</param>
        /// <param name="message">A friendly error message.</param>
        public ServiceError(string code, string message)
        {
            this.Code = code;
            this.Message = message;
        }

        /// <summary>
        /// Gets or sets the error code. Callers will key off of this field.
        /// </summary>
        [JsonProperty(PropertyName = "code")]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets a friendly error message. Callers should not rely on this field. It is for debugging purposes only.
        /// </summary>
        [JsonProperty(PropertyName = "message", NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the source of the error. Optional.
        /// </summary>
        [JsonProperty(PropertyName = "target", NullValueHandling = NullValueHandling.Ignore)]
        public string Target { get; set; }

        /// <summary>
        /// Gets or sets more specific error information. Should not be combined with details.
        /// </summary>
        [JsonProperty(PropertyName = "innererror", NullValueHandling = NullValueHandling.Ignore)]
        public InnerError InnerError { get; set; }

        /// <summary>
        /// Gets or sets multiple specific error values. Should not be combined with inner error.
        /// </summary>
        [JsonProperty(PropertyName = "details", NullValueHandling = NullValueHandling.Ignore)]
        public Detail[] Details { get; set; }

        /// <summary>
        /// Converts the object to a simple string value.
        /// </summary>
        /// <returns>The object as a string.</returns>
        public override string ToString()
        {
            return this.FlattenErrorCode();
        }

        /// <summary>
        /// Iterates through all inner errors and joins the error codes into a single value.
        /// </summary>
        /// <returns>A unified error code.</returns>
        private string FlattenErrorCode()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(this.Code);

            var innerError = this.InnerError;
            while (innerError != null)
            {
                stringBuilder.Append(":");
                stringBuilder.Append(innerError.Code);
                innerError = innerError.NestedError;
            }

            return stringBuilder.ToString();
        }
    }
}
