namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Http;
    using System.Web.Http.Controllers;
    using System.Web.Http.Dependencies;
    using System.Web.Http.Dispatcher;
    using System.Web.Http.ExceptionHandling;

    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions.ErrorHardening;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Filters;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Handlers;

    using WebApiThrottle;

    /// <summary>
    /// A function to register controllers.
    /// </summary>
    /// <param name="configuration">The http configuration object.</param>
    public delegate void ApiRegistration(HttpConfiguration configuration);

    /// <summary>
    /// A module that contains helper functions for the <see cref="HttpConfiguration" /> class.
    /// </summary>
    public static class HttpConfigurationModule
    {
        /// <summary>
        /// Creates a new <see cref="HttpConfiguration" /> and registers the standard WebAPI components.
        /// </summary>
        /// <param name="dependencyResolver">The dependency resolver to set.</param>
        /// <param name="registrations">Any custom registration actions.</param>
        /// <returns>The initialized config object.</returns>
        public static HttpConfiguration Initialize(IDependencyResolver dependencyResolver, IEnumerable<ApiRegistration> registrations)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();

            // Register your Web API controllers for version support.      
            var assemblies = registrations.Select(r => r.Method.DeclaringType.Assembly);
            var controllerTypes = 
                assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.Name.EndsWith("Controller"));

            // Set Web Api filters, actions, and handlers. They execute in order of registration.
            config.Services.Replace(typeof(IExceptionHandler), new ExceptionHandlerProxy<ServiceExceptionHandler>());
            config.Services.Replace(typeof(IHttpControllerSelector), new ErrorAwareControllerSelector(config, controllerTypes));
            config.Services.Replace(typeof(IHttpActionSelector), new ErrorAwareActionSelector());
            config.Services.Replace(typeof(IActionValueBinder), new ErrorAwareActionValueBinder());
            
            config.MessageHandlers.Add(new DelegatingHandlerProxy<ThrottlingHandler>());
            
            config.Filters.Add(new ActionFilterProxy<AuthenticationFilter>());
            config.Filters.Add(new ActionFilterProxy<AuthorizationFilter>());

            // Register the probe handler as a specific api.
            // This is available at https://servicename/probe.
            config.Routes.MapHttpRoute(
                name: "Probe",
                routeTemplate: "probe",
                defaults: null,
                constraints: null,
                handler: new DelegatingHandlerProxy<ProbeHandler>());

            config.Routes.MapHttpRoute(
                name: "OpenApi",
                routeTemplate: "openapi",
                defaults: null,
                constraints: null,
                handler: new DelegatingHandlerProxy<OpenApiHandler>());

            foreach (var registration in registrations)
            {
                // Ensures the controller assembly is loaded. 
                // This is necessary if you use MapHttpRouteAttributes.
                registration.Invoke(config); 
            }

            // Route unknown urls in a controlled manner.
            config.Routes.MapHttpRoute(
                name: "Error404",            
                routeTemplate: "{*url}",            
                defaults: new { controller = "Error", action = "Handle404" });

            // Set the web api dependency resolver.
            config.DependencyResolver = dependencyResolver;

            return config;
        }
    }
}
