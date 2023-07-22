// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security
{
    using System;
    using System.Net.Http;
    using System.Security.Authentication;
    using System.Security.Principal;
    using System.Web.Http;
    using System.Web.Http.Controllers;
    using System.Web.Http.Dependencies;
    using System.Web.Http.Filters;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Configuration;

    /// <inheritdoc />
    public abstract class PrivacyExperienceAuthorizationAttributeBase : AuthorizationFilterAttribute
    {
        /// <summary>
        /// Gets the <see cref="IPrivacyExperienceServiceConfiguration" /> for the given request
        /// </summary>
        /// <param name="ctx">The HTTP action context</param>
        /// <returns>The <see cref="IPrivacyExperienceServiceConfiguration" /></returns>
        protected static IPrivacyExperienceServiceConfiguration GetPrivacyExperienceServiceConfiguration(HttpActionContext ctx)
        {
            IDependencyScope scope = ctx.Request.GetDependencyScope();
            var config = scope.GetService(typeof(IPrivacyConfigurationManager)) as IPrivacyConfigurationManager;
            return config?.PrivacyExperienceServiceConfiguration;
        }

        /// <summary>
        ///     Reads the <see cref="IIdentity" /> from the <see cref="HttpActionContext" />, if it exists.
        /// </summary>
        /// <param name="actionContext">The action context.</param>
        /// <returns>The <see cref="MsaSelfIdentity" /></returns>
        protected static T ReadIdentity<T>(HttpActionContext actionContext)
            where T : class, IIdentity
        {
            if (!(actionContext.ControllerContext.Controller is ApiController apiController))
            {
                throw new InvalidCastException($"Auth filter can only be used on or within an {nameof(ApiController)}");
            }

            if (apiController.User?.Identity == null || !apiController.User.Identity.IsAuthenticated)
            {
                throw new AuthenticationException("User should be authenticated by this point.");
            }

            if (!(apiController.User.Identity is T identity))
            {
                throw new AuthenticationException($"Expected {nameof(IIdentity)} of type {nameof(T)}");
            }

            return identity;
        }
    }
}
