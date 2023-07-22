namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Owin
{
    using System;

    using global::Autofac;
    using global::Autofac.Integration.WebApi;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;

    /// <summary>
    /// Registers dependencies for this component with <c>Autofac</c>.
    /// </summary>
    public class OwinModule : Module
    {
        /// <summary>
        /// Registers the minimum set of OWIN infrastructure.
        /// Useful for test code to avoid registering unnecessary dependencies.
        /// </summary>
        /// <param name="builder">The container builder to which it should add the registrations.</param>
        public static void RegisterOwin(ContainerBuilder builder)
        {
            // Register your Web API controllers for dependency injection.      
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            builder.RegisterApiControllers(assemblies);
        }

        /// <summary>
        /// Registers dependencies for this component with <c>Autofac</c>.
        /// </summary>
        /// <param name="builder">The container builder to which it should add the registrations.</param>
        protected override void Load(ContainerBuilder builder)
        {
            RegisterOwin(builder);

            builder
                .Register(ctx =>
                {
                    var configuration = ctx.Resolve<IPrivacyConfigurationManager>();

                    return configuration.OwinConfiguration;
                })
                .As<IOwinConfiguration>()
                .SingleInstance();

            // Register custom middle ware.
            builder.RegisterType<LogMiddleWare>().InstancePerLifetimeScope();
            builder.RegisterType<RequestUrlValidationMiddleWare>().SingleInstance();
            builder.RegisterType<HstsMiddleWare>().SingleInstance();
        }
    }
}
