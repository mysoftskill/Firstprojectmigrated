namespace Microsoft.PrivacyServices.DataManagement.Worker
{
    using DataAccess.Authentication;

    using global::Autofac;
    using global::Autofac.Core;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Autofac;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb;
    using Microsoft.PrivacyServices.DataManagement.Worker.Autofac;

    /// <summary>
    /// Registers all <c>Autofac</c> modules.
    /// </summary>
    public static class AutofacConfig
    {
        /// <summary>
        /// Registers the <c>Autofac</c> modules and seals the container.
        /// </summary>
        /// <returns>The <c>Autofac</c> container.</returns>
        public static ContainerBuilder RegisterComponents()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterAutoMapper();
            containerBuilder.RegisterAppConfiguration();

            // Common modules used by all workers.
            containerBuilder.RegisterModule(new InstrumentationModule());
            containerBuilder.RegisterModule(new FileSystemModule());
            containerBuilder.RegisterModule(new AuthenticationModule());

            // Must register a DatabaseInitializer before calling DataAccessModule so that
            // we only get one IInitializer registered and it gets a null StoredProcedureProvider
            containerBuilder
                .RegisterType<DataAccess.Autofac.DatabaseInitializer>()
                .AsSelf()  // Note: Need this for check in DataAccessModule
                .As<IInitializer>()
                .WithParameter(
                    new ResolvedParameter(
                        (pi, ctx) => pi.ParameterType == typeof(IStoredProcedureProvider),
                        (pi, ctx) => null))
                .SingleInstance();

            containerBuilder.RegisterModule(new DataAccessModule());

            // Common components
            containerBuilder
                .Register(ctx =>
                {
                    var configuration = ctx.Resolve<IPrivacyConfigurationManager>();

                    return configuration.LockConfig;
                })
                .As<ILockConfig>()
                .SingleInstance();

            // Worker specific registrations
            containerBuilder.RegisterModule(new ChangeFeedReaderModule());
            containerBuilder.RegisterModule(new DataOwnerWorkerModule());
            containerBuilder.RegisterModule(new ServiceTreeMetadataWorkerModule());

            containerBuilder.Register(ctx =>
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
