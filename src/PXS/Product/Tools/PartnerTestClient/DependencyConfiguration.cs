// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Test.PartnerTestClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Common.Logging.LoggingFilter;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.SecretStore;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.Privacy.Core.DataManagementConfig;
    using Microsoft.Membership.MemberServices.Privacy.Core.Export;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.Core.ScopedDelete;
    using Microsoft.Membership.MemberServices.Privacy.Core.Timeline;
    using Microsoft.Membership.MemberServices.Privacy.Core.UserSettings;
    using Microsoft.Membership.MemberServices.Privacy.Core.VerificationTokenValidation;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter;
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
    using Moq;
    using ScheduleDbClient;

    /// <summary>
    ///     Sets up a Unity Container with all the necessary dependencies.
    /// </summary>
    public class DependencyConfiguration
    {
        private readonly AdapterConfigurationSource adapterConfigurationSource;

        private IUnityContainer container;

        /// <summary>
        ///     Returns the Unity container with all registered dependencies.
        /// </summary>
        public IUnityContainer Container
        {
            get { return container; }
        }

        /// <summary>
        ///     Default constructor for Dependency Configuration class.
        /// </summary>
        public DependencyConfiguration(AdapterConfigurationSource adapterConfigurationSource)
        {
            this.adapterConfigurationSource = adapterConfigurationSource;

            InitializeContainer();
        }

        private void CopyConfigurations()
        {
            Directory.CreateDirectory("PrivacyExperienceService");

            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            IEnumerable<FileInfo> flattenedIniFiles = directory.GetFiles("*ini.flattened.ini");
            foreach (FileInfo file in flattenedIniFiles)
            {
                file.CopyTo("PrivacyExperienceService\\" + file.Name, overwrite: true);
            }
        }

        private void InitializeContainer()
        {
            container = new UnityContainer();

            CopyConfigurations();
            InitializePxs();
        }

        private void InitializePxs()
        {
            // Singleton instance for privacy policies.
            Policy privacyPolicy = Policies.Current;

            // Register the privacy policy as a global singleton.
            container.RegisterInstance(Policies.Current, new ContainerControlledLifetimeManager());

            container.RegisterInstance<ILogger>(DualLogger.Instance);
            container.RegisterInstance(CreateCounterFactory().Object);

            container.RegisterType<IMsaIdentityServiceClientFactory, MsaIdentityServiceClientFactory>(new HierarchicalLifetimeManager());
            container.RegisterType<IMsaIdentityServiceAdapter, MsaIdentityServiceAdapter>(new HierarchicalLifetimeManager());

            container.RegisterType<IVerificationTokenValidationService, VerificationTokenValidationService>(new HierarchicalLifetimeManager());

            // ContainerControlledLifetimeManager
            container.RegisterType<ICertificateProvider, CertificateProvider>(new ContainerControlledLifetimeManager());
            container.RegisterType<IDataManagementConfigLoader, DataManagementConfigLoader>(new ContainerControlledLifetimeManager());

            // HierarchicalLifetimeManager
            container.RegisterType<ISecretStoreReader, AzureKeyVaultReader>(new HierarchicalLifetimeManager());
            container.RegisterType<IPxfAdapterFactory, PxfAdapterFactory>(new HierarchicalLifetimeManager());

            container.RegisterType<ICustomerMasterAdapterFactory, CustomerMasterAdapterFactory>(new HierarchicalLifetimeManager());
            container.RegisterType<ICustomerMasterAdapter, CustomerMasterAdapter>(
                new HierarchicalLifetimeManager(),
                new InjectionFactory(
                    c =>
                        c.Resolve<ICustomerMasterAdapterFactory>()
                            .Create(
                                c.Resolve<ICertificateProvider>(),
                                c.Resolve<IPrivacyConfigurationManager>(),
                                c.Resolve<ILogger>(),
                                c.Resolve<ICounterFactory>())));

            container.RegisterType<IXboxAcountsAdapterFactory, XboxAccountsAdapterFactory>(new HierarchicalLifetimeManager());
            container.RegisterType<IXboxAccountsAdapter, XboxAccountsAdapter>(
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

            container.RegisterType<IAadRequestVerficationServiceAdapterFactory, AadRequestVerificationServiceAdapterFactory>(new HierarchicalLifetimeManager());
            container.RegisterType<IAadRequestVerificationServiceAdapter, AadRequestVerificationServiceAdapter>(
                new HierarchicalLifetimeManager(),
                new InjectionFactory(
                    c =>
                        c.Resolve<IAadRequestVerficationServiceAdapterFactory>().Create()));

            container.RegisterType<IPxfDispatcher, PxfDispatcher>(new HierarchicalLifetimeManager());

            // Mock the writes in this client so they don't write anywhere
            // TODO: Write them to the real storage layer.

            container.RegisterType<IAadJwtSecurityTokenHandler, AadJwtSecurityTokenHandler>(new HierarchicalLifetimeManager());
            container.RegisterType<IGraphAdapter, GraphAdapter>(new HierarchicalLifetimeManager());
            container.RegisterType<IValidationServiceFactory, ValidationServiceFactory>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IAppConfiguration, AppConfiguration>(new ContainerControlledLifetimeManager(),
                new InjectionFactory(c => AppConfigurationFactory.Create(c.Resolve<IPrivacyConfigurationManager>())));
            container.RegisterType<IPcfAdapter, PcfAdapter>(new HierarchicalLifetimeManager());
            container.RegisterType<IPcfProxyService, PcfProxyService>(new HierarchicalLifetimeManager());
            container.RegisterType<IRequestClassifier, RequestClassifier>(new HierarchicalLifetimeManager());
            container.RegisterType<IScheduleDbClient, ScheduleDbCosmosClient>(new HierarchicalLifetimeManager(), 
                new InjectionConstructor(
                    new ResolvedParameter<IPrivacyConfigurationManager>(),
                    new ResolvedParameter<IAppConfiguration>(),
                    new ResolvedParameter<ILogger>()));
            container.RegisterType<ITimelineService>(
                new HierarchicalLifetimeManager(),
                new InjectionFactory(c => new TimelineService(
                    c.Resolve<IPxfDispatcher>(), 
                    privacyPolicy, 
                    c.Resolve<IPcfProxyService>(), 
                    c.Resolve<IRequestClassifier>(),
                    c.Resolve<IScheduleDbClient>(),
                    c.Resolve<IMsaIdentityServiceAdapter>(),
                    c.Resolve<ILogger>())));
            container.RegisterType<IUserSettingsService>(
                new HierarchicalLifetimeManager(),
                new InjectionFactory(c => new UserSettingsService(c.Resolve<ICustomerMasterAdapter>(), c.Resolve<ILogger>())));
            container.RegisterType<IScopedDeleteService>(
                new HierarchicalLifetimeManager(),
                new InjectionFactory(c => new ScopedDeleteService(c.Resolve<IPxfDispatcher>(), c.Resolve<IPcfProxyService>(), c.Resolve<IPrivacyConfigurationManager>(), c.Resolve<IAppConfiguration>(), c.Resolve<ILogger>())));

            container.RegisterType<ILoggingFilter, LoggingFilter>(new HierarchicalLifetimeManager());
            container.RegisterType<IHttpClientFactory, HttpClientFactoryPublic>(new HierarchicalLifetimeManager());
            container.RegisterType<IAadTokenProvider, AadTokenProvider>(new HierarchicalLifetimeManager());
            container.RegisterType<IAadAuthManager, AadAuthManager>(new HierarchicalLifetimeManager());
            container.RegisterType<IClock, Clock>(new ContainerControlledLifetimeManager());

            var configurationLoader = container.Resolve<PrivacyConfigurationLoader>();
            container.RegisterInstance(configurationLoader.CurrentConfiguration);

            container.RegisterType<ITokenManager, InstrumentedAadTokenManager>(new ContainerControlledLifetimeManager(), new InjectionConstructor(new AadTokenManager()));

            switch (adapterConfigurationSource)
            {
                case AdapterConfigurationSource.ConfigurationIniFile:
                    container.RegisterInstance(configurationLoader.CurrentConfiguration.DataManagementConfig);
                    break;
                case AdapterConfigurationSource.MockConfiguration:
                    container.RegisterInstance(configurationLoader.CurrentConfiguration.MockDataManagementConfig);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            container.RegisterType<IAadJwtSecurityTokenHandler, AadJwtSecurityTokenHandler>(new ContainerControlledLifetimeManager());
            // Mise token validation
            this.container.RegisterType<IMiseTokenValidationUtility, MiseTokenValidationUtility>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<ILogger>(),
                    new ResolvedParameter<IPrivacyConfigurationManager>()));
            container.RegisterType<IAzureStorageProvider, AzureStorageProvider>(new ContainerControlledLifetimeManager());
            container.RegisterType<IExportStorageProvider, ExportStorageProvider>(new ContainerControlledLifetimeManager());
            container.RegisterType<ICloudBlobFactory, CloudBlobFactory>(new HierarchicalLifetimeManager());
            container.RegisterType<IExportService, ExportService>(new HierarchicalLifetimeManager());
            container.RegisterType<IMemoryCache, MemoryCache>(new ContainerControlledLifetimeManager(), new InjectionConstructor(new MemoryCacheOptions()));
        }

        private static Mock<ICounterFactory> CreateCounterFactory()
        {
            var mockCounter = new Mock<ICounter>();
            var mockCounterFactory = new Mock<ICounterFactory>();
            mockCounterFactory.Setup(
                t => t.GetCounter(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CounterType>())).Returns(mockCounter.Object);
            return mockCounterFactory;
        }
    }
}
