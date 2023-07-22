namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions.ErrorHardening
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Web.Http.Controllers;

    using Microsoft.AspNet.OData.Routing;
    using Microsoft.AspNet.OData.Routing.Conventions;

    /// <summary>
    /// Creates a routing convention that throws an error if it is invoked.
    /// This is added as the last convention in the set of routing conventions, 
    /// so that if any fail to find a route, this triggers an error.
    /// </summary>
    public class ErrorAwareODataRoutingConventions : IODataRoutingConvention
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="ErrorAwareODataRoutingConventions" /> class from being created.
        /// </summary>
        private ErrorAwareODataRoutingConventions()
        {
        }

        /// <summary>
        /// Retrieves the standard conventions wrapped with error handling.
        /// </summary>
        /// <returns>The collection of conventions.</returns>
        public static IList<IODataRoutingConvention> CreateDefault()
        {
            var conventions = ODataRoutingConventions.CreateDefault();          
            conventions.Add(new ErrorAwareODataRoutingConventions());
            return conventions;
        }

        /// <summary>
        /// Retrieves the standard conventions wrapped with error handling.
        /// </summary>
        /// <param name="routeName">The route name.</param>
        /// <param name="configuration">The configuration object.</param>
        /// <returns>The collection of conventions.</returns>
        public static IList<IODataRoutingConvention> CreateDefaultWithAttributeRouting(string routeName, System.Web.Http.HttpConfiguration configuration)
        {
            var conventions = ODataRoutingConventions.CreateDefaultWithAttributeRouting(routeName, configuration);            
            conventions.Add(new ErrorAwareODataRoutingConventions());            
            return conventions;
        }

        /// <summary>
        /// Fails the call.
        /// </summary>
        /// <param name="odataPath">The parameter is not used.</param>
        /// <param name="controllerContext">The parameter is not used.</param>
        /// <param name="actionMap">The parameter is not used.</param>
        /// <returns>The original value.</returns>
        public string SelectAction(ODataPath odataPath, HttpControllerContext controllerContext, ILookup<string, HttpActionDescriptor> actionMap)
        {
            throw new InvalidRequestError("The requested action is not allowed: " + controllerContext.Request.Method);            
        }

        /// <summary>
        /// Fails the call.
        /// </summary>
        /// <param name="odataPath">The parameter is not used.</param>
        /// <param name="request">The parameter is not used.</param>
        /// <returns>The original value.</returns>
        public string SelectController(ODataPath odataPath, HttpRequestMessage request)
        {
            throw new InvalidRequestError("This requested controller is invalid.");
        }
    }
}