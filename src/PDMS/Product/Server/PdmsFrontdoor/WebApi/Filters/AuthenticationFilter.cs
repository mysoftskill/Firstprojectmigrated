namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http.Controllers;

    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication;

    /// <summary>
    /// A message handler that extracts authentication information and stores it on
    /// a principal that is available via dependency injection.
    /// </summary>
    public class AuthenticationFilter : BaseActionFilter
    {
        private readonly AuthenticatedPrincipal authenticatedPrincipal;
        private readonly IEnumerable<IAuthenticationProvider> authenticationProviders;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationFilter" /> class.
        /// </summary>
        /// <param name="authenticatedPrincipal">The principal object created for this specific request.</param>
        /// <param name="authenticationProviders">The authentication providers that are registered in the service.</param>
        public AuthenticationFilter(
            AuthenticatedPrincipal authenticatedPrincipal, 
            IEnumerable<IAuthenticationProvider> authenticationProviders)
        {
            this.authenticatedPrincipal = authenticatedPrincipal;
            this.authenticationProviders = authenticationProviders;
        }
        
        /// <summary>
        /// Injects the principal information into the dependency injection based principal of the current request.
        /// </summary>
        /// <param name="actionContext">The action context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="continuation">The continuation for the request.</param>
        /// <returns>The result of the continuation.</returns>
        public override Task<HttpResponseMessage> ExecuteActionFilterAsync(HttpActionContext actionContext, CancellationToken cancellationToken, Func<Task<HttpResponseMessage>> continuation)
        {
            var enabledProviders = this.authenticationProviders.Where(v => v.Enabled);

            if (enabledProviders.Any())
            {
                foreach (var authenticationProvider in enabledProviders)
                {
                    authenticationProvider.SetPrincipal(actionContext.RequestContext.Principal, this.authenticatedPrincipal);
                }
            }
            else
            {
                this.authenticatedPrincipal.ApplicationId = "Disabled";
                this.authenticatedPrincipal.ClaimsPrincipal = null;
                this.authenticatedPrincipal.UserId = "Disabled";
            }

            return continuation();
        }
    }
}