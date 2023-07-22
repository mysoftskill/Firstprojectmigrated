namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Throttling
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;

    using WebApiThrottle;
    using WebApiThrottle.Net;

    /// <summary>
    /// Logging class for the throttle layer.
    /// </summary>
    public class ServerErrorThrottleHandler : ThrottlingHandler
    {
        private readonly IRequestContextFactory contextFactory;
        private readonly IEnumerable<IAuthenticationProvider> authenticationProviders;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerErrorThrottleHandler" /> class.
        /// </summary>
        /// <param name="contextFactory">The request context factory.</param>        
        /// <param name="authenticationProviders">The authentication providers.</param>        
        /// <param name="policy">The policy object.</param>
        /// <param name="policyRepository">The policy repository.</param>
        /// <param name="repository">The throttle repository.</param>
        /// <param name="logger">The throttle logger.</param>
        /// <param name="ipAddressParser">The IP address parser.</param>
        public ServerErrorThrottleHandler(
            IRequestContextFactory contextFactory,
            IEnumerable<IAuthenticationProvider> authenticationProviders,
            ThrottlePolicy policy, 
            IPolicyRepository policyRepository, 
            IThrottleRepository repository, 
            IThrottleLogger logger, 
            IIpAddressParser ipAddressParser = null) : base(policy, policyRepository, repository, logger, ipAddressParser)
        {
            this.contextFactory = contextFactory;
            this.QuotaExceededContent = this.CreateResponseContent;
            this.authenticationProviders = authenticationProviders;
        }

        /// <summary>
        /// Create the service response.
        /// </summary>
        /// <param name="request">The original request.</param>
        /// <param name="content">The response content.</param>
        /// <param name="responseCode">The response code.</param>
        /// <param name="retryAfter">The retry after value.</param>
        /// <returns>The response message.</returns>
        protected override Task<HttpResponseMessage> QuotaExceededResponse(HttpRequestMessage request, object content, HttpStatusCode responseCode, string retryAfter)
        {
            var serviceException = (ThrottledError)content;

            this.contextFactory.Create(request).SetServiceException(serviceException);

            return base.QuotaExceededResponse(request, serviceException.ServiceError, responseCode, retryAfter);
        }

        /// <summary>
        /// Sets the identity for the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The identity data.</returns>
        protected override RequestIdentity SetIdentity(HttpRequestMessage request)
        {
            var enabledProviders = this.authenticationProviders.Where(v => v.Enabled);

            var principal = request.GetRequestContext().Principal;

            var identity = base.SetIdentity(request);

            if (enabledProviders.Any())
            {
                foreach (var authenticationProvider in enabledProviders)
                {
                    var appId = authenticationProvider.GetApplicationId(principal);

                    if (appId != null)
                    {
                        identity.ClientKey = appId;
                    }
                }
            }

            return identity;
        }

        private object CreateResponseContent(long rateLimit, RateLimitPeriod rateLimitPeriod)
        {
            return new ThrottledError("Request limit exceeded.", rateLimit, rateLimitPeriod.ToString());
        }
    }
}