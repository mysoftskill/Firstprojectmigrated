// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Web.Http.Dependencies;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.LoggingFilter;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.SecretStore;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AnaheimIdAdapter;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.Privacy.Core.DataManagementConfig;
    using Microsoft.Membership.MemberServices.Privacy.Core.Export;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Aggregation;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.Core.ScopedDelete;
    using Microsoft.Membership.MemberServices.Privacy.Core.TestMsa;
    using Microsoft.Membership.MemberServices.Privacy.Core.Timeline;
    using Microsoft.Membership.MemberServices.Privacy.Core.UserSettings;
    using Microsoft.Membership.MemberServices.Privacy.Core.VerificationTokenValidation;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.DataManagementConfig;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Graph;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts;
    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.Windows.Services.AuthN.Server;
    using Microsoft.Membership.MemberServices.ScheduleDbClient;

    /// <summary>
    ///     DependencyManager
    /// </summary>
    public sealed class DependencyManager : IDependencyResolver, IDisposable
    {
        private const string ComponentName = nameof(DependencyManager);

        private const string VortexDeviceDeleteQueueStorageProviders = "VortexDeviceDeleteQueueStorageProviders";

        private readonly object currentSyncLock = new object();

        private readonly ILogger logger;

        private IUnityContainer activeContainer;

        private IUnityContainer container;

        private bool disposed;

        private IDependencyScope resolver;

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
            this.SetupDependencies(configuration.CurrentConfiguration);
            this.InitializeActiveContainer(configuration.CurrentConfiguration);
            this.InitializeAndRegisterExportStorageProvider();

            // resolve the PDMS config loader
            IDataManagementConfigLoader dataManagementConfigLoader = this.ResolveDataManagementConfigLoader();

            if (dataManagementConfigLoader == null)
            {
                // this is required to start up the service
                throw new InvalidOperationException($"Failed to resolve instance of type {nameof(IDataManagementConfigLoader)}.");
            }

            // handle events from PDMS loading a new configuration
            dataManagementConfigLoader.DataManagementConfigurationUpdate += (o, args) => this.ConfigurationUpdate(o, args, configuration.CurrentConfiguration);

            // handle events if the service config file loads a new configuration
            configuration.ConfigurationUpdate += (o, args) => this.ConfigurationUpdate(o, args, dataManagementConfigLoader.CurrentDataManagementConfig);

            // load the data management config. Must be done after the event 'DataManagementConfigurationUpdate' is registered to prevent race condition of event firing before it's registered.
            dataManagementConfigLoader.Load();
        }

        /// <summary>
        ///     Begins the scope.
        /// </summary>
        public IDependencyScope BeginScope()
        {
            return this.resolver;
        }

        /// <summary>
        ///     Implement <see cref="IDisposable" />; dispose resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Gets the service.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        public object GetService(Type serviceType)
        {
            return this.resolver.GetService(serviceType);
        }

        /// <summary>
        ///     Gets the services.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        public IEnumerable<object> GetServices(Type serviceType)
        {
            return this.resolver.GetServices(serviceType);
        }

        private void ConfigurationUpdate(object o, DataManagementConfigUpdateEventArgs args, IPrivacyConfigurationManager currentConfiguration)
        {
            this.UpdateActiveContainer(args.Config, currentConfiguration);
        }

        private void ConfigurationUpdate(object o, PrivacyConfigurationUpdateEventArgs args, IDataManagementConfig dataManagementConfiguration)
        {
            this.UpdateActiveContainer(dataManagementConfiguration, args.ConfigurationManager);
        }

        /// <summary>
        ///     Dispose underlying AP resources.
        /// </summary>
        /// <param name="disposing">true if called from <see cref="System.IDisposable" />; otherwise false.</param>
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

                this.resolver?.Dispose();
                this.resolver = null;

                this.activeContainer?.Dispose();
                this.activeContainer = null;
            }

            this.disposed = true;
        }

        private void InitializeActiveContainer(IPrivacyConfigurationManager privacyServiceConfiguration)
        {
            this.UpdateActiveContainer(null, privacyServiceConfiguration);
        }

        private void InitializeAndRegisterExportStorageProvider()
        {
            try
            {
                var exportStorageProvider = (ExportStorageProvider)this.resolver.GetService(typeof(ExportStorageProvider));
                exportStorageProvider.InitializeAsync(
                    ((IPrivacyConfigurationManager)this.resolver.GetService(typeof(IPrivacyConfigurationManager))).PrivacyExperienceServiceConfiguration).GetAwaiter().GetResult();
                this.container.RegisterInstance<IExportStorageProvider>(exportStorageProvider, new ContainerControlledLifetimeManager());
                this.logger.Information(ComponentName, "completed register export storage provider");
            }
            catch (Exception ex)
            {
                this.logger.Error(ComponentName, ex, "Failed to initialize export storage provider");
            }
        }

        private void RegisterConfigurationDependentSingletons(IPrivacyConfigurationManager privacyServiceConfiguration)
        {
            this.container.RegisterInstance(privacyServiceConfiguration);
            this.container.RegisterType<ISecretStoreReader, AzureKeyVaultReader>(new HierarchicalLifetimeManager());

            this.container.RegisterType<ILoggingFilter, LoggingFilter>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IHttpClientFactory, HttpClientFactoryPublic>(new HierarchicalLifetimeManager());

            this.container.RegisterType<IPxfAdapterFactory, PxfAdapterFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IPxfDispatcher, PxfDispatcher>(new HierarchicalLifetimeManager());

            this.container.RegisterType<ICustomerMasterAdapterFactory, CustomerMasterAdapterFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<ICustomerMasterAdapter, CustomerMasterAdapter>(
                new HierarchicalLifetimeManager(),
                new InjectionFactory(
                    c =>
                        c.Resolve<ICustomerMasterAdapterFactory>()
                            .Create(
                                c.Resolve<ICertificateProvider>(),
                                c.Resolve<IPrivacyConfigurationManager>(),
                                c.Resolve<ILogger>(),
                                c.Resolve<ICounterFactory>())));

            this.container.RegisterType<IXboxAcountsAdapterFactory, XboxAccountsAdapterFactory>(
                new HierarchicalLifetimeManager());
            this.container.RegisterType<IXboxAccountsAdapter, XboxAccountsAdapter>(
                new HierarchicalLifetimeManager(),
                new InjectionFactory(
                    c =>
                        c.Resolve<IXboxAcountsAdapterFactory>()
                            .Create(
                                c.Resolve<ICertificateProvider>(),
                                c.Resolve<IPrivacyConfigurationManager>(),
                                c.Resolve<ILogger>(),
                                c.Resolve<ICounterFactory>(),
                                c.Resolve<IClock>())));

            this.container.RegisterType<IDataManagementConfig, DataManagementConfig>(
                new HierarchicalLifetimeManager(),
                new InjectionFactory(c => c.Resolve<IDataManagementConfigLoader>().CurrentDataManagementConfig));

            this.container.RegisterType<IAadRequestVerficationServiceAdapterFactory, AadRequestVerificationServiceAdapterFactory>(
                new HierarchicalLifetimeManager());
            this.container.RegisterType<IAadRequestVerificationServiceAdapter, AadRequestVerificationServiceAdapter>(
                new HierarchicalLifetimeManager(),
                new InjectionFactory(
                    c => c.Resolve<IAadRequestVerficationServiceAdapterFactory>().Create()));

            this.container.RegisterType<ITokenManager, InstrumentedAadTokenManager>(new ContainerControlledLifetimeManager(), new InjectionConstructor(new AadTokenManager()));
            this.container.RegisterType<IAadTokenProvider, AadTokenProvider>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IAadAuthManager, AadAuthManager>(new HierarchicalLifetimeManager());
            
            this.container.RegisterType<IMsaIdentityServiceClientFactory, MsaIdentityServiceClientFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IMsaIdentityServiceAdapter, MsaIdentityServiceAdapter>(new HierarchicalLifetimeManager());

            this.container.RegisterType<IAppConfiguration, AppConfiguration>(
                new ContainerControlledLifetimeManager(),
                new InjectionFactory(
                    c => AppConfigurationFactory.Create(c.Resolve<IPrivacyConfigurationManager>())));

            this.container.RegisterType<IEventHubProducer, EventHubProducer>(
                new ContainerControlledLifetimeManager(),
                new InjectionFactory(
                    c => EventHubProducerFactory.Create(
                        c.Resolve<IPrivacyConfigurationManager>(),
                        c.Resolve<IAppConfiguration>()
                        )));

            this.container.RegisterType<IAnaheimIdAdapter, EventHubAIdAdapter>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IValidationServiceFactory, ValidationServiceFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IVerificationTokenValidationService, VerificationTokenValidationService>(new HierarchicalLifetimeManager());

            this.container.RegisterType<IRequestClassifier, RequestClassifier>(new HierarchicalLifetimeManager());

            this.container.RegisterType<IRandom, ThreadSafeRandom>(new ContainerControlledLifetimeManager());

            this.container.RegisterType<IRedisClient, RedisClient>(
                new ContainerControlledLifetimeManager(),
                new InjectionFactory(
                    c => RedisClientFactory.Create(
                        c.Resolve<IPrivacyConfigurationManager>(),
                        c.Resolve<ISecretStoreReader>(),
                        "PXS.Frontdoor",
                        c.Resolve<ILogger>()
                        )));

            // Recurring deletes
            this.container.RegisterType<IScheduleDbClient, ScheduleDbCosmosClient>(new HierarchicalLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<IPrivacyConfigurationManager>(),
                    new ResolvedParameter<IAppConfiguration>(),
                    new ResolvedParameter<ILogger>()));

            this.logger.Information(ComponentName, "Configuration dependent singleton registration complete.");
        }

        private void RegisterConfigurationDependentStorage(IPrivacyConfigurationManager privacyServiceConfiguration)
        {
            this.RegisterVortexConfigurationDependentStorage(privacyServiceConfiguration.VortexDeviceDeleteWorkerConfiguration);
        }

        private void RegisterCoreServices()
        {
            this.container.RegisterType<IAccountDeleteWriter, AccountDeleteWriter>(new HierarchicalLifetimeManager());
            this.container.RegisterType<ICloudBlobFactory, CloudBlobFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IExportService, ExportService>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IGraphAdapter, GraphAdapter>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IPcfAdapter, PcfAdapter>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IPcfProxyService, PcfProxyService>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IScopedDeleteService, ScopedDeleteService>(new HierarchicalLifetimeManager());
            this.container.RegisterType<ITestMsaService, TestMsaService>(new HierarchicalLifetimeManager());
            this.container.RegisterType<ITimelineService, TimelineService>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IUserSettingsService, UserSettingsService>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IVortexEventService, VortexEventService>(new HierarchicalLifetimeManager());
        }

        private void RegisterGlobalSingletons()
        {
            // TODO: This should be replace with a counter factory for Geneva
            this.container.RegisterType<ICounterFactory, NoOpCounterFactory>(new ContainerControlledLifetimeManager());

            // RPS must be singleton as RPS.Initialize should be called only once in application scope.
            this.container.RegisterType<IRpsAuthServer, RpsAuthServer>(new ContainerControlledLifetimeManager(), new InjectionFactory(c => new RpsAuthServer()));
            this.container.RegisterType<ICertificateProvider, CertificateProvider>(new ContainerControlledLifetimeManager());

            // Register the privacy policy as a global singleton.
            this.container.RegisterInstance(Policies.Current, new ContainerControlledLifetimeManager());

            // Register DataType Classifier as a global singleton.
            this.container.RegisterInstance(new DataTypesClassifier(Policies.Current), new ContainerControlledLifetimeManager());

            // No need to recreate this when new child container is created,
            // otherwise it will end up firing off a task on each container that is polling PDMS.
            // This means modifying service config files will not recreate a DataManagementConfigLoader
            this.container.RegisterType<IDataManagementConfigLoader, DataManagementConfigLoader>(
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<IPrivacyConfigurationManager>(),
                    new ResolvedParameter<ILogger>()));

            this.container.RegisterType<IAzureStorageProvider, AzureStorageProvider>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<IExportStorageProvider, ExportStorageProvider>(new ContainerControlledLifetimeManager());

            this.container.RegisterType<IClock, Clock>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<IMemoryCache, MemoryCache>(new ContainerControlledLifetimeManager(), new InjectionConstructor(new MemoryCacheOptions()));

            this.container.RegisterType<ICertificateValidator>(
                new ContainerControlledLifetimeManager(),
                new InjectionFactory(
                    c => new MultiValidatorCertificateValidator(
                        new CertificateChainValidator(c.Resolve<ILogger>()),
                        new CertificateSNIValidator(
                            c.Resolve<ILogger>(),
                            c.Resolve<IPrivacyConfigurationManager>().PrivacyExperienceServiceConfiguration.VortexAllowedCertSubjects.Keys,
                            c.Resolve<IPrivacyConfigurationManager>().PrivacyExperienceServiceConfiguration.VortexAllowedCertIssuers))));

            this.container.RegisterType<ITokenManager, InstrumentedAadTokenManager>(new ContainerControlledLifetimeManager(), new InjectionConstructor(new AadTokenManager()));
            this.container.RegisterType<IAadJwtSecurityTokenHandler, AadJwtSecurityTokenHandler>(new ContainerControlledLifetimeManager());
            // Mise token validation
            this.container.RegisterType<IMiseTokenValidationUtility, MiseTokenValidationUtility>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<ILogger>(),
                    new ResolvedParameter<IPrivacyConfigurationManager>()));

            this.container.RegisterType<IMachineIdRetriever, ServiceFabricMachineIdRetriever>(new ContainerControlledLifetimeManager());

            this.container.RegisterType<IFamilyClaimsParser, FamilyClaimsParser>(new ContainerControlledLifetimeManager());
        }

        private void RegisterVortexConfigurationDependentStorage(IVortexDeviceDeleteWorkerConfiguration config)
        {
            var secretReader = this.container.Resolve<ISecretStoreReader>();

            IList<IAzureStorageProvider> queueStorageProviders = new List<IAzureStorageProvider>();
            foreach (IAzureStorageConfiguration queueConfig in config.QueueProccessorConfig.AzureQueueStorageConfigurations)
            {
                var storage = new AzureStorageProvider(this.logger, secretReader);
                storage.InitializeAsync(queueConfig).GetAwaiter().GetResult();
                queueStorageProviders.Add(storage);
            }

            this.container.RegisterInstance(VortexDeviceDeleteQueueStorageProviders, queueStorageProviders, new HierarchicalLifetimeManager());

            this.container.RegisterType<IVortexDeviceDeleteQueueManager, VortexDeviceDeleteQueueManager>(
                new HierarchicalLifetimeManager(),
                new InjectionConstructor(
                    queueStorageProviders,
                    new ResolvedParameter<ILogger>(),
                    config.QueueProccessorConfig,
                    new RoundRobinQueueSelectionStrategyFactory<DeviceDeleteRequest>(),
                    new ResolvedParameter<ICounterFactory>()));
        }

        private IDataManagementConfigLoader ResolveDataManagementConfigLoader()
        {
            return (IDataManagementConfigLoader)this.resolver.GetService(typeof(IDataManagementConfigLoader));
        }

        private void ResolveInstancesRequiredForStartup()
        {

            this.resolver.GetService(typeof(IExportService));
            this.resolver.GetService(typeof(IGraphAdapter));
            this.resolver.GetService(typeof(IPcfAdapter));
            this.resolver.GetService(typeof(IPcfProxyService));
            this.resolver.GetService(typeof(IPxfDispatcher));
            this.resolver.GetService(typeof(IScopedDeleteService));
            this.resolver.GetService(typeof(ITimelineService));
            this.resolver.GetService(typeof(IUserSettingsService));
            this.resolver.GetService(typeof(IVerificationTokenValidationService));
            this.resolver.GetService(typeof(IVortexEventService));
        }

        private void SetupDependencies(IPrivacyConfigurationManager privacyServiceConfiguration)
        {
            this.RegisterGlobalSingletons();
            this.RegisterConfigurationDependentSingletons(privacyServiceConfiguration);
            this.RegisterConfigurationDependentStorage(privacyServiceConfiguration);
            this.RegisterCoreServices();
        }

        private void UpdateActiveContainer(IDataManagementConfig dataManagementConfiguration, IPrivacyConfigurationManager privacyServiceConfiguration)
        {
            lock (this.currentSyncLock)
            {
                IUnityContainer oldContainer = this.activeContainer;
                IUnityContainer newContainer = this.container.CreateChildContainer();

                // can be null on app start
                if (dataManagementConfiguration != null)
                {
                    newContainer.RegisterInstance(dataManagementConfiguration, new HierarchicalLifetimeManager());
                }

                newContainer.RegisterInstance(privacyServiceConfiguration, new HierarchicalLifetimeManager());
                this.activeContainer = newContainer;
                this.resolver = new UnityDependencyScope(this.activeContainer, this.container.Resolve<ILogger>());
                this.ResolveInstancesRequiredForStartup();

                int delayInSeconds = privacyServiceConfiguration.PrivacyExperienceServiceConfiguration.StaleConfigurationDisposalDelayInSeconds;
                ScheduleDisposalOfContainer(oldContainer, TimeSpan.FromSeconds(delayInSeconds));
            }
        }

        private static void ScheduleDisposalOfContainer(IUnityContainer oldContainer, TimeSpan delay)
        {
            if (oldContainer != null)
            {
                Task.Run(
                    async () =>
                    {
                        await Task.Delay(delay).ConfigureAwait(false);
                        oldContainer.Dispose();
                    });
            }
        }
    }
}
