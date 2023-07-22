namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    /// <summary>
    /// Identifies a service tree service not found error.
    /// </summary>
    public class ServiceNotFoundError : InnerError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceNotFoundError" /> class.
        /// </summary>
        public ServiceNotFoundError() : base("Service")
        {
        }
    }
}