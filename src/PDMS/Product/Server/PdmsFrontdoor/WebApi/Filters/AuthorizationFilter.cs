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
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;

    /// <summary>
    /// A custom attribute used to require authentication on an API.
    /// </summary>
    public class AuthorizationFilter : BaseActionFilter
    {
        private readonly AuthenticatedPrincipal authenticatedPrincipal;
        private readonly SessionProperties sessionProperties;
        private readonly IEnumerable<IAuthenticationProvider> authenticationProviders;
        private readonly IOperationAccessProvider operationAccessProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationFilter" /> class.
        /// </summary>
        /// <param name="authenticatedPrincipal">The principal object created for this specific request.</param>
        /// <param name="sessionProperties">The session properties object created for this specific request.</param>
        /// <param name="authenticationProviders">The authentication providers that are registered in the service.</param>
        /// <param name="operationAccessProvider">The operation access provider that provides authorization AllowedList information.</param>
        public AuthorizationFilter(
            AuthenticatedPrincipal authenticatedPrincipal,
            SessionProperties sessionProperties,
            IEnumerable<IAuthenticationProvider> authenticationProviders,
            IOperationAccessProvider operationAccessProvider)
        {
            this.authenticatedPrincipal = authenticatedPrincipal;
            this.sessionProperties = sessionProperties;
            this.authenticationProviders = authenticationProviders;
            this.operationAccessProvider = operationAccessProvider;
        }

        /// <summary>
        /// Fails the call if the authenticated principal is not valid.
        /// </summary>
        /// <param name="actionContext">The action context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="continuation">The continuation for the request.</param>
        /// <returns>The result of the continuation.</returns>
        public override Task<HttpResponseMessage> ExecuteActionFilterAsync(HttpActionContext actionContext, CancellationToken cancellationToken, Func<Task<HttpResponseMessage>> continuation)
        {
            this.sessionProperties.PartnerName = this.authenticatedPrincipal.ApplicationId;
            this.sessionProperties.User = this.authenticatedPrincipal.UserId;

            if (this.authenticationProviders.Any(v => v.Enabled))
            {
                if (string.IsNullOrWhiteSpace(this.authenticatedPrincipal.ApplicationId) ||
                    this.authenticatedPrincipal.ClaimsPrincipal == null)
                {
                    throw new NotAuthenticatedError("Authentication failed: ApplicationId or ClaimsPrincipal is not specified.");
                }

                var operationAccessPermission = this.operationAccessProvider.GetAccessPermissions(this.authenticatedPrincipal.ApplicationId);
                if (operationAccessPermission != null)
                {
                    this.sessionProperties.PartnerName = operationAccessPermission.FriendlyName;

                    if (!this.HasPermission("*", operationAccessPermission) && !this.HasPermission(this.authenticatedPrincipal.OperationName, operationAccessPermission))
                    {
                        throw new ApplicationNotAuthorizedError(this.authenticatedPrincipal.ApplicationId);
                    }
                }
                else
                {
                    throw new ApplicationNotAuthorizedError(this.authenticatedPrincipal.ApplicationId);
                }
            }

            return continuation();            
        }

        /// <summary>
        /// Check whether the specified API is allowed in the OperationAccessPermission.
        /// </summary>
        /// <param name="api">The API operation name.</param>
        /// <param name="operationAccessPermission">The OperationAccessPermission to check on.</param>
        /// <returns>Check result.</returns>
        private bool HasPermission(string api, OperationAccessPermission operationAccessPermission)
        {
            var result = operationAccessPermission.AllowedOperations.SingleOrDefault<string>(operationName => operationName.Equals(api));
            return !string.IsNullOrEmpty(result);
        }
    }
}