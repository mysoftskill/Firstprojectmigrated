namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.ServiceModel;
    using System.ServiceModel.Security;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Autofac;
    using global::Autofac.Core;
    using global::Autofac.Extras.DynamicProxy;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Auth;
    using Microsoft.Azure.Storage.Queue;
    using Microsoft.AzureAd.Icm.Types;
    using Microsoft.AzureAd.Icm.WebService.Client;
    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Client.AAD;
    using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree;
    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.FileSystem.KeyVault;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.ActiveDirectory;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac.Sll;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.AzureQueue;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Icm;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Kusto;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Writer;
    using Microsoft.PrivacyServices.DataManagement.DataGridService;
    using Microsoft.PrivacyServices.DataManagement.Models;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    using Module = global::Autofac.Module;

    /// <summary>
    /// Registers the dependencies for this component.
    /// </summary>
    public class DataAccessModule : Module
    {
        /// <summary>
        /// Registers dependencies for this component with <c>Autofac</c>.
        /// </summary>
        /// <param name="builder">The container builder to which it should add the registrations.</param>
        protected override void Load(ContainerBuilder builder)
        {
            // Register Configs
            builder
                .Register(ctx =>
                {
                    var configuration = ctx.Resolve<IPrivacyConfigurationManager>();

                    return configuration.CoreConfiguration;
                })
                .As<ICoreConfiguration>()
                .SingleInstance();

            builder
                .Register(ctx =>
                {
                    var configuration = ctx.Resolve<IPrivacyConfigurationManager>();

                    return configuration.DataAccessConfiguration;
                })
                .As<IDataAccessConfiguration>()
                .SingleInstance();

            builder
                .Register(ctx =>
                {
                    var configuration = ctx.Resolve<IPrivacyConfigurationManager>();

                    return configuration.IcmConfiguration;
                })
                .As<IIcmConfiguration>()
                .SingleInstance();

            builder
                .Register(ctx =>
                {
                    var configuration = ctx.Resolve<IPrivacyConfigurationManager>();

                    return configuration.ServiceTreeKustoConfiguration;
                })
                .As<IServiceTreeKustoConfiguration>()
                .SingleInstance();

            builder
                .Register(ctx =>
                {
                    var configuration = ctx.Resolve<IPrivacyConfigurationManager>();

                    return configuration.DocumentDatabaseConfig;
                })
                .As<IDocumentDatabaseConfig>()
                .SingleInstance();

            builder
                .Register(ctx =>
                {
                    var configuration = ctx.Resolve<IPrivacyConfigurationManager>();

                    return configuration.KustoClientConfig;
                })
                .As<IKustoClientConfig>()
                .SingleInstance();

            builder
                .Register(ctx =>
                {
                    var configuration = ctx.Resolve<IPrivacyConfigurationManager>();

                    return configuration.ServiceTreeClientConfig;
                })
                .Named<IClientConfiguration>("ServiceTree")
                .As<IClientConfiguration>()
                .SingleInstance();

            builder
                .Register(ctx =>
                {
                    var configuration = ctx.Resolve<IPrivacyConfigurationManager>();

                    return configuration.AzureActiveDirectoryProviderConfig;
                })
                .As<IAzureActiveDirectoryProviderConfig>()
                .SingleInstance();

            builder
                .Register(ctx =>
                {
                    var configuration = ctx.Resolve<IPrivacyConfigurationManager>();

                    return configuration.CloudStorageConfig;
                })
                .As<ICloudStorageConfig>()
                .SingleInstance();

            builder
                .Register(ctx =>
                {
                    var configuration = ctx.Resolve<IPrivacyConfigurationManager>();
                    return configuration.CloudQueueConfig;
                })
                .Named<ICloudQueueConfig>("CloudQueueConfig")
                .As<ICloudQueueConfig>()
                .SingleInstance();

            builder
                .Register(ctx =>
                {
                    var configuration = ctx.Resolve<IPrivacyConfigurationManager>();

                    var storageConfig = configuration.CloudStorageConfig;

                    var accountName = storageConfig.AccountName;
                    CloudStorageAccount account;
                    if (accountName == "devstoreaccount1")
                    {
                        account = CloudStorageAccount.DevelopmentStorageAccount;
                    }
                    else
                    {
                        var blobUri = new Uri(string.Format(storageConfig.BlobUriFormat, accountName));
                        var queueUri = new Uri(string.Format(storageConfig.QueueUriFormat, accountName));
                        var tableUri = new Uri(string.Format(storageConfig.TableUriFormat, accountName));
                        var fileUri = new Uri(string.Format(storageConfig.FileUriFormat, accountName));

                        // TODO: Once we moved to Azure.Storage packages (12.x), we should also move to Azure.Identity library, and 
                        //       use new DefaultAzureCredential(), instead of AzureServiceTokenProvider(). That will also save us from the token refreshing businees
                        var azureServiceTokenProvider = new AzureServiceTokenProvider(configuration.CoreConfiguration.AzureServicesAuthConnectionString);

                        // Since we are keeping the storage account instance for long time, we need to be able to refresh the token before it expires
                        NewTokenAndFrequency tokenAndFrequency = RenewTokenAsync(storageConfig.ResourceId, azureServiceTokenProvider, CancellationToken.None).GetAwaiter().GetResult();
                        var tokenCred = new TokenCredential(
                            tokenAndFrequency.Token,
                            (tokenProvider, cancellationToken) => RenewTokenAsync(storageConfig.ResourceId, tokenProvider, cancellationToken),
                            azureServiceTokenProvider,
                            tokenAndFrequency.Frequency.Value);

                        account = new CloudStorageAccount(new StorageCredentials(tokenCred), blobUri, queueUri, tableUri, fileUri);
                    }

                    return account.CreateCloudQueueClient();
                })
                .As<CloudQueueClient>()
                .SingleInstance();

            // Register PAF queue

            builder
                .Register(ctx =>
                {
                    var configInfo = ctx.ResolveNamed<ICloudQueueConfig>("CloudQueueConfig");

                    var queueName = configInfo.PafVariantRequestsQueueName;
                    var cloudQueue = ctx.Resolve<CloudQueueClient>().GetQueueReference(queueName);
                    var sessionFactory = ctx.Resolve<ISessionFactory>();
                    return new AzureQueue.CloudQueue(cloudQueue, sessionFactory, configInfo, ctx.Resolve<IDateFactory>());
                })
                .Named<ICloudQueue>($"PafVariantRequestsQueue")
                .As<ICloudQueue>()
                .As<IInitializer>()
                .SingleInstance();

            builder
                .Register(ctx =>
                {
                    var config = ctx.ResolveNamed<IClientConfiguration>("ServiceTree");
                    var keyVaultClient = ctx.Resolve<IAzureKeyVaultReader>();
                    var cert = keyVaultClient.GetCertificateByNameAsync(config.KeyVaultCertificateName, includePrivateKey: true).GetAwaiter().GetResult();

                    bool targetProductionEnv = config.ServiceUrl == Defaults.ServiceTreeUrl;
                    string resourceId = targetProductionEnv ? Defaults.ServiceTreeResourceId : Defaults.ServiceTreePpeResourceId;
                    var factory = new ServiceAzureActiveDirectoryProviderFactory(config.ClientId, cert, targetProductionEnv, sendX5c: true);
                    factory.ResourceId = resourceId;

                    return factory;
                })
                .As<IAuthenticationProviderFactory>()
                .SingleInstance();

            builder
                .Register(ctx =>
                {
                    var config = ctx.Resolve<IClientConfiguration>();
                    var environmentUri = new Uri(config.ServiceUrl);

                    var azureKeyVaultReader = ctx.Resolve<IAzureKeyVaultReader>();
                    var cert = azureKeyVaultReader.GetCertificateByNameAsync(config.KeyVaultCertificateName, includePrivateKey: true).GetAwaiter().GetResult();

                    return new HttpServiceProxy(environmentUri, cert, defaultTimeout: TimeSpan.FromSeconds(config.TimeoutInSeconds));
                })
                .Named<IHttpServiceProxy>("ServiceTree")
                .As<IDisposable>() // Make sure it gets disposed.
                .SingleInstance();

            // Readers

            builder.RegisterType<DataOwnerReader>().As<IDataOwnerReader>().InstancePerLifetimeScope();
            builder.RegisterType<AssetGroupReader>().As<IAssetGroupReader>().InstancePerLifetimeScope();
            builder.RegisterType<VariantDefinitionReader>().As<IVariantDefinitionReader>().InstancePerLifetimeScope();
            builder.RegisterType<TransferRequestReader>().As<ITransferRequestReader>().InstancePerLifetimeScope();
            builder.RegisterType<DeleteAgentReader>().As<IDeleteAgentReader>().InstancePerLifetimeScope();
            builder.RegisterType<DataAgentReader>().As<IDataAgentReader>().InstancePerLifetimeScope();
            builder.RegisterType<HistoryItemReader>().As<IHistoryItemReader>().InstancePerLifetimeScope();
            builder.RegisterType<SharingRequestReader>().As<ISharingRequestReader>().InstancePerLifetimeScope();
            builder.RegisterType<VariantRequestReader>().As<IVariantRequestReader>().InstancePerLifetimeScope();
            builder.RegisterType<DataAssetReader>().As<IDataAssetReader>().InstancePerLifetimeScope();
            builder.RegisterType<PrivacyDataStorageReader>().As<IPrivacyDataStorageReader>().InstancePerLifetimeScope();

            builder.RegisterType<DataOwnerWriter>().As<IDataOwnerWriter>().InstancePerLifetimeScope();
            builder.RegisterType<AssetGroupWriter>().As<IAssetGroupWriter>().InstancePerLifetimeScope();
            builder.RegisterType<VariantDefinitionWriter>().As<IVariantDefinitionWriter>().InstancePerLifetimeScope();
            builder.RegisterType<DeleteAgentWriter>().As<IDeleteAgentWriter>().InstancePerLifetimeScope();
            builder.RegisterType<DataAgentWriter>().As<IDataAgentWriter>().InstancePerLifetimeScope();
            builder.RegisterType<SharingRequestWriter>().As<ISharingRequestWriter>().InstancePerLifetimeScope();
            builder.RegisterType<VariantRequestWriter>().As<IVariantRequestWriter>().InstancePerLifetimeScope();
            builder.RegisterType<TransferRequestWriter>().As<ITransferRequestWriter>().InstancePerLifetimeScope();
            builder.RegisterType<PrivacyDataStorageWriter>().As<IPrivacyDataStorageWriter>().InstancePerLifetimeScope();

            builder.RegisterType<IcmConnector>().As<IIcmConnector>().InstancePerLifetimeScope();

            // Register Modules
            builder.RegisterModule(new DocumentDbModule());
            builder.RegisterModule(new DataGridModule());

            // Note: Order matters here.  This checks to see if there is already a 
            // DatabaseInitializer registered. If not, then it registers a stored
            // procedure provider that will get passed to the DatabaseInitializer
            // registered below.  If not, then don't register a StoredProcedureProvider.
            builder.RegisterInstance<IStoredProcedureProvider>(
                    new EmbeddedResourceStoredProcedureProvider(
                        "Microsoft.PrivacyServices.DataManagement.DataAccess.StoredProcedures",
                        "Installation.xml",
                        typeof(IPrivacyDataStorageReader).Assembly)
                )
                .As<IStoredProcedureProvider>()
                .SingleInstance()
                .IfNotRegistered(typeof(DatabaseInitializer));

            // This must be a singleton because of the initialization logic.
            // Note: This registration only happens if we haven't already registered
            // a DatabaseInitializer.
            builder
                .RegisterType<DatabaseInitializer>()
                .AsSelf()
                .As<IInitializer>()
                .SingleInstance()
                .IfNotRegistered(typeof(DatabaseInitializer));

            builder.RegisterType<AuthorizationProvider>().As<IAuthorizationProvider>().InstancePerLifetimeScope();
            builder.RegisterType<ActiveDirectory>().Keyed<IActiveDirectory>("Core").InstancePerLifetimeScope();
            builder.RegisterType<ActiveDirectoryCache>().As<IActiveDirectoryCache>().InstancePerLifetimeScope();

            builder
                .RegisterType<CachedActiveDirectory>()
                .As<IActiveDirectory>()
                .As<ICachedActiveDirectory>()
                .WithParameter(
                    new ResolvedParameter(
                        (pi, ctx) => pi.ParameterType == typeof(IActiveDirectory),
                        (pi, ctx) => ctx.ResolveKeyed<IActiveDirectory>("Core")))
                .InstancePerLifetimeScope();

            builder.RegisterType<KustoResponseSessionWriter>().As<ISessionWriter<IHttpResult<KustoResponse>>>().InstancePerLifetimeScope();

            builder
               .Register(
                   ctx =>
                   {
                       var kustoConfig = ctx.Resolve<IKustoClientConfig>();
                       var azureKeyVaultReader = ctx.Resolve<IAzureKeyVaultReader>();
                       var cert = azureKeyVaultReader.GetCertificateByNameAsync(kustoConfig.KeyVaultCertificateName, includePrivateKey: true).GetAwaiter().GetResult();
                       var credsClient = new ConfidentialCredential(kustoConfig.ClientId, cert, new Uri(kustoConfig.Authority));
                       return new KustoClientInstrumented(kustoConfig, new HttpClient(), credsClient, ctx.Resolve<ISessionFactory>());
                   })
               .As<IKustoClient>()
               .SingleInstance();

            builder
               .Register(
                   ctx =>
                   {
                       var kustoConfig = ctx.Resolve<IServiceTreeKustoConfiguration>();
                       var azureKeyVaultReader = ctx.Resolve<IAzureKeyVaultReader>();
                       var cert = azureKeyVaultReader.GetCertificateByNameAsync(kustoConfig.KeyVaultCertificateName, includePrivateKey: true).GetAwaiter().GetResult();
                       var credsClient = new ConfidentialCredential(kustoConfig.ClientId, cert, new Uri(kustoConfig.Authority));
                       return new ServiceTreeKustoClient(kustoConfig, new HttpClient(), credsClient, ctx.Resolve<ISessionFactory>());
                   })
               .As<IServiceTreeKustoClient>()
               .SingleInstance();

            builder
                .Register<ConnectorIncidentManagerClient>(ctx =>
                {
                    var config = ctx.Resolve<IIcmConfiguration>();
                    var azureKeyVaultReader = ctx.Resolve<IAzureKeyVaultReader>();
                    var cert = azureKeyVaultReader.GetCertificateByNameAsync(config.KeyVaultCertificateName, includePrivateKey: true).GetAwaiter().GetResult();
                    return this.CreateIcmClient(config, cert);
                })
                .As<IConnectorIncidentManager>()
                .SingleInstance();

            // Singletons.
            builder.RegisterType<DateFactory>().As<IDateFactory>().SingleInstance();
            builder.RegisterType<Validator>().As<IValidator>().SingleInstance();
            builder.RegisterType<InternalProcessFactory>().As<IInternalProcessFactory>().SingleInstance();

            // Hook up AutoMapper configurations.
            builder.RegisterType<MappingProfile>().As<AutoMapper.Profile>().SingleInstance();

            // Hook up Policy configuration.
            builder.RegisterInstance(Microsoft.PrivacyServices.Policy.Policies.Current);

            // ServiceTree
            this.RegisterClientType<ServiceGroupClient, IServiceGroupClient>(builder);
            this.RegisterClientType<TeamGroupClient, ITeamGroupClient>(builder);
            this.RegisterClientType<ServiceClient, IServiceClient>(builder);

            builder
                .RegisterType<ServiceTreeClient>()
                .WithParameter(
                    new ResolvedParameter(
                        (pi, ctx) => pi.ParameterType == typeof(IHttpServiceProxy),
                        (pi, ctx) => ctx.ResolveNamed<IHttpServiceProxy>("ServiceTree")))
                .As<IServiceTreeClient>()
                .InstancePerLifetimeScope();

            // Use an interceptor to auto-instrument the clients.
            builder
                .RegisterType<ClientInterceptor>()
                .WithParameter("clientName", "ServiceTree")
                .InstancePerLifetimeScope();

            // Register the instrumentation classes.
            builder.RegisterType<IHttpResultSessionWriter>().As<ISessionWriter<IHttpResult>>().InstancePerLifetimeScope();
            builder.RegisterType<BaseExceptionSessionWriter>().As<ISessionWriter<BaseException>>().InstancePerLifetimeScope();
            builder.RegisterType<CloudQueueSessionWriter>().As<ISessionWriter<CloudQueueEvent>>().InstancePerLifetimeScope();
            builder.RegisterType<CloudQueueExceptionSessionWriter>().As<ISessionWriter<CloudQueueException>>().InstancePerLifetimeScope();

            builder.RegisterType<IcmResultSessionWriter>().As<ISessionWriter<Tuple<Guid, AlertSourceIncident, IncidentAddUpdateResult, string>>>().InstancePerLifetimeScope();

            // Register all the tuple combinations in use.
            // Ideally, we'd register this using RegisterGeneric, but then it overrides the EmptySessionWriter registration.
            builder.RegisterType<DataAccessResultSessionWriter<bool>> ().As<ISessionWriter<(DataAccessResult logInfo, bool result)>>().InstancePerLifetimeScope();
            builder.RegisterType<DataAccessResultSessionWriter<DataAgent>>().As<ISessionWriter<(DataAccessResult, DataAgent)>>().InstancePerLifetimeScope();
            builder.RegisterType<DataAccessResultSessionWriter<List<DataAgent>>>().As<ISessionWriter<(DataAccessResult, List<DataAgent>)>>().InstancePerLifetimeScope();
            builder.RegisterType<DataAccessResultSessionWriter<FilterResult<DataAgent>>>().As<ISessionWriter<(DataAccessResult, FilterResult<DataAgent>)>>().InstancePerLifetimeScope();
            builder.RegisterType<DataAccessResultSessionWriter<FilterResult<DataAsset>>>().As<ISessionWriter<(DataAccessResult, FilterResult<DataAsset>)>>().InstancePerLifetimeScope();
        }

        private ConnectorIncidentManagerClient CreateIcmClient(IIcmConfiguration configuration, X509Certificate2 clientCertificate)
        {
            var binding = new WS2007HttpBinding(SecurityMode.Transport)
            {
                Name = "IcmBindingConfigCert",
                MaxBufferPoolSize = 4194304,
                MaxReceivedMessageSize = 16777216
            };

            binding.Security.Transport.Realm = string.Empty;
            binding.Security.Transport.ProxyCredentialType = HttpProxyCredentialType.None;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
            binding.ReaderQuotas.MaxArrayLength = 16384;
            binding.ReaderQuotas.MaxBytesPerRead = 1048576;
            binding.ReaderQuotas.MaxStringContentLength = 1048576;
            binding.Security.Message.EstablishSecurityContext = false;
            binding.Security.Message.NegotiateServiceCredential = true;
            binding.Security.Message.AlgorithmSuite = SecurityAlgorithmSuite.Default;
            binding.Security.Message.ClientCredentialType = MessageCredentialType.Certificate;

            var remoteAddress = new EndpointAddress(configuration.ServiceUrl);

            var client = new ConnectorIncidentManagerClient(binding, remoteAddress);
            client.ClientCredentials.ClientCertificate.Certificate = clientCertificate;

            return client;
        }

        private void RegisterClientType<T, I>(ContainerBuilder builder)
        {
            builder
                .RegisterType<T>()
                .FindConstructorsWith(new BindingFlagsConstructorFinder(BindingFlags.NonPublic))
                .Named<I>("ServiceClient")
                .WithParameter(
                    new ResolvedParameter(
                        (pi, ctx) => pi.ParameterType == typeof(IHttpServiceProxy),
                        (pi, ctx) => ctx.ResolveNamed<IHttpServiceProxy>("ServiceTree")))
                .SingleInstance();

            builder
                .Register(ctx =>
                {
                    return ctx.ResolveNamed<I>("ServiceClient");
                })
                .As<I>()
                .EnableInterfaceInterceptors()
                .InterceptedBy(typeof(ClientInterceptor))
                .InstancePerLifetimeScope()
                .OnRelease(_ => { }); // Do not call dispose, because it disposes the singleton registered above.
        }

        private static async Task<NewTokenAndFrequency> RenewTokenAsync(string resource, object tokenProvider, CancellationToken token)
        {
            AppAuthenticationResult authResult = await ((AzureServiceTokenProvider)tokenProvider).GetAuthenticationResultAsync(resource, cancellationToken: token);

            // Renew the token 5 minutes before it expires.
            TimeSpan next = authResult.ExpiresOn - DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5);
            if (next.Ticks <= 0)
            {
                next = default;
            }

            // Return the new token and the next refresh time.
            return new NewTokenAndFrequency(authResult.AccessToken, next);
        }
    }
}
