namespace Microsoft.Membership.MemberServices.PrivacyMockService
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
    using Microsoft.Membership.MemberServices.Common.Logging.LoggingFilter;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.Privacy.Core.DataManagementConfig;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.Core.VerificationTokenValidation;
    using Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.Common;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.DataManagementConfig;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Graph;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts;
    using Microsoft.Membership.MemberServices.ScheduleDbClient;
    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;

    public sealed class DependencyManager : IDisposable, IDependencyResolver
    {
        private const string ComponentName = nameof(DependencyManager);

        private readonly ILogger logger;

        private readonly object syncLock = new object();

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
            this.activeContainer = this.container.CreateChildContainer();

            this.RegisterGlobalSingletons();
            this.RegisterConfigurationDependentSingletons(configuration.CurrentConfiguration);
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
                (o, args) => this.UpdateActiveContainer(args.Config, configuration.CurrentConfiguration);

            // handle events if the service config file loads a new configuration
            configuration.ConfigurationUpdate +=
                (o, args) => this.UpdateActiveContainer(dataManagementConfigLoader.CurrentDataManagementConfig, args.ConfigurationManager);

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

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public object GetService(Type serviceType)
        {
            return this.resolver.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return this.resolver.GetServices(serviceType);
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

                this.resolver?.Dispose();
                this.resolver = null;

                this.activeContainer?.Dispose();
                this.activeContainer = null;
            }

            this.disposed = true;
        }

        private IDataManagementConfigLoader ResolveDataManagementConfigLoader()
        {
            return (IDataManagementConfigLoader)this.GetService(typeof(IDataManagementConfigLoader));
        }

        private void ResolveInstancesRequiredForStartup()
        {
            this.GetService(typeof(IPxfDispatcher));
        }

        private void InitializeActiveContainer(IPrivacyConfigurationManager currentConfiguration)
        {
            this.logger.Information(nameof(DependencyManager), "InitializeActiveContainer");
            this.UpdateActiveContainer(null, currentConfiguration);
        }

        /// <summary>
        ///     Updates the active container.
        /// </summary>
        /// <param name="dataManagementConfiguration"></param>
        /// <param name="newConfiguration">The new configuration.</param>
        private void UpdateActiveContainer(
            IDataManagementConfig dataManagementConfiguration,
            IPrivacyConfigurationManager newConfiguration)
        {
            this.logger.Information(nameof(DependencyManager), "UpdateActiveContainer");
            lock (this.syncLock)
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
                    newConfiguration,
                    new HierarchicalLifetimeManager());
                this.activeContainer = newContainer;
                this.resolver = new UnityDependencyScope(this.activeContainer, this.container.Resolve<ILogger>());
                this.ResolveInstancesRequiredForStartup();

                int delayInSeconds =
                    newConfiguration.PrivacyExperienceServiceConfiguration
                        .StaleConfigurationDisposalDelayInSeconds;
                ScheduleDisposalOfContainer(oldContainer, TimeSpan.FromSeconds(delayInSeconds), this.logger);
            }
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

            this.container.RegisterType<IAppConfiguration, AppConfiguration>(
                new ContainerControlledLifetimeManager(),
                new InjectionFactory(
                    c => AppConfigurationFactory.Create(c.Resolve<IPrivacyConfigurationManager>())));

            this.container.RegisterType<IRecurrentDeleteQueueFactory, RecurrentDeleteQueueFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IRecurrentDeleteWorkerFactory, RecurrentDeleteWorkerFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IDistributedLockBlobPrimitivesFactory, DistributedLockBlobPrimitivesFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IScheduleDbClient, ScheduleDbCosmosClient>(new HierarchicalLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<IPrivacyConfigurationManager>(),
                    new ResolvedParameter<IAppConfiguration>(),
                    new ResolvedParameter<ILogger>()));

            this.container.RegisterType<IAadTokenProvider, AadTokenProvider>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IHttpClientFactory, HttpClientFactoryPublic>(new HierarchicalLifetimeManager());

            // PDOS
            this.container.RegisterType<IPxfDispatcher, PxfDispatcher>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IPxfAdapterFactory, PxfAdapterFactory>(new HierarchicalLifetimeManager());

            // PCF
            this.container.RegisterType<IGraphAdapter, GraphAdapter>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IAadRequestVerficationServiceAdapterFactory, AadRequestVerificationServiceAdapterFactory>(
                new HierarchicalLifetimeManager());
            this.container.RegisterType<IAadRequestVerificationServiceAdapter, AadRequestVerificationServiceAdapter>(
                new HierarchicalLifetimeManager(),
                new InjectionFactory(
                    c => c.Resolve<IAadRequestVerficationServiceAdapterFactory>().Create()));
            this.container.RegisterType<IValidationServiceFactory, ValidationServiceFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IVerificationTokenValidationService, VerificationTokenValidationService>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IMsaIdentityServiceClientFactory, MsaIdentityServiceClientFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IMsaIdentityServiceAdapter, MsaIdentityServiceAdapter>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IAadAuthManager, AadAuthManager>(new HierarchicalLifetimeManager());
            this.container.RegisterType<ILoggingFilter, LoggingFilter>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IXboxAcountsAdapterFactory, XboxAccountsAdapterFactory>(new HierarchicalLifetimeManager());
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
            this.container.RegisterType<IPcfAdapter, PcfAdapter>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IPcfProxyService, PcfProxyService>(new HierarchicalLifetimeManager());
        }

        /// <summary>
        ///     Registers the global singletons.
        /// </summary>
        private void RegisterGlobalSingletons()
        {
            this.logger.Information(nameof(DependencyManager), "RegisterGlobalSingletons");
            this.container.RegisterType<ICounterFactory, NoOpCounterFactory>(new ContainerControlledLifetimeManager());

            this.container.RegisterType<ICertificateProvider, CertificateProvider>(new ContainerControlledLifetimeManager());

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

            this.container.RegisterType<IClock, Clock>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<IMemoryCache, MemoryCache>(new ContainerControlledLifetimeManager(), new InjectionConstructor(new MemoryCacheOptions()));

            this.container.RegisterType<ITokenManager, InstrumentedAadTokenManager>(new ContainerControlledLifetimeManager(), new InjectionConstructor(new AadTokenManager()));
            this.container.RegisterType<IAadJwtSecurityTokenHandler, AadJwtSecurityTokenHandler>(new ContainerControlledLifetimeManager());
            // Mise token validation
            this.container.RegisterType<IMiseTokenValidationUtility, MiseTokenValidationUtility>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<ILogger>(),
                    new ResolvedParameter<IPrivacyConfigurationManager>()));
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
