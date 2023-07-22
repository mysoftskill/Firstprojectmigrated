namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Owin
{
    using System.Collections.Generic;
    using System.Threading;

    using global::Autofac;
    using global::Autofac.Integration.WebApi;
    using global::Owin;
    using Microsoft.Azure.ComplianceServices.Common.Owin;
    using Microsoft.Owin;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi;

    /// <summary>
    /// Registers OWIN specific components and integrates <c>Autofac</c> into the OWIN pipeline.
    /// </summary>
    public static class OwinStartup
    {
        /// <summary>
        /// Registers OWIN specific components and integrates <c>Autofac</c> into the OWIN pipeline.
        /// </summary>
        /// <param name="app">The OWIN app builder.</param>
        /// <param name="container">The <c>Autofac</c> container.</param>
        /// <param name="registrations">Any custom http configuration registrations.</param>
        public static void Register(IAppBuilder app, IContainer container, IEnumerable<ApiRegistration> registrations)
        {         
            // Configure Web API for self-host. 
            var config = HttpConfigurationModule.Initialize(new AutofacWebApiDependencyResolver(container), registrations);

            // Register a middleware to identify any dependency resolution errors.
            app.Use<DebugMiddleWare>();

            // 
            app.Use(typeof(NoSniffXContentTypeOptionsMiddleware));

            // Register the Autofac middleware FIRST.
            app.UseAutofacMiddleware(container);

            // Register any authentication providers before registering the Web Api components.
            // This ensures that authentication happens before the controllers are invoked.
            var authenticationProviders = container.Resolve<IEnumerable<IAuthenticationProvider>>();
            
            foreach (var provider in authenticationProviders)
            {
                if (provider.Enabled)
                {
                    provider.ConfigureAuth(app);                    
                }
            }
            
            // Register Web Api components last.
            app.UseAutofacWebApi(config);
            app.UseWebApi(config);

            // Ensure Autofac is disposed properly.
            app.DisposeScopeOnAppDisposing(container);

            // Register shutdown behaviors.
            var context = new OwinContext(app.Properties);
            var token = context.Get<CancellationToken>("host.OnAppDisposing");
            if (token != CancellationToken.None)
            {
                token.Register(() =>
                {
                    container.Dispose();
                });
            }
        }
    }
}
