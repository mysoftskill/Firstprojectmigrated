// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security
{
    using System;
    using System.Linq;
    using System.Security.Authentication;
    using System.Web.Http;
    using System.Web.Http.Controllers;
    using System.Web.Http.Filters;

    using Microsoft.Membership.MemberServices.Common;

    /// <summary>
    ///     PrivacyExperience Identity authorization Attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class PrivacyExperienceIdentityAuthorizationAttribute : AuthorizationFilterAttribute
    {
        private readonly Type[] allowedIdentityTypes;

        /// <summary>
        ///     Initializes a new <see cref="PrivacyExperienceIdentityAuthorizationAttribute" />
        /// </summary>
        /// <param name="allowedIdentityTypes">The types of identities allowed</param>
        public PrivacyExperienceIdentityAuthorizationAttribute(params Type[] allowedIdentityTypes)
        {
            this.allowedIdentityTypes = allowedIdentityTypes;
        }

        /// <summary>
        ///     Calls when a process requests authorization.
        /// </summary>
        /// <param name="actionContext">The action context, which encapsulates information for using <see cref="T:System.Web.Http.Filters.AuthorizationFilterAttribute" />.</param>
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (!(actionContext.ControllerContext.Controller is ApiController controller))
            {
                throw new AuthenticationException($"{nameof(PrivacyExperienceIdentityAuthorizationAttribute)} can only be used with the {nameof(ApiController)}.");
            }

            if (controller.User?.Identity == null || !controller.User.Identity.IsAuthenticated)
            {
                throw new AuthenticationException("User should be authenticated by this point.");
            }

            if (this.allowedIdentityTypes.Any(t => t == controller.User.Identity.GetType()))
                return;

            throw new AuthenticationException($"User identity is incorrect type: ${controller.User.Identity.GetType()}");
        }
    }
}
