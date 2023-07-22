namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication
{
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using global::Autofac;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Graph;
    using Microsoft.Identity.Client;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.FileSystem.KeyVault;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;

    /// <summary>
    /// Registers the dependencies for this component.
    /// </summary>
    public class AuthenticationModule : Module
    {
        /// <summary>
        /// Registers dependencies for this component with <c>Autofac</c>.
        /// </summary>
        /// <param name="builder">The container builder to which it should add the registrations.</param>
        protected override void Load(ContainerBuilder builder)
        {
            // Singletons.
            builder
                .Register(ctx =>
                {
                    var configuration = ctx.Resolve<IPrivacyConfigurationManager>();
                    var eventWriterFactory = ctx.Resolve<IEventWriterFactory>();
                    var aadConfig = configuration.AzureActiveDirectoryProviderConfig;
                    var appConfig = ctx.Resolve<IAppConfiguration>();

                    IList<X509Certificate2> certs = null;
                    if (aadConfig.TokenEncryptionEnabled)
                    {
                        var keyVaultClient = ctx.Resolve<IAzureKeyVaultReader>();
                        // Get all enabled, unexpired certs with private key, by name.
                        certs = keyVaultClient.GetCertificateVersionsAsync(aadConfig.TokenEncryptionKeyName).GetAwaiter().GetResult();
                    }

                    return new AzureActiveDirectoryProviderBuilder(aadConfig, eventWriterFactory, certs).WithAppConfiguration(appConfig).Build();
                })
                .As<IAuthenticationProvider>()
                .SingleInstance();

            builder
                .Register(ctx =>
                {
                    var configuration = ctx.Resolve<IPrivacyConfigurationManager>();
                    var azureKeyVaultReader = ctx.Resolve<IAzureKeyVaultReader>();
                    var eventWriterFactory = ctx.Resolve<IEventWriterFactory>();
                    var appConfig = ctx.Resolve<IAppConfiguration>();

                    return new TokenProvider(configuration.TokenProviderConfig, azureKeyVaultReader, eventWriterFactory, appConfig);
                })
                .As<ITokenProvider>()
                .SingleInstance();

            builder
                .Register(ctx =>
                {
                    var configuration = ctx.Resolve<IPrivacyConfigurationManager>();
                    var tokenProvider = ctx.Resolve<ITokenProvider>();
                    return new GraphServiceClientFactory(configuration.TokenProviderConfig, tokenProvider);
                })
                .As<IGraphServiceClientFactory>()
                .SingleInstance();

            // Per request types.
            builder
                .Register(ctx =>
                {
                    var configuration = ctx.Resolve<IPrivacyConfigurationManager>();
                    var graphServiceClientFactory = ctx.Resolve<IGraphServiceClientFactory>();
                    var sessionFactory = ctx.Resolve<ISessionFactory>();

                    return new ActiveDirectory(graphServiceClientFactory, sessionFactory, configuration.AzureActiveDirectoryProviderConfig);
                })
                .InstancePerLifetimeScope()
                .PreserveExistingDefaults();

            builder.RegisterType<AuthenticationResultSessionWriter>().As<ISessionWriter<AuthenticationResult>>().InstancePerLifetimeScope();
            builder.RegisterType<UserTransitiveMemberOfCollectionPageSessionWriter>().As<ISessionWriter<IUserTransitiveMemberOfCollectionWithReferencesPage>>().InstancePerLifetimeScope();
            builder.RegisterType<GetByIdsCollectionPageSessionWriter>().As<ISessionWriter<IDirectoryObjectGetByIdsCollectionPage>>().InstancePerLifetimeScope();
            builder.RegisterType<MsalExceptionSessionWriter>().As<ISessionWriter<MsalException>>().InstancePerLifetimeScope();
            builder.RegisterType<ServiceExceptionSessionWriter>().As<ISessionWriter<ServiceException>>().InstancePerLifetimeScope();
            builder.RegisterType<GroupSessionWriter>().As<ISessionWriter<Group>>().InstancePerLifetimeScope();
        }
    }
}