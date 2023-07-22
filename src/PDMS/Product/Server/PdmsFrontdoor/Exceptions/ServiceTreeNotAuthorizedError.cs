namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using Newtonsoft.Json;

    /// <summary>
    /// Identifies a service tree user not authorized error.
    /// </summary>
    public class ServiceTreeNotAuthorizedError : InnerError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceTreeNotAuthorizedError" /> class.
        /// </summary>
        /// <param name="serviceId">The service id that the user fails the authorization check.</param>
        public ServiceTreeNotAuthorizedError(string serviceId) : base("ServiceTree")
        {
            this.ServiceId = serviceId;
        }

        /// <summary>
        /// Gets or sets the source property name that causes the mutual exclusiveness.
        /// </summary>
        [JsonProperty(PropertyName = "serviceId")]
        public string ServiceId { get; set; }
    }
}