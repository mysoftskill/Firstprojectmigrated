// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.PrivacyVsoWorker
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

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
    using Microsoft.Membership.MemberServices.Privacy.PrivacyVsoWorker.Processor;
    using Microsoft.Membership.MemberServices.Privacy.PrivacyVsoWorker.Utility;
    using Microsoft.Membership.PrivacyServices.KustoHelpers;
    using Microsoft.Practices.Unity;

    using Newtonsoft.Json;

    using Microsoft.PrivacyServices.Common.Azure;

    internal class DependencyManager : IDisposable, IDependencyManager
    {
        /// <summary>
        ///     Registration name for the PrivacyVsoWorker
        /// </summary>
        internal const string PrivacyVsoWorker = "PrivacyVsoWorker";

        private readonly ILogger logger;

        private readonly object syncLock = new object();
        private bool disposed;
        private IUnityContainer container;

        /// <summary>
        ///     Gets the container.
        /// </summary>
        public IUnityContainer Container { get; private set; }

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
            return this.Container.Resolve(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return this.Container.ResolveAll(serviceType);
        }

        /// <summary>
        ///     Releases un managed and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and un managed resources; <c>false</c> to release only unmanaged resources.</param>
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

                this.Container?.Dispose();
                this.Container = null;
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

            this.container.RegisterInstance<IAadTokenAuthConfiguration>(currentConfiguration.AadTokenAuthGeneratorConfiguration);
            this.container.RegisterInstance<IKustoConfig>(currentConfiguration.PrivacyVsoWorkerConfiguration.KustoConfig, new HierarchicalLifetimeManager());
            this.container.RegisterInstance<IVSOConfig>(currentConfiguration.PrivacyVsoWorkerConfiguration.VSOConfig, new HierarchicalLifetimeManager());
            this.container.RegisterInstance<IFileSystemProcessorConfig>(currentConfiguration.PrivacyVsoWorkerConfiguration.FileSystemProcessorConfig, new HierarchicalLifetimeManager());
            
            this.container.RegisterType<IAadJwtSecurityTokenHandler, AadJwtSecurityTokenHandler>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IAadAuthManager, AadAuthManager>(new HierarchicalLifetimeManager());
            this.container.RegisterType<ITokenManager, InstrumentedAadTokenManager>(new ContainerControlledLifetimeManager(), new InjectionConstructor(new AadTokenManager()));
            this.container.RegisterType<IClock, Clock>(new ContainerControlledLifetimeManager());
            // Mise token validation
            this.container.RegisterType<IMiseTokenValidationUtility, MiseTokenValidationUtility>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<ILogger>(),
                    new ResolvedParameter<IPrivacyConfigurationManager>()));

            this.container.RegisterType<IFileSystemProcessor, FileSystemProcessor>(
                new HierarchicalLifetimeManager(),
                new InjectionConstructor(
                    currentConfiguration.PrivacyVsoWorkerConfiguration.FileSystemProcessorConfig,
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)));

            this.container.RegisterType<IKustoClientFactory, KustoClientFactory>(new ContainerControlledLifetimeManager());
            this.container.RegisterInstance<IKustoDataHelper>(new KustoDataHelper(
                this.container.Resolve<IKustoClientFactory>(),
                this.container.Resolve<IKustoConfig>()));

            this.container.RegisterType<ISecretStoreReader, AzureKeyVaultReader>(new HierarchicalLifetimeManager());

            this.container.RegisterInstance<IVsoHelper>(new VsoHelper(
                this.container.Resolve<ILogger>(),
                this.container.Resolve<IVSOConfig>(),
                this.container.Resolve<IFileSystemProcessor>(),
                this.container.Resolve<ISecretStoreReader>()));

            this.container.RegisterType<ILoggingFilter, LoggingFilter>(new HierarchicalLifetimeManager());

            this.container.RegisterType<IPrivacyConfigurationManager, PrivacyConfigurationManager>(new HierarchicalLifetimeManager());

            this.container.RegisterType<IWorker, ItemProcessor>(PrivacyVsoWorker, new HierarchicalLifetimeManager(),
                new InjectionConstructor(this.container.Resolve<ILogger>(),
                    this.container.Resolve<ICounterFactory>(),
            this.container.Resolve<IKustoDataHelper>(),
            this.container.Resolve<IFileSystemProcessor>(),
            this.container.Resolve<IVsoHelper>(),
            currentConfiguration.PrivacyVsoWorkerConfiguration.JobExecutionIntervalInDays));

            Trace.TraceInformation($"AzureKeyVaultConfiguration.VaultBaseUrl: {currentConfiguration.AzureKeyVaultConfiguration.VaultBaseUrl}"); 
        }

        /// <summary>
        ///     Registers the global singletons.
        /// </summary>
        private void RegisterGlobalSingletons()
        {
            this.logger.Information(nameof(DependencyManager), "RegisterGlobalSingletons");

            container.RegisterType<ICounterFactory, NoOpCounterFactory>(new ContainerControlledLifetimeManager());
            container.RegisterType<ICertificateProvider, CertificateProvider>(new ContainerControlledLifetimeManager());
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
                this.Container = newContainer;
            }
        }
    }
}
