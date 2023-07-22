// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Filters
{
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Configuration;
    using System.Net.Http;
    using System.Web.Http;
    using System.Web.Http.Controllers;
    using System.Web.Http.Dependencies;

    public class ProductionAuthorize : AuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            IDependencyScope scope = actionContext.Request.GetDependencyScope();
            var config = scope.GetService(typeof(IPrivacyConfigurationManager)) as IPrivacyConfigurationManager;
            bool isProduction = config.EnvironmentConfiguration.EnvironmentType == EnvironmentType.Prod;
            return !isProduction || base.IsAuthorized(actionContext);
        }
    }
}
