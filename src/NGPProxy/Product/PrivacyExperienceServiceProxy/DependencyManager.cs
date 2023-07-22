// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.NgpProxy.PrivacyExperienceServiceProxy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Http.Dependencies;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Membership.MemberServices.AzureInfraCommon.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.LoggingFilter;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.SecretStore;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.Core.VerificationTokenValidation;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Graph;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers;
    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.Windows.Services.AuthN.Server;

    /// <summary>
    ///     DependencyManager
    /// </summary>
    public sealed class DependencyManager : IDependencyResolver, IDisposable
    {
        private const string AadTokenIssuerInstance = "aadTokenIssuer";

        private const string ComponentName = nameof(DependencyManager);

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
            this.SetupDependencies();
            this.InitializeActiveContainer(configuration.CurrentConfiguration);
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

        /// <summary>
        ///     Dispose underlying AP resources.
        /// </summary>
        /// <param name="disposing">true if called from <see cref="IDisposable" />; otherwise false.</param>
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
            this.UpdateActiveContainer(privacyServiceConfiguration);
        }

        private void RegisterConfigurationDependentSingletons()
        {
            this.container.RegisterType<ILoggingFilter, LoggingFilter>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IHttpClientFactory, HttpClientFactoryPublic>(new HierarchicalLifetimeManager());

            this.container.RegisterType<IAadRequestVerficationServiceAdapterFactory, AadRequestVerificationServiceAdapterFactory>(
                new HierarchicalLifetimeManager());
            this.container.RegisterType<IAadRequestVerificationServiceAdapter, AadRequestVerificationServiceAdapter>(
                new HierarchicalLifetimeManager(),
                new InjectionFactory(
                    c => c.Resolve<IAadRequestVerficationServiceAdapterFactory>().Create()));

            this.container.RegisterType<ISecretStoreReader, AzureKeyVaultReader>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<IAadTokenProvider, AadTokenProvider>(AadTokenIssuerInstance, new HierarchicalLifetimeManager());
            this.container.RegisterType<IAadAuthManager, AadAuthManager>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IValidationServiceFactory, ValidationServiceFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IVerificationTokenValidationService, VerificationTokenValidationService>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IGraphAdapter, GraphAdapter>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IRequestClassifier, RequestClassifier>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IPcfAdapter, PcfAdapter>(new HierarchicalLifetimeManager());
            this.container.RegisterType<ICustomerMasterAdapter, NotSupportedCustomerMasterAdapter>();

            this.container.RegisterType<IAppConfiguration, AppConfiguration>(
                new ContainerControlledLifetimeManager(),
                new InjectionFactory(
                    c => AppConfigurationFactory.Create(c.Resolve<IPrivacyConfigurationManager>())));

            this.logger.Information(ComponentName, "Configuration dependent singleton registration complete.");
        }

        private void RegisterCoreServices()
        {
            this.container.RegisterType<IPcfProxyService, PcfProxyService>(new HierarchicalLifetimeManager());

            this.logger.Information(ComponentName, "Core services registration complete.");
        }

        private void RegisterGlobalSingletons()
        {
            this.container.RegisterInstance(Policies.Current, new ContainerControlledLifetimeManager());
            this.container.RegisterType<ICounterFactory, NoOpCounterFactory>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<ICertificateProvider, CertificateProvider>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<IAzureStorageProvider, AzureStorageProvider>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<IClock, Clock>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<IMemoryCache, MemoryCache>(new ContainerControlledLifetimeManager(), new InjectionConstructor(new MemoryCacheOptions()));
            this.container.RegisterType<ICertificateValidator>(
                new ContainerControlledLifetimeManager(),
                new InjectionFactory(c => new Sha256HashCertificateValidator(Enumerable.Empty<string>())));
            this.container.RegisterType<ITokenManager, InstrumentedAadTokenManager>(new ContainerControlledLifetimeManager(), new InjectionConstructor(new AadTokenManager()));
            this.container.RegisterType<IAadJwtSecurityTokenHandler, AadJwtSecurityTokenHandler>(new ContainerControlledLifetimeManager());
            // Mise token validation
            this.container.RegisterType<IMiseTokenValidationUtility, MiseTokenValidationUtility>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<ILogger>(),
                    new ResolvedParameter<IPrivacyConfigurationManager>()));

            // NotSupported. 
            // Register something since certain dependencies we use require these types registered.
            // If any code path tries to use any of these, exceptions are supposed to be thrown.
            this.container.RegisterType<IMsaIdentityServiceAdapter, NotSupportedMsaIdentityServiceAdapter>();
            this.container.RegisterType<IXboxAccountsAdapter, NotSupportedXboxAccountsAdapter>();
            this.container.RegisterType<IRpsAuthServer, NotSupportedRpsAuth>();
            this.container.RegisterType<IMachineIdRetriever, ServiceFabricMachineIdRetriever>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<IFamilyClaimsParser, NotSupportedFamilyClaimsParser>();

            this.logger.Information(ComponentName, "Global singleton registration complete.");
        }

        private void ResolveInstancesRequiredForStartup()
        {
            this.resolver.GetService(typeof(IGraphAdapter));
            this.resolver.GetService(typeof(IPcfAdapter));
            this.resolver.GetService(typeof(IPcfProxyService));
            this.resolver.GetService(typeof(IAadRequestVerificationServiceAdapter));
            this.resolver.GetService(typeof(IVerificationTokenValidationService));
            this.resolver.GetService(typeof(UsersController));
            this.resolver.GetService(typeof(DataPolicyOperationsController));
            this.resolver.GetService(typeof(DirectoryController));
        }

        private void SetupDependencies()
        {
            this.RegisterGlobalSingletons();
            this.RegisterConfigurationDependentSingletons();
            this.RegisterCoreServices();
        }

        private void UpdateActiveContainer(IPrivacyConfigurationManager privacyServiceConfiguration)
        {
            lock (this.currentSyncLock)
            {
                IUnityContainer oldContainer = this.activeContainer;
                IUnityContainer newContainer = this.container.CreateChildContainer();

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
