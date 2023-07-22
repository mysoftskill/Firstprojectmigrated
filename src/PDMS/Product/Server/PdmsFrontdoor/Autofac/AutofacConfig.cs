namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Autofac
{
    using System.IO;
    using System.Reflection;

    using global::Autofac;

    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Autofac;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Owin;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi;

    /// <summary>
    /// Registers all <c>Autofac</c> modules.
    /// </summary>
    public static class AutofacConfig
    {
        /// <summary>
        /// Registers the <c>Autofac</c> modules and seals the container.
        /// </summary>
        /// <param name="executionPath">The location where the code is running.</param>
        /// <returns>The <c>Autofac</c> container.</returns>
        public static ContainerBuilder RegisterComponents(string executionPath = null)
        {
            executionPath = executionPath ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterAutoMapper();
            containerBuilder.RegisterAppConfiguration();

            containerBuilder.RegisterModule(new InstrumentationModule());
            containerBuilder.RegisterModule(new FileSystemModule());
            containerBuilder.RegisterModule(new WebApiModule<OwinRequestContextFactory>());
            containerBuilder.RegisterModule(new AuthenticationModule());
            containerBuilder.RegisterModule(new DataAccessModule());

            containerBuilder.RegisterModule(new RegistrationModule(executionPath));

            containerBuilder.RegisterModule(new OwinModule());// Must be the last registration.
            
            containerBuilder
                .Register(ctx =>
                {
                    var configuration = ctx.Resolve<IPrivacyConfigurationManager>();

                    return new ServicePointManagerInitializer(configuration.ServicePointManagerConfig);
                })
                .As<IInitializer>()
                .SingleInstance();

            return containerBuilder;
        }
    }
}
 