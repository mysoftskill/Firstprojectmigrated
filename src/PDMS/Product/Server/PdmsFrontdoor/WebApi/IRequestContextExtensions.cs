namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi
{
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions; 

    /// <summary>
    /// Extension methods for the IRequestContext interface.
    /// </summary>
    public static class IRequestContextExtensions
    {
        /// <summary>
        /// The key for get/set of the service exception.
        /// </summary>
        public const string ServiceExceptionKey = "ServiceException";
        
        /// <summary>
        /// Sets the service exception on the request context.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="value">The value to set.</param>
        public static void SetServiceException(this IRequestContext requestContext, ServiceException value)
        {
            requestContext.Set(ServiceExceptionKey, value);
        }

        /// <summary>
        /// Gets the service exception from the request context.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The stored value.</returns>
        public static ServiceException GetServiceException(this IRequestContext requestContext)
        {
            return requestContext.Get<ServiceException>(ServiceExceptionKey);
        }
    }
}