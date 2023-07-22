// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyMockService
{
    using System.Web.Http;
    using System.Web.Http.ExceptionHandling;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.PrivacyMockService.Security;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Windows.Services.AuthN.Server;

    public class Startup
    {
        internal static HttpConfiguration CreateHttpConfiguration()
        {
            HttpConfiguration config = new HttpConfiguration();
            config.Filters.Add(new UnhandledExceptionFilterAttribute(IfxTraceLogger.Instance));
            
            // Use Web API 2 Attribute Routing for specifying API paths in each controller
            config.MapHttpAttributeRoutes();

            // Register a global exception handler
            config.Services.Replace(typeof(IExceptionHandler), new UnhandledExceptionHandler());

            // Add Authentication filter
            config.Filters.Add(new AuthenticationFilter(new RpsAuthServer()));

            // PDP specific config settings. These are mocking what PDP is doing.
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;


            return config;
        }
    }
}