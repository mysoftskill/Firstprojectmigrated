// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker
{
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Common.Logging.LoggingFilter;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.SecretStore;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.CosmosHelpers;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Aqs;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.AzureQueueProcessor;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Cosmos;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.EventProcessors;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Host;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.LeaseId;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.Core.VerificationTokenValidation;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts;
    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Net;

    internal class DependencyManager : IDisposable, IDependencyManager
    {
        /// <summary>
        ///     Registration name for the AccountDeleteWriter for PCF
        /// </summary>
        private const string AccountDeleteWriterToPcf = "AccountDeleteWriterToPcf";

        /// <summary>
        ///     Registration name for the AccountDeleteWriter to Queue
        /// </summary>
        private const string AccountDeleteWriterToQueue = "AccountDeleteWriterToQueue";

        /// <summary>
        ///     Registration name for the CDP worker
        /// </summary>
        /// <remarks>This is internal because it's referenced in <see cref="AqsDequeuerDecorator" /></remarks>
        internal const string CdpWorker = "CdpWorker";

        /// <summary>
        ///     Registration name for the Msa-Account-Delete-Queue-Processor-Worker
        /// </summary>
        /// <remarks>This is internal because it's referenced in <see cref="AqsDequeuerDecorator" /></remarks>
        internal const string MsaAccountDeleteQueueProcessorWorker = "MsaAccountDeleteQueueProcessorWorker";

        private readonly ILogger logger;

        private readonly object syncLock = new object();

        private IUnityContainer activeContainer;

        private IUnityContainer container;

        private bool disposed;

        /// <summary>
        ///     Gets the container.
        /// </summary>
        public IUnityContainer Container => this.activeContainer;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DependencyManager" /> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="configuration">The configuration.</param>
        public DependencyManager(IUnityContainer container, IPrivacyConfigurationLoader configuration)
        {
            this.container = container;
            this.logger = this.container.Resolve<ILogger>();
            this.RegisterGlobalSingletons();
            this.SetupServicePointConfig(configuration.CurrentConfiguration);
            this.RegisterConfigurationDependentSingletons(configuration.CurrentConfiguration);
            this.SetupConfigurationDependentStorage(configuration.CurrentConfiguration.AqsWorkerConfiguration);
            this.InitializeActiveContainer(configuration.CurrentConfiguration);

            // Register to handle the new config file loaded event.
            configuration.ConfigurationUpdate +=
                (o, args) => this.UpdateActiveContainer(args.ConfigurationManager);
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public object GetService(Type serviceType)
        {
            return this.activeContainer.Resolve(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return this.activeContainer.ResolveAll(serviceType);
        }

        /// <summary>
        ///     Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                this.container?.Dispose();
                this.container = null;

                this.activeContainer?.Dispose();
                this.activeContainer = null;
            }

            this.disposed = true;
        }

        /// <summary>
        ///     Initializes the active container.
        /// </summary>
        /// <param name="currentConfiguration">The current configuration.</param>
        private void InitializeActiveContainer(IPrivacyConfigurationManager currentConfiguration)
        {
            this.UpdateActiveContainer(currentConfiguration);
        }

        private void SetupServicePointConfig(IPrivacyConfigurationManager config)
        {
            // setup vcclient based service point.
            var cosmosUri = new Uri(config.AqsWorkerConfiguration.MappingConfig.CosmosVcPath);

            ServicePoint cosmosServicePoint = ServicePointManager.FindServicePoint(cosmosUri);
            cosmosServicePoint.ConnectionLimit = config.AqsWorkerConfiguration.CosmosConnectionLimit;

            // setup vcclient based service point.
            var adlsCosmosUri = new Uri($"https://{config.AqsWorkerConfiguration.MappingConfig.CosmosAdlsAccountName}.{config.AqsWorkerConfiguration.AdlsConfiguration.AdlsAccountSuffix}{config.AqsWorkerConfiguration.MappingConfig.RootDir}");

            ServicePoint adlsCosmosServicePoint = ServicePointManager.FindServicePoint(adlsCosmosUri);
            adlsCosmosServicePoint.ConnectionLimit = config.AqsWorkerConfiguration.CosmosConnectionLimit;

        }

        /// <summary>
        ///     Registers the configuration dependent singletons.
        /// </summary>
        /// <param name="currentConfiguration"></param>
        private void RegisterConfigurationDependentSingletons(IPrivacyConfigurationManager currentConfiguration)
        {
            this.logger.Information(nameof(DependencyManager), "RegisterConfigurationDependentSingletons");
            this.container.RegisterInstance(currentConfiguration);
            JsonSerializerSettingsForWorkers.SetupJsonSerializerSettings(this.container);

            this.container.RegisterType<ILoggingFilter, LoggingFilter>(new HierarchicalLifetimeManager());

            this.container.RegisterType<IHttpClientFactory, HttpClientFactoryPublic>(new HierarchicalLifetimeManager());

            this.container.RegisterType<IMsaIdentityServiceClientFactory, MsaIdentityServiceClientFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IMsaIdentityServiceAdapter, MsaIdentityServiceAdapter>(new HierarchicalLifetimeManager());

            this.container.RegisterType<ISecretStoreReader, AzureKeyVaultReader>(new ContainerControlledLifetimeManager());

            // Xbox Adapter
            this.container.RegisterType<IClock, Clock>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<IXboxAcountsAdapterFactory, XboxAccountsAdapterFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IXboxAccountsAdapter, XboxAccountsAdapter>(
                new HierarchicalLifetimeManager(),
                new InjectionFactory(
                    c =>
                        c.Resolve<IXboxAcountsAdapterFactory>().Create(
                            c.Resolve<ICertificateProvider>(),
                            c.Resolve<IPrivacyConfigurationManager>(),
                            c.Resolve<ILogger>(),
                            c.Resolve<ICounterFactory>(),
                            c.Resolve<IClock>())));

            // Cosmos
            this.container.RegisterType<IAccountCreateWriter, CosmosAccountCreateWriter>(
                new HierarchicalLifetimeManager(),
                new InjectionFactory(
                    c =>
                        CosmosAccountCreateWriterFactory.Create(
                            c.Resolve<IPrivacyConfigurationManager>(),
                            c.Resolve<ICosmosClient>(),
                            c.Resolve<ILogger>(),
                            c.Resolve<IDistributedIdFactory>())));

            // Verification Token Validation service
            this.container.RegisterType<IValidationServiceFactory, ValidationServiceFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IVerificationTokenValidationService, VerificationTokenValidationService>(new HierarchicalLifetimeManager());

            // Pcf Adapter
            this.container.RegisterType<IPcfAdapter, PcfAdapter>(new HierarchicalLifetimeManager());

            // Delete writer
            this.container.RegisterType<IAccountDeleteWriter, AccountDeleteWriter>(AccountDeleteWriterToPcf, new HierarchicalLifetimeManager());
            this.container.RegisterType<IAccountDeleteWriter, AccountDeleteQueueWriter>(AccountDeleteWriterToQueue, new HierarchicalLifetimeManager());

            this.container.RegisterType<IUserCreateEventProcessorFactory, UserCreateEventProcessorFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IUserDeleteEventProcessorFactory, UserDeleteEventProcessorFactory>(new HierarchicalLifetimeManager());

            // Aqs Client Factory
            this.container.RegisterType<IAsyncQueueService2ClientFactory, AsyncQueueService2ClientInstrumentedFactory>(new HierarchicalLifetimeManager());

            // CdpEvent2Helper
            this.container.RegisterType<CdpEvent2Helper>(new HierarchicalLifetimeManager());

            // CDPEvent Handlers
            this.container.RegisterType<ICdpEventQueueProcessorFactory, CdpEventQueueProcessorFactory>(new HierarchicalLifetimeManager());

            // AQS Queue Processor
            this.container.RegisterType<IWorker, CdpEventQueueProcessorCollection>(
                CdpWorker,
                new HierarchicalLifetimeManager(),
                new InjectionFactory(
                    c => new CdpEventQueueProcessorCollection(
                        c.Resolve<IPrivacyConfigurationManager>(),
                        c.Resolve<IAsyncQueueService2ClientFactory>(),
                        c.Resolve<ICdpEventQueueProcessorFactory>(),
                        c.Resolve<ILogger>(),
                        c.Resolve<IUserCreateEventProcessorFactory>(),
                        c.Resolve<IUserDeleteEventProcessorFactory>(),
                        c.Resolve<IAccountCreateWriter>(),
                        c.Resolve<IAccountDeleteWriter>(AccountDeleteWriterToQueue),
                        c.Resolve<ICounterFactory>(),
                        c.Resolve<ITable<MsaDeadLetterStorage>>())));

            this.container.RegisterType<IAadAuthManager, AadAuthManager>(new HierarchicalLifetimeManager());

            this.container.RegisterType<IAzureStorageProvider, AzureStorageProvider>(new HierarchicalLifetimeManager());

            // MsaAccountDeleteQueueProcessorCollection is a collection of workers that processes events from azure queue and completes items after sent to pcf
            this.container.RegisterType<IWorker, MsaAccountDeleteQueueProcessorCollection>(
                MsaAccountDeleteQueueProcessorWorker,
                new HierarchicalLifetimeManager(),
                new InjectionFactory(
                    c => new MsaAccountDeleteQueueProcessorCollection(
                        c.Resolve<IPrivacyConfigurationManager>(),
                        c.Resolve<IMsaAccountDeleteQueue>(),
                        c.Resolve<IXboxAccountsAdapter>(),
                        c.Resolve<IMsaIdentityServiceAdapter>(),
                        c.Resolve<IVerificationTokenValidationService>(),
                        c.Resolve<IAccountDeleteWriter>(AccountDeleteWriterToPcf),
                        c.Resolve<ICounterFactory>(),
                        c.Resolve<ILogger>())));
        }

        /// <summary>
        ///     Registers the global singletons.
        /// </summary>
        private void RegisterGlobalSingletons()
        {
            this.logger.Information(nameof(DependencyManager), "RegisterGlobalSingletons");

            container.RegisterType<ICosmosResourceFactory, CosmosResourceFactory>(new ContainerControlledLifetimeManager());

            this.container.RegisterType<ICounterFactory, NoOpCounterFactory>(new ContainerControlledLifetimeManager());

            this.container.RegisterType<ICertificateProvider, CertificateProvider>(new ContainerControlledLifetimeManager());

            this.container.RegisterType<ITokenManager, InstrumentedAadTokenManager>(new ContainerControlledLifetimeManager(), new InjectionConstructor(new AadTokenManager()));
            this.container.RegisterType<IAadJwtSecurityTokenHandler, AadJwtSecurityTokenHandler>(new ContainerControlledLifetimeManager());
            // Mise token validation
            this.container.RegisterType<IMiseTokenValidationUtility, MiseTokenValidationUtility>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<ILogger>(),
                    new ResolvedParameter<IPrivacyConfigurationManager>()));
        }

        private void SetupConfigurationDependentStorage(IPrivacyAqsWorkerConfiguration currentConfigurationAqsWorkerConfiguration)
        {
            var storage = new AzureStorageProvider(this.logger, this.container.Resolve<ISecretStoreReader>());
            storage.InitializeAsync(currentConfigurationAqsWorkerConfiguration.AzureStorageConfiguration).GetAwaiter().GetResult();

            // App Configuration
            this.container.RegisterType<IAppConfiguration, AppConfiguration>(
                new ContainerControlledLifetimeManager(),
                new InjectionFactory(
                    c => AppConfigurationFactory.Create(c.Resolve<IPrivacyConfigurationManager>())));

            // cosmos client.
            this.container.RegisterType<ICosmosClient, CosmosClientInstrumented>(
                new HierarchicalLifetimeManager(),
                new InjectionFactory(
                    c => AqsCosmosClientFactory.CreateCosmosClient(c.Resolve<IPrivacyConfigurationManager>(),
                    c.Resolve<IAppConfiguration>(),
                    c.Resolve<ICosmosResourceFactory>(),
                    c.Resolve<ILogger>())));

            // Dead letter storage
            this.container.RegisterType<ITable<MsaDeadLetterStorage>, AzureTable<MsaDeadLetterStorage>>(
                new HierarchicalLifetimeManager(),
                new InjectionConstructor(storage, this.container.Resolve<ILogger>(), nameof(MsaDeadLetterStorage).ToLowerInvariant()));

            CloudBlobClient blobClient = storage.CreateCloudBlobClient();
            CloudBlobContainer containerRef = blobClient.GetContainerReference(currentConfigurationAqsWorkerConfiguration.LeaseContainerName);
            this.container.RegisterType<IDistributedIdFactory, DistributedIdFactory>(
                new HierarchicalLifetimeManager(),
                new InjectionConstructor(containerRef, currentConfigurationAqsWorkerConfiguration.CosmosWriterLimit));

            // Initialize MsaAccountDeleteQueue
            try
            {
                List<IAzureStorageProvider> queueStorageAccounts = new List<IAzureStorageProvider>();
                foreach (IAzureStorageConfiguration azureStorageConfiguration in currentConfigurationAqsWorkerConfiguration.MsaAccountDeleteQueueProcessorConfiguration
                    .AzureQueueStorageConfigurations)
                {
                    var storageProvider = new AzureStorageProvider(this.logger, this.container.Resolve<ISecretStoreReader>());
                    storageProvider.InitializeAsync(azureStorageConfiguration).GetAwaiter().GetResult();
                    queueStorageAccounts.Add(storageProvider);
                }

                this.container.RegisterType<IMsaAccountDeleteQueue, MsaAccountDeleteQueue>(
                    new HierarchicalLifetimeManager(),
                    new InjectionConstructor(
                        queueStorageAccounts,
                        this.logger,
                        currentConfigurationAqsWorkerConfiguration.MsaUserDeleteQueueConfiguration,
                        this.container.Resolve<ICounterFactory>()));
            }
            catch (Exception e)
            {
                this.logger.Error(nameof(DependencyManager), e, $"Error initializing {nameof(MsaAccountDeleteQueue)}");
                throw;
            }
        }

        /// <summary>
        ///     Updates the active container.
        /// </summary>
        /// <param name="newConfiguration">The new configuration.</param>
        private void UpdateActiveContainer(IPrivacyConfigurationManager newConfiguration)
        {
            this.logger.Information(nameof(DependencyManager), "UpdateActiveContainer");
            lock (this.syncLock)
            {
                IUnityContainer newContainer = this.container.CreateChildContainer();
                newContainer.RegisterInstance(newConfiguration, new HierarchicalLifetimeManager());
                this.activeContainer = newContainer;
            }
        }
    }
}
