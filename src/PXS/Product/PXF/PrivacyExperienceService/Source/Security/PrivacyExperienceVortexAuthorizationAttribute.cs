// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security
{
    using System;
    using System.Security.Authentication;
    using System.Web.Http.Controllers;
    using System.Web.Http.Filters;

    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    internal sealed class PrivacyExperienceVortexAuthorizationAttribute : AuthorizationFilterAttribute
    {
        /// <summary>
        ///     Calls when a process requests authorization.
        /// </summary>
        /// <param name="actionContext">The action context, which encapsulates information for using <see cref="T:System.Web.Http.Filters.AuthorizationFilterAttribute" />.</param>
        /// <exception cref="AuthenticationException">
        ///     PrivacyExperienceVortexAuthorizationAttribute
        ///     or
        ///     User should be authenticated by this point.
        /// </exception>
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (!(actionContext.ControllerContext.Controller is VortexIngestionController controller))
            {
                throw new AuthenticationException($"{nameof(PrivacyExperienceVortexAuthorizationAttribute)} can only be used with the {nameof(VortexIngestionController)}.");
            }

            if (!controller.User.Identity.IsAuthenticated)
            {
                throw new AuthenticationException("User should be authenticated by this point.");
            }

            if (!(actionContext.RequestContext?.Principal is VortexPrincipal))
            {
                throw new AuthenticationException($"{nameof(PrivacyExperienceVortexAuthorizationAttribute)} can only be used with the {nameof(VortexPrincipal)}.");
            }
        }
    }
}
