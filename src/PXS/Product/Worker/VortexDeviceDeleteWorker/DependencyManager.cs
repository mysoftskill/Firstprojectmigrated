// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Common.Logging.LoggingFilter;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.SecretStore;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.VerificationTokenValidation;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex;
    using Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.QueueProcessor;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AnaheimIdAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Newtonsoft.Json;

    internal class DependencyManager : IDisposable, IDependencyManager
    {
        /// <summary>
        ///     Registration name for the VortexDeviceDeleteQueueStorageProviders
        /// </summary>
        internal const string VortexDeviceDeleteQueueStorageProviders = "VortexDeviceDeleteQueueStorageProviders";

        /// <summary>
        ///     Registration name for the VortexDeviceDeleteQueueWorker
        /// </summary>
        internal const string VortexDeviceDeleteQueueWorker = "VortexDeviceDeleteQueueWorker";

        /// <summary>
        ///     Registration name for the AnaheimIdWorker
        /// </summary>
        internal const string AnaheimIdWorkerName = "AnaheimIdWorkerName";

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
            this.RegisterConfigurationDependentSingletons(configuration.CurrentConfiguration);
            this.SetupConfigurationDependentStorage(
                configuration.CurrentConfiguration.VortexDeviceDeleteWorkerConfiguration);
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

        /// <summary>
        ///     Registers the configuration dependent singletons.
        /// </summary>
        /// <param name="currentConfiguration"></param>
        private void RegisterConfigurationDependentSingletons(IPrivacyConfigurationManager currentConfiguration)
        {
            this.logger.Information(nameof(DependencyManager), "RegisterConfigurationDependentSingletons");
            this.container.RegisterInstance(currentConfiguration, new HierarchicalLifetimeManager());
            JsonSerializerSettingsForWorkers.SetupJsonSerializerSettings(this.container);

            this.container.RegisterType<ILoggingFilter, LoggingFilter>(new HierarchicalLifetimeManager());

            this.container.RegisterType<IHttpClientFactory, HttpClientFactoryPublic>(new HierarchicalLifetimeManager());

            this.container.RegisterType<ISecretStoreReader, AzureKeyVaultReader>(new HierarchicalLifetimeManager());

            // Verification Token Validation service
            this.container.RegisterType<IValidationServiceFactory, ValidationServiceFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IVerificationTokenValidationService, VerificationTokenValidationService>(new HierarchicalLifetimeManager());

            this.container.RegisterType<IAadAuthManager, AadAuthManager>(new HierarchicalLifetimeManager());

            // AAD Queue Processor
            this.container.RegisterType<IAzureStorageProvider, AzureStorageProvider>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IQueue<DeviceDeleteRequest>>(
                new ContainerControlledLifetimeManager(),
                new InjectionFactory(
                    c => new AzureQueue<DeviceDeleteRequest>(c.Resolve<IAzureStorageProvider>(), c.Resolve<ILogger>(), nameof(DeviceDeleteRequest))));
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
            this.container.RegisterType<IVortexEventService, VortexEventService>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IVortexDeviceDeleteQueueProcessorFactory, VortexDeviceDeleteQueueProcessorFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IWorker, VortexDeviceDeleteQueueProcessorCollection>(VortexDeviceDeleteQueueWorker, new HierarchicalLifetimeManager());

            this.container.RegisterType<IPcfAdapter, PcfAdapter>(new HierarchicalLifetimeManager());

            this.container.RegisterType<IRedisClient, RedisClient>(
                new ContainerControlledLifetimeManager(),
                new InjectionFactory(
                    c => RedisClientFactory.Create(
                        c.Resolve<IPrivacyConfigurationManager>(),
                        c.Resolve<ISecretStoreReader>(),
                        "VortexDeviceDeleteWorker",
                        c.Resolve<ILogger>()
                        )));

            // Anaheim Id worker
            this.container.RegisterType<IAnaheimIdQueueFactory, AnaheimIdQueueFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IAnaheimIdQueueWorkerFactory, AnaheimIdQueueWorkerFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IWorker, AnaheimIdQueueWorkersCollection>(AnaheimIdWorkerName, new HierarchicalLifetimeManager());
        }

        /// <summary>
        ///     Registers the global singletons.
        /// </summary>
        private void RegisterGlobalSingletons()
        {
            this.logger.Information(nameof(DependencyManager), "RegisterGlobalSingletons");
            this.container.RegisterType<ICounterFactory, NoOpCounterFactory>(new ContainerControlledLifetimeManager());
            this.container.RegisterInstance(Policies.Current, new ContainerControlledLifetimeManager());

            this.container.RegisterType<ICertificateProvider, CertificateProvider>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<IClock, Clock>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<ITokenManager, InstrumentedAadTokenManager>(new ContainerControlledLifetimeManager(), new InjectionConstructor(new AadTokenManager()));
            this.container.RegisterType<IAadJwtSecurityTokenHandler, AadJwtSecurityTokenHandler>(new ContainerControlledLifetimeManager());
            // Mise token validation
            this.container.RegisterType<IMiseTokenValidationUtility, MiseTokenValidationUtility>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<ILogger>(),
                    new ResolvedParameter<IPrivacyConfigurationManager>()));
        }

        /// <summary>
        ///     Setups the configuration dependent storage provider
        /// </summary>
        /// <param name="config">configuration</param>
        private void SetupConfigurationDependentStorage(
            IVortexDeviceDeleteWorkerConfiguration config)
        {
            ISecretStoreReader secretReader = this.container.Resolve<ISecretStoreReader>();

            IList<IAzureStorageProvider> queueStorageProviders = new List<IAzureStorageProvider>();
            foreach (IAzureStorageConfiguration queueConfig in config.QueueProccessorConfig.AzureQueueStorageConfigurations)
            {
                AzureStorageProvider storage = new AzureStorageProvider(this.logger, secretReader);
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
