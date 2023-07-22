namespace Microsoft.PrivacyServices.DataManagement.DataGridService
{
    using System;

    using Autofac;

    using Microsoft.DataPlatform.DataDiscoveryService.Contracts;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;

    public class DataGridModule : Module
    {
        /// <summary>
        /// Registers dependencies for this component with <c>Autofac</c>.
        /// </summary>
        /// <param name="builder">The container builder to which it should add the registrations.</param>
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DataDiscoveryClientFactory>().As<IDataDiscoveryClientFactory>().InstancePerLifetimeScope();
            builder.RegisterType<DataGridResultSessionWriter>().As<ISessionWriter<Tuple<SearchResponse, string, string>>>().InstancePerLifetimeScope();
            builder.RegisterType<DataAssetProvider>().As<IDataAssetProvider>().InstancePerLifetimeScope();

            builder.RegisterInstance(Identity.Metadata.Manifest.Current);

            builder
                .Register(ctx =>
                {
                    var configuration = ctx.Resolve<IPrivacyConfigurationManager>();

                    return configuration.DataGridConfiguration;
                })
                .As<IDataGridConfiguration>()
                .SingleInstance();
        }
    }
}
