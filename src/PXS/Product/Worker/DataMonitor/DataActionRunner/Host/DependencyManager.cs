// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.LoggingFilter;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.SecretStore;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Locks;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Tables;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.DataManagementAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.Membership.PrivacyServices.KustoHelpers;
    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility.Incidents;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Data;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Storage;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Tasks;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Utility;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.Azure.Cosmos.Table;

    using Newtonsoft.Json;

    using IPxsHttpClientFactory = Microsoft.Membership.MemberServices.PrivacyAdapters.Factory.IHttpClientFactory;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Worker;

    /// <summary>
    ///     contract for an object resolver
    /// </summary>
    public interface IDependencyManager : IDisposable
    {
        /// <summary>
        ///     Gets the underlying active Unity container
        /// </summary>
        IUnityContainer Container { get; }

        /// <summary>
        ///     Gets an instance of the requested type with a null tag
        /// </summary>
        T GetType<T>();

        /// <summary>
        ///     Gets an instance of the requested type with the specified tag
        /// </summary>
        /// <typeparam name="T">type of object to create</typeparam>
        /// <param name="tag">item tag</param>
        /// <returns>resulting value</returns>
        T GetType<T>(string tag);
    }

    /// <summary>
    ///     DependencyManager
    /// </summary>
    public sealed class DependencyManager : IDependencyManager
    {
        private static readonly IDictionary<string, (Type Req, Type Concrete)> ConfigTypeMap =
            new Dictionary<string, (Type Config, Type Worker)>(StringComparer.OrdinalIgnoreCase)
            {
                { "JobScheduler", (typeof(IDataActionJobSchedulerConfig), typeof(JobSchedulerTask)) },
                { "JobRunner", (typeof(IDataActionJobRunnerConfig), typeof(JobRunnerTask)) },
            };
        
        private readonly ILogger logger;

        private IUnityContainer activeContainer;
        private IUnityContainer container;

        /// <summary>
        ///     Initializes a new instance of the DependencyManager class
        /// </summary>
        /// <param name="container">container to register objects in</param>
        public DependencyManager(IUnityContainer container)
        {
            ArgumentCheck.ThrowIfNull(container, nameof(container));
            
            IUnityContainer childContainer = container.CreateChildContainer();

            this.activeContainer = childContainer;
            this.container = container;
            this.logger = container.Resolve<ILogger>();

            this.container.RegisterInstance<IDependencyManager>(this);
        }

        /// <summary>
        ///     Gets the underlying active Unity container
        /// </summary>
        public IUnityContainer Container => this.container;

        public void Initialize(IPrivacyConfigurationLoader configLoader)
        {
            this.RegisterGlobalSingletons(this.container, configLoader.CurrentConfiguration.DataActionRunnerConfig);

            Microsoft.PrivacyServices.Common.ContextModelCommon.Setup.UnitySetup.RegisterAssemblyTypes(this.container);
            Microsoft.PrivacyServices.Common.TemplateBuilder.Setup.UnitySetup.RegisterAssemblyTypes(this.container);
            Microsoft.PrivacyServices.DataMonitor.DataAction.Setup.UnitySetup.RegisterAssemblyTypes(this.container);

            this.RegisterConfigurationDependentSingletons(this.activeContainer, configLoader.CurrentConfiguration);
        }

        /// <summary>
        ///     frees, releases, or resets resources
        /// </summary>
        public void Dispose()
        {
            this.activeContainer?.Dispose();
            this.container?.Dispose();
            this.activeContainer = null;
            this.container = null;
        }

        /// <summary>
        ///     Gets an instance of the requested type with a null tag
        /// </summary>
        /// <typeparam name="T">type of object to create</typeparam>
        /// <returns>resulting instance</returns>
        public T GetType<T>()
        {
            if (this.container == null)
            {
                throw new ObjectDisposedException("object has been disposed");
            }

            return this.activeContainer.Resolve<T>();
        }

        /// <summary>
        ///     Gets an instance of the requested type with the specified tag
        /// </summary>
        /// <typeparam name="T">type of object to create</typeparam>
        /// <param name="tag">registration tag</param>
        /// <returns>resulting instance</returns>
        public T GetType<T>(string tag)
        {
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(tag, nameof(tag));

            if (this.container == null)
            {
                throw new ObjectDisposedException("object has been disposed");
            }

            return this.activeContainer.Resolve<T>(tag);
        }

        /// <summary>
        ///     Registers an ITable instance of the specified type
        /// </summary>
        /// <typeparam name="T">row type</typeparam>
        /// <param name="container">unity container</param>
        /// <param name="namePrefix">prefix to prepend to the name of the type that represents the table's schema</param>
        /// <param name="storage">storage provider</param>
        private void RegisterTable<T>(
            IUnityContainer container,
            string namePrefix,
            IAzureStorageProvider storage)
            where T : class, ITableEntity, ITableEntityInitializer, new()
        {
            // Azure tables don't allow non-alphanumeric characters, so use '0' as a delimiter.
            string tableName = string.IsNullOrWhiteSpace(namePrefix) ?
                typeof(T).Name.ToLowerInvariant() :
                namePrefix + "0" + typeof(T).Name.ToLowerInvariant();

            container.RegisterType<ITable<T>, AzureTable<T>>(
                new HierarchicalLifetimeManager(),
                new InjectionConstructor(storage, this.logger, tableName));
        }

        /// <summary>
        ///     Registers an IQueue instance of the specified type
        /// </summary>
        /// <typeparam name="T">row type</typeparam>
        /// <param name="container">unity container</param>
        private void RegisterQueue<T>(IUnityContainer container)
        {
            container.RegisterType<IQueue<T>, InMemoryQueue<T>>(
                new HierarchicalLifetimeManager(),
                new InjectionConstructor(typeof(T).Name.ToLowerInvariant()));
        }

        /// <summary>
        ///     Setups the configuration dependent storage provider
        /// </summary>
        /// <param name="container">container</param>
        /// <param name="config">configuration</param>
        private void SetupConfigurationDependentStorage(
            IUnityContainer container,
            IDataActionRunnerConfig config)
        {
            ISecretStoreReader secretReader = container.Resolve<ISecretStoreReader>();
            AzureStorageProvider storage = new AzureStorageProvider(this.logger, secretReader);
            string namePrefix = config.StorageItemNamePrefix?.ToLowerInvariant();

            storage.InitializeAsync(config.AzureStorageConfiguration).GetAwaiter().GetResult();

            container.RegisterInstance<IAzureStorageProvider>(storage, new HierarchicalLifetimeManager());

            this.RegisterTable<TemplateDefState>(container, namePrefix, storage);
            this.RegisterTable<ActionDefState>(container, namePrefix, storage);
            this.RegisterTable<ActionRefState>(container, namePrefix, storage);

            this.RegisterQueue<JobWorkItem>(container);

            string lockTableName = string.IsNullOrWhiteSpace(namePrefix) ?
                config.LockTableName.ToLowerInvariant() :
                namePrefix + "0" + config.LockTableName.ToLowerInvariant();

            container.RegisterType<ILockManager, AzureTableLockManager>(
                new HierarchicalLifetimeManager(),
                new InjectionConstructor(
                    storage, 
                    this.logger, 
                    container.Resolve<IClock>(),
                    lockTableName));
        }

        /// <summary>
        ///     Registers the configuration dependent singletons
        /// </summary>
        private void RegisterConfigurationDependentSingletons(
            IUnityContainer container,
            IPrivacyConfigurationManager config)
        {
            this.logger.Information(nameof(DependencyManager), "Begin dependent singleton configuration");

            container.RegisterInstance(config);

            container.RegisterInstance<IAadTokenAuthConfiguration>(config.AadTokenAuthGeneratorConfiguration);
            container.RegisterInstance<IDataActionRunnerConfig>(config.DataActionRunnerConfig);

            this.SetupConfigurationDependentStorage(container, config.DataActionRunnerConfig);

            this.container.RegisterType<IAadJwtSecurityTokenHandler, AadJwtSecurityTokenHandler>(new HierarchicalLifetimeManager());
            this.container.RegisterType<IAadAuthManager, AadAuthManager>(new HierarchicalLifetimeManager());
            // Mise token validation
            this.container.RegisterType<IMiseTokenValidationUtility, MiseTokenValidationUtility>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<ILogger>(),
                    new ResolvedParameter<IPrivacyConfigurationManager>()));

            // register each of the tasks based on what we have in the config
            foreach (ITaskConfig taskConfig in config.DataActionRunnerConfig.Tasks.Values)
            {
                (Type Config, Type Worker) types = DependencyManager.ConfigTypeMap[taskConfig.TaskType];

                container.RegisterType(
                    typeof(IBackgroundTask),
                    types.Worker,
                    taskConfig.Tag,
                    new HierarchicalLifetimeManager(),
                    new InjectionFactory(c => c.Resolve(types.Worker, new DependencyOverride(types.Config, taskConfig))));
            }

            this.container.RegisterType<IAppConfiguration, AppConfiguration>(
                new ContainerControlledLifetimeManager(),
                new InjectionFactory(
                    c => AppConfigurationFactory.Create(c.Resolve<IPrivacyConfigurationManager>())));

            this.logger.Information(nameof(DependencyManager), "Completed dependent singleton configuration");
        }

        /// <summary>
        ///     Registers the global singletons
        /// </summary>
        private void RegisterGlobalSingletons(
            IUnityContainer container,
            IDataActionRunnerConfig config)
        {
            LifetimeManager Singleton() => new ContainerControlledLifetimeManager();

            // Global singletons should be 'ContainerControlled' so if config reloads they do not get recreated in a child container.

            this.logger.Information(nameof(DependencyManager), "Begin global singleton configuration");

            container.RegisterType<ITokenManager, InstrumentedAadTokenManager>(new ContainerControlledLifetimeManager(), new InjectionConstructor(new AadTokenManager()));
            container.RegisterType<ITelemetryLogger, TelemetryLogger>(new ContainerControlledLifetimeManager());

            JsonSerializerSettingsForWorkers.SetupJsonSerializerSettings(container);

            // Counter Factory needs to be Singleton.
            // If you create instance of UInt32Counter and UInt64Counter classes with same counter name twice it will still be bound 
            //  to same physical counter, however for better performance it is recommended to create instance one time when you start 
            //  using the counter and dispose it when program finishes rather than creating and disposing it every time you need to 
            //  update it.
            // Reference : https://sharepoint/sites/autopilot/wiki/perf%20counters/Perf%20Counters%20API.aspx
            container.RegisterType<ICounterFactory, NoOpCounterFactory>(Singleton());
            container.RegisterType<ICertificateProvider, CertificateProvider>(Singleton());

            container.RegisterType<ISecretStoreReader, AzureKeyVaultReader>(Singleton());

            container.RegisterInstance(Policies.Current, Singleton());

            container.RegisterType<IClock, Clock>(Singleton());

            container.RegisterType<ILoggingFilter, AlwaysSkipLoggingFilter>(Singleton());
            container.RegisterType<IPxsHttpClientFactory, HttpClientFactoryPublic>(Singleton());

            container.RegisterType<IActionLibraryAccessor, FileSystemAccessor>(
                Singleton(),
                new InjectionConstructor(
                    config.FileSystemLibraryConfig, 
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    new ResolvedParameter<IActionRefExcludedAgentsOverrides>()));

            container.RegisterType<IActionRefExcludedAgentsOverrides, ActionRefExcludedAgentsOverrides>(
                Singleton(),
                new InjectionConstructor(
                    new ResolvedParameter<IAppConfiguration>(),
                    new ResolvedParameter<ILogger>()));

            container.RegisterType<IActionManagerFactory, ActionManagerFactory>(Singleton());
            container.RegisterType<ILocalUnityRegistrar, LocalUnityRegistrar>(Singleton());

            container.RegisterType<IKustoClientFactory, KustoClientFactory>(Singleton());

            container.RegisterInstance<IKustoConfig>(config.KustoConfig, Singleton());
            container.RegisterInstance<ISmtpConfig>(config.SmtpConfig, Singleton());
            container.RegisterInstance<IMucpConfig>(config.MucpConfig, Singleton());

            this.container.RegisterType<IDataManagementClientFactory, DataManagementClientFactory>(Singleton());

            this.logger.Information(nameof(DependencyManager), "Completed global singleton configuration");
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class AlwaysSkipLoggingFilter : ILoggingFilter
        {
            /// <summary>
            ///     Returns a value indicating whether to log request and response details for the specified identity
            /// </summary>
            /// <param name="identity">identity to check</param>
            /// <returns>true to log details; false otherwise</returns>
            public bool ShouldLogDetailsForUser(string identity) => false;
        }

        private class FakeIncidentFiler : IIncidentCreator
        {
            /// <summary>
            ///     Files the specified incident
            /// </summary>
            /// <param name="cancellationToken">cancellation token</param>
            /// <param name="incident">incident to file</param>
            /// <returns>result of filing the incident</returns>
            public Task<IncidentCreateResult> CreateIncidentAsync(
                CancellationToken cancellationToken,
                AgentIncident incident)
            {
                return Task.FromResult(new IncidentCreateResult(1, IncidentFileStatus.Created));
            }
        }
    }
}
