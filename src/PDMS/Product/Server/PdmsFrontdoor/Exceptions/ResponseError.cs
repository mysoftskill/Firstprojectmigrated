namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using Newtonsoft.Json;

    /// <summary>
    /// Provides wrapper around ServiceError so that it is returned with the proper format.
    /// </summary>
    public class ResponseError
    {
        /// <summary>
        /// Gets or sets the service error.
        /// </summary>
        [JsonProperty(PropertyName = "error")]
        public ServiceError Error { get; set; }
    }
}