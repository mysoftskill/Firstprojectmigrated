// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.LoggingFilter;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.SecretStore;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.EventHubProcessor;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.QueueProcessor;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAccountClose;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.Core.VerificationTokenValidation;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.Practices.Unity;

    using Newtonsoft.Json;

    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.DeadLetterReProcessor;

    internal class DependencyManager : IDisposable, IDependencyManager
    {
        /// <summary>
        ///     Registration name for the Aad-Aacount-Close-Queue-Worker
        /// </summary>
        internal const string AadAacountCloseQueueWorker = "AadAacountCloseQueueWorker";

        internal const string AadAacountCloseDeadLetterWorker = "AadAacountCloseDeadLetterWorker";

        /// <summary>
        ///     Registration name for the Aad-Account-Close Queue Storage Providers
        /// </summary>
        internal const string AadAccountCloseQueueStorageProviders = "AadAccountCloseQueueStorageProviders";

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
            this.container.RegisterInstance(configuration.CurrentConfiguration, new HierarchicalLifetimeManager());
            this.SetupDependencies();
            this.SetupConfigurationDependentStorage(configuration.CurrentConfiguration.AadAccountCloseWorkerConfiguration);
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
        private void RegisterConfigurationDependentSingletons()
        {
            this.logger.Information(nameof(DependencyManager), "RegisterConfigurationDependentSingletons");

            JsonSerializerSettingsForWorkers.SetupJsonSerializerSettings(this.container);

            this.container.RegisterType<ILoggingFilter, LoggingFilter>(new HierarchicalLifetimeManager());

            this.container.RegisterType<IHttpClientFactory, HttpClientFactoryPublic>(new HierarchicalLifetimeManager());

            this.container.RegisterType<ISecretStoreReader, AzureKeyVaultReader>(new HierarchicalLifetimeManager());

            // Verification Token Validation service
            this.container.RegisterType<IValidationServiceFactory, ValidationServiceFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IVerificationTokenValidationService, VerificationTokenValidationService>(new HierarchicalLifetimeManager());

            // AAD Queue Processor
            this.container.RegisterType<IAadAccountCloseQueueProcessorFactory, AadAccountCloseQueueProcessorFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IWorker, AadAccountCloseQueueProcessorCollection>(AadAacountCloseQueueWorker, new HierarchicalLifetimeManager());

            // AAD dependencies
            this.container.RegisterType<IEventHubHelpers, EventHubHelpers>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IAadAuthenticationHelper, AadAuthenticationHelper>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IPcfAdapter, PcfAdapter>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IAadAccountCloseService, AadAccountCloseService>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IRequestClassifier, RequestClassifier>(new HierarchicalLifetimeManager());

            this.container.RegisterType<IAadRequestVerficationServiceAdapterFactory, AadRequestVerificationServiceAdapterFactory>(
                new HierarchicalLifetimeManager());

            this.container.RegisterType<IAadRequestVerificationServiceAdapter, AadRequestVerificationServiceAdapter>(
                new HierarchicalLifetimeManager(),
                new InjectionFactory(
                    c => c.Resolve<IAadRequestVerficationServiceAdapterFactory>().Create()));
            this.container.RegisterType<IAadAuthManager, AadAuthManager>(new HierarchicalLifetimeManager());

            this.container.RegisterType<IAppConfiguration, AppConfiguration>(
                new ContainerControlledLifetimeManager(),
                new InjectionFactory(
                    c => AppConfigurationFactory.Create(c.Resolve<IPrivacyConfigurationManager>())));
        }

        /// <summary>
        ///     Registers the global singletons.
        /// </summary>
        private void RegisterGlobalSingletons()
        {
            this.logger.Information(nameof(DependencyManager), "RegisterGlobalSingletons");
            this.container.RegisterType<ICounterFactory, NoOpCounterFactory> (new ContainerControlledLifetimeManager());

            this.container.RegisterType<ICertificateProvider, CertificateProvider>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<ITokenManager, InstrumentedAadTokenManager>(new ContainerControlledLifetimeManager(), new InjectionConstructor(new AadTokenManager()));
            this.container.RegisterType<IClock, Clock>(new ContainerControlledLifetimeManager());
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
            IAadAccountCloseWorkerConfiguration config)
        {
            ISecretStoreReader secretReader = this.container.Resolve<ISecretStoreReader>();

            IList<IAzureStorageProvider> queueStorageProviders = new List<IAzureStorageProvider>();
            foreach (IAzureStorageConfiguration storageConfiguration in config.QueueProccessorConfig.AzureQueueStorageConfigurations)
            {
                AzureStorageProvider storage = new AzureStorageProvider(this.logger, secretReader);
                storage.InitializeAsync(storageConfiguration).GetAwaiter().GetResult();
                queueStorageProviders.Add(storage);
            }

            this.container.RegisterInstance(AadAccountCloseQueueStorageProviders, queueStorageProviders, new HierarchicalLifetimeManager());

            this.container.RegisterType<ITable<AccountCloseDeadLetterStorage>, AzureTable<AccountCloseDeadLetterStorage>>(
                new HierarchicalLifetimeManager(),
                new InjectionConstructor(queueStorageProviders[0], this.container.Resolve<ILogger>(), nameof(AccountCloseDeadLetterStorage).ToLowerInvariant()));
            this.container.RegisterType<ITable<NotificationDeadLetterStorage>, AzureTable<NotificationDeadLetterStorage>>(
                new HierarchicalLifetimeManager(),
                new InjectionConstructor(queueStorageProviders[0], this.container.Resolve<ILogger>(), nameof(NotificationDeadLetterStorage).ToLowerInvariant()));

            this.container.RegisterType<IAccountCloseQueueManager, AadAccountCloseQueueManager>(
                new HierarchicalLifetimeManager(),
                new InjectionConstructor(
                    queueStorageProviders,
                    new ResolvedParameter<ILogger>(),
                    config.QueueProccessorConfig,
                    new ResolvedParameter<ICounterFactory>()));

            // Dead Letter table processor
            this.container.RegisterType<IWorker, DeadLetterReProcessorWorker>(AadAacountCloseDeadLetterWorker, new HierarchicalLifetimeManager(), new InjectionConstructor(
                    new ResolvedParameter<ILogger>(),
                    config.QueueProccessorConfig,
                    new ResolvedParameter<IAccountCloseQueueManager>(),
                    new ResolvedParameter<IAadAccountCloseService>(),
                    new ResolvedParameter<ITable<AccountCloseDeadLetterStorage>>(),
                    new ResolvedParameter<IAppConfiguration>(),
                    queueStorageProviders[0]
                    ));

        }

        /// <summary>
        ///     Setups the dependencies.
        /// </summary>
        private void SetupDependencies()
        {
            this.RegisterGlobalSingletons();
            this.RegisterConfigurationDependentSingletons();
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
