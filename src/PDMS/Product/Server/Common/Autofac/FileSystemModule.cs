namespace Microsoft.PrivacyServices.DataManagement.Common.Autofac
{
    using global::Autofac;

    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.FileSystem;
    using Microsoft.PrivacyServices.DataManagement.Common.FileSystem.KeyVault;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;

    /// <summary>
    /// Registers dependencies for File System with <c>Autofac</c>.
    /// </summary>
    public class FileSystemModule : Module
    {
        /// <summary>
        /// Registers dependencies for File System with <c>Autofac</c>.
        /// </summary>
        /// <param name="builder">The container builder to which it should add the registrations.</param>
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .Register(ctx =>
                {
                    var eventWriterFactory = ctx.Resolve<IEventWriterFactory>();
                    var configuration = ctx.Resolve<IPrivacyConfigurationManager>();

                    var azureKeyVaultReader = new AzureKeyVaultReader(configuration, eventWriterFactory);

                    return azureKeyVaultReader;
                })
                .As<IAzureKeyVaultReader>()
                .SingleInstance();

            builder.RegisterType<FileSystem>().As<IFileSystem>().InstancePerLifetimeScope();
            builder.RegisterType<ProcessLauncher>().As<IProcessLauncher>();
        }
    }
}
