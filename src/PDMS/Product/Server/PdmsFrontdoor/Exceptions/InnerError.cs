namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using Newtonsoft.Json;

    /// <summary>
    /// InnerError class MUST contain a Code value.
    /// It MAY contain a nested InnerError.
    /// It MAY contain any other service defined types.
    /// For this reason, the class is abstract and may be extended.
    /// </summary>
    /// <remarks>
    /// Based on the ODATA standard and Microsoft OneAPI Guidelines.
    /// <c>https://docs.oasis-open.org/odata/odata-json-format/v4.0/os/odata-json-format-v4.0-os.html#_Toc372793091</c>
    /// </remarks>
    public abstract class InnerError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InnerError" /> class.
        /// </summary>
        /// <param name="code">The more specific error code.</param>
        protected InnerError(string code)
        {
            this.Code = code;
        }

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        [JsonProperty(PropertyName = "code")]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the nested inner error. This is optional.
        /// </summary>
        [JsonProperty(PropertyName = "innererror", NullValueHandling = NullValueHandling.Ignore)]
        public InnerError NestedError { get; set; }
    }
}
