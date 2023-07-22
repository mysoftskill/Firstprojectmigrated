// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.UnitTests.Security
{
    using System.Net.Http;
    using System.Web.Http;
    using System.Web.Http.Controllers;
    using System.Web.Http.Hosting;
    using System.Web.Http.Routing;

    using Moq;

    public abstract class AuthorizationAttributeTestBase
    {
        protected static HttpActionContext CreateHttpActionContext<T>(T controller) where T : ApiController
        {
            HttpConfiguration config = new HttpConfiguration();
            IHttpRouteData route = new HttpRouteData(new HttpRoute());
            HttpRequestMessage request = new HttpRequestMessage();
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
            request.Properties[HttpPropertyKeys.HttpRouteDataKey] = route;

            HttpControllerContext controllerContext = new HttpControllerContext(config, route, request)
            {
                ControllerDescriptor = new HttpControllerDescriptor(config, "PrivacyController", typeof(T)),
                Controller = controller
            };

            HttpActionDescriptor descriptor = new Mock<HttpActionDescriptor> { CallBase = true }.Object;
            HttpActionContext actionContext = new HttpActionContext(controllerContext, descriptor);

            actionContext.ControllerContext.Request = request;
            return actionContext;
        }
    }
}
