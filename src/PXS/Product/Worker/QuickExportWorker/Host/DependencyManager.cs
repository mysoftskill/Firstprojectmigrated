// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.Host
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Extensions.Caching.Memory;
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
    using Microsoft.Membership.MemberServices.Privacy.Core.DataManagementConfig;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.QuickExportWorker;
    using Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.CsvSerializer;
    using Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.Tasks;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.DataManagementConfig;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts;
    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;

    using Newtonsoft.Json;

    /// <summary>
    ///     DependencyManager
    /// </summary>
    public sealed class DependencyManager : IDependencyManager, IDisposable
    {
        private readonly object currentSyncLock = new object();

        private readonly ILogger logger;

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
        public DependencyManager(IUnityContainer container)
        {
            this.container = container;
            this.logger = this.container.Resolve<ILogger>();
            this.activeContainer = this.container.CreateChildContainer();
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
            return this.activeContainer.Resolve(serviceType);
        }

        /// <summary>
        ///     Gets the services.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        public IEnumerable<object> GetServices(Type serviceType)
        {
            return this.activeContainer.ResolveAll(serviceType);
        }

        public void Initialize(IPrivacyConfigurationLoader configuration)
        {
            this.container.RegisterInstance<IDependencyManager>(this);
            this.RegisterGlobalSingletons();
            this.RegisterConfigurationDependantSingletons(configuration.CurrentConfiguration);
            this.InitializeActiveContainer(configuration.CurrentConfiguration);

            // resolve the PDMS config loader
            IDataManagementConfigLoader dataManagementConfigLoader = this.ResolveDataManagementConfigLoader();

            if (dataManagementConfigLoader == null)
            {
                this.logger.Error(nameof(DependencyManager), "Failed to create dataManagementConfigLoader");

                // this is required to start up the service
                throw new InvalidOperationException($"Failed to resolve instance of type {nameof(IDataManagementConfigLoader)}.");
            }

            // handle events from PDMS loading a new configuration
            dataManagementConfigLoader.DataManagementConfigurationUpdate +=
                (o, args) => this.ConfigurationUpdate(o, args, configuration.CurrentConfiguration);

            // handle events if the service config file loads a new configuration
            configuration.ConfigurationUpdate +=
                (o, args) => this.ConfigurationUpdate(o, args, dataManagementConfigLoader.CurrentDataManagementConfig);

            // load the data management config. Must be done after the event 'DataManagementConfigurationUpdate' is registered to prevent race condition of event firing before it's registered.
            dataManagementConfigLoader.Load();
        }

        private void ConfigurationUpdate(
            object o,
            DataManagementConfigUpdateEventArgs args,
            IPrivacyConfigurationManager currentConfiguration)
        {
            this.UpdateActiveContainer(args.Config, currentConfiguration);
        }

        private void ConfigurationUpdate(
            object o,
            PrivacyConfigurationUpdateEventArgs args,
            IDataManagementConfig dataManagementConfiguration)
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

                this.activeContainer?.Dispose();
                this.activeContainer = null;
            }

            this.disposed = true;
        }

        private void InitializeActiveContainer(IPrivacyConfigurationManager privacyServiceConfiguration)
        {
            this.logger.Information(nameof(DependencyManager), "InitializeActiveContainer");
            lock (this.currentSyncLock)
            {
                this.activeContainer.RegisterInstance(
                    privacyServiceConfiguration,
                    new HierarchicalLifetimeManager());
                this.ResolveInstancesRequiredForStartup();
            }
        }

        private void RegisterConfigurationDependantSingletons(IPrivacyConfigurationManager currentConfiguration)
        {
            this.logger.Information(nameof(DependencyManager), "setup configuration dependent singletons");
            this.container.RegisterInstance(currentConfiguration, new HierarchicalLifetimeManager());
            this.container.RegisterType<ILoggingFilter, LoggingFilter>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<IHttpClientFactory, HttpClientFactoryPublic>(new ContainerControlledLifetimeManager());

            this.container.RegisterType<IPxfAdapterFactory, PxfAdapterFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IPxfDispatcher, PxfDispatcher>(new HierarchicalLifetimeManager());

            this.container.RegisterType<ICustomerMasterAdapterFactory, CustomerMasterAdapterFactory>(
                new HierarchicalLifetimeManager());
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

            this.container.RegisterType<ITokenManager, InstrumentedAadTokenManager>(new ContainerControlledLifetimeManager(), new InjectionConstructor(new AadTokenManager()));
            this.container.RegisterType<IAadTokenProvider, AadTokenProvider>(new HierarchicalLifetimeManager());
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

            this.container.RegisterType<IAppConfiguration, AppConfiguration>(
                new ContainerControlledLifetimeManager(),
                new InjectionFactory(
                    c => AppConfigurationFactory.Create(c.Resolve<IPrivacyConfigurationManager>())));

            this.logger.Information(nameof(DependencyManager), "Configuration dependent singleton registration complete.");
        }

        private void RegisterGlobalSingletons()
        {
            this.logger.Information(nameof(DependencyManager), "setup global singletons");
            JsonSerializerSettingsForWorkers.SetupJsonSerializerSettings(this.container);

            this.container.RegisterType<ICounterFactory, NoOpCounterFactory> (new ContainerControlledLifetimeManager());

            // RPS must be singleton as RPS.Initialize should be called only once in application scope.
            // this.container.RegisterType<IRpsAuthServer, RpsAuthServer>(new ContainerControlledLifetimeManager(),  new InjectionFactory(c => new RpsAuthServer()));
            this.container.RegisterType<ICertificateProvider, CertificateProvider>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<ISecretStoreReader, AzureKeyVaultReader>(new ContainerControlledLifetimeManager());

            // Register the privacy policy as a global singleton.
            this.container.RegisterInstance(Policies.Current, new ContainerControlledLifetimeManager());

            this.container.RegisterType<IDataManagementConfig, DataManagementConfig>(
                new HierarchicalLifetimeManager(),
                new InjectionFactory(c => c.Resolve<IDataManagementConfigLoader>().CurrentDataManagementConfig));

            // No need to recreate this when new child container is created, 
            // otherwise it will end up firing off a task on each container that is polling PDMS.
            // This means modifying service config files will not recreate a DataManagementConfigLoader
            this.container.RegisterType<IDataManagementConfigLoader, DataManagementConfigLoader>(
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<IPrivacyConfigurationManager>(),
                    new ResolvedParameter<ILogger>()));

            this.logger.Information(nameof(DependencyManager), "Setup export global singletons");
            this.container.RegisterType<IAzureStorageProvider, AzureStorageProvider>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<IExportStorageProvider, ExportStorageProvider>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<ISerializer, CsvSerializer>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<IMemoryCache, MemoryCache>(new ContainerControlledLifetimeManager(), new InjectionConstructor(new MemoryCacheOptions()));
            this.container.RegisterInstance(new ExportDequeuer());
            this.container.RegisterInstance(new DeleteExportArchivesDequeuer());

            // Global singletons should be 'ContainerControlled' so if config reloads they do not get recreated in a child container.
            this.container.RegisterType<IClock, Clock>(new ContainerControlledLifetimeManager());
        }

        private IDataManagementConfigLoader ResolveDataManagementConfigLoader()
        {
            return (IDataManagementConfigLoader)this.GetService(typeof(IDataManagementConfigLoader));
        }

        private void ResolveInstancesRequiredForStartup()
        {
            this.GetService(typeof(IPxfDispatcher));
        }

        private void UpdateActiveContainer(
            IDataManagementConfig dataManagementConfiguration,
            IPrivacyConfigurationManager privacyServiceConfiguration)
        {
            this.logger.Information(nameof(DependencyManager), "UpdateActiveContainer");
            lock (this.currentSyncLock)
            {
                IUnityContainer oldContainer = this.activeContainer;
                IUnityContainer newContainer = this.container.CreateChildContainer();

                // can be null on app start
                if (dataManagementConfiguration != null)
                {
                    newContainer.RegisterInstance(
                        dataManagementConfiguration,
                        new HierarchicalLifetimeManager());
                }

                newContainer.RegisterInstance(
                    privacyServiceConfiguration,
                    new HierarchicalLifetimeManager());
                this.activeContainer = newContainer;
                this.ResolveInstancesRequiredForStartup();

                int delayInSeconds =
                    privacyServiceConfiguration.PrivacyExperienceServiceConfiguration
                        .StaleConfigurationDisposalDelayInSeconds;
                ScheduleDisposalOfContainer(oldContainer, TimeSpan.FromSeconds(delayInSeconds), this.logger);
            }
        }

        private static void ScheduleDisposalOfContainer(IUnityContainer oldContainer, TimeSpan delay, ILogger logger)
        {
            if (oldContainer != null)
            {
                Task.Run(
                    async () =>
                    {
                        try
                        {
                            await Task.Delay(delay).ConfigureAwait(false);
                            logger.Information(nameof(DependencyManager), "disposing old container");
                            oldContainer.Dispose();
                        }
                        catch (Exception ex)
                        {
                            logger.Error(nameof(DependencyManager), "exception disposing old container " + ex);
                        }
                    });
            }
        }
    }
}
