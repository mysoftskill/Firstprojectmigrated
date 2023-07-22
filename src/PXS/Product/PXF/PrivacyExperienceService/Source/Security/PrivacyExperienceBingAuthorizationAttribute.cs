// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security
{
    using System;
    using System.Collections.Generic;
    using System.Security.Authentication;
    using System.Web.Http;
    using System.Web.Http.Controllers;
    using Microsoft.Membership.MemberServices.Common;

    /// <summary>
    ///     PrivacyExperience Identity Bing portal authorization Attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class PrivacyExperienceBingAuthorizationAttribute : PrivacyExperienceAuthorizationAttributeBase
    {
        private static readonly string bingPortal = Portal.Bing.ToString().ToUpper();

        /// <summary>
        ///     Calls when a process requests authorization.
        /// </summary>
        /// <param name="actionContext">The action context, which encapsulates information for using <see cref="T:System.Web.Http.Filters.AuthorizationFilterAttribute" />.</param>
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (!(actionContext.ControllerContext.Controller is ApiController controller))
            {
                throw new AuthenticationException($"{nameof(PrivacyExperienceBingAuthorizationAttribute)} can only be used with the {nameof(ApiController)}.");
            }

            if (controller.User?.Identity == null || !controller.User.Identity.IsAuthenticated)
            {
                throw new AuthenticationException("User should be authenticated by this point.");
            }

            AadIdentity aadIdentity = controller.User.Identity as AadIdentity;
            IDictionary<string, string> siteIdMap = GetPrivacyExperienceServiceConfiguration(actionContext).SiteIdToCallerName;

            if (aadIdentity == null)
            {
                throw new AuthenticationException($"User identity is of incorrect type: ${controller.User.Identity.GetType()}");
            }

            if (siteIdMap.TryGetValue(aadIdentity.ApplicationId, out string portalName) && portalName.ToUpper().StartsWith(bingPortal))
            {
                return;
            }

            throw new AuthenticationException($"Only Bing is authorized to call this API.");
        }
    }
}
