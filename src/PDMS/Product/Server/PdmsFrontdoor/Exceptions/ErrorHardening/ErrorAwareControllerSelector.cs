namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions.ErrorHardening
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Web.Http;
    using System.Web.Http.Controllers;
    using System.Web.Http.Dispatcher;

    using Microsoft.AspNet.OData.Extensions;

    /// <summary>
    /// A controller selector that catches any routing errors and converts them into handled errors.
    /// </summary>
    public class ErrorAwareControllerSelector : DefaultHttpControllerSelector
    {
        private readonly HashSet<string> controllerNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorAwareControllerSelector" /> class.
        /// </summary>
        /// <param name="configuration">The http configuration data.</param>
        /// <param name="controllerTypes">The set of controller types.</param>
        public ErrorAwareControllerSelector(HttpConfiguration configuration, IEnumerable<Type> controllerTypes = null)
            : base(configuration)
        {
            this.controllerNames =
                controllerTypes == null ? new HashSet<string>() : new HashSet<string>(controllerTypes.Select(c => c.Name.ToLowerInvariant()));
        }

        /// <summary>
        /// Wraps standard controller selection with an error handler.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>A controller.</returns>
        public override HttpControllerDescriptor SelectController(HttpRequestMessage request)
        {
            try
            {
                return base.SelectController(request);
            }
            catch (Exception ex)
            {
                throw new InvalidRequestError(ex.Message);
            }
        }

        /// <summary>
        /// Gets the versioned controller name.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The controller name.</returns>
        public override string GetControllerName(HttpRequestMessage request)
        {
            // This is the controller name with the Controller suffix removed.
            // Example: TestController -> Test
            var controllerName = base.GetControllerName(request);

            // This is the version pulled out of the request url.
            // This requires the route name to be registered as name_Vx
            // where _Vx is the version number corresponding to the versioned controller.
            var routeVersion = this.GetRouteVersion(request);

            var className = controllerName + routeVersion;
            var lookupName = (className + "Controller").ToLowerInvariant();

            if (this.controllerNames.Contains(lookupName))
            {
                return className;
            }
            else
            {
                return controllerName;
            }
        }

        private string GetRouteVersion(HttpRequestMessage request)
        {
            var routeName = request.ODataProperties().RouteName;

            var routeNameParts = routeName.Split('_');

            if (routeNameParts.Length == 2)
            {
                return routeNameParts[1];
            }
            else
            {
                return string.Empty;
            }
        }
    }
}