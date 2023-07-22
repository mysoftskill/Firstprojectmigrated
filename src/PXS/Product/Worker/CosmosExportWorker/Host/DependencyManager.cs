// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Locks;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Tables;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.ManifestParsers;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.Storage;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.Azure.Cosmos.Table;

    using Newtonsoft.Json;

    using IPxsHttpClientFactory = Microsoft.Membership.MemberServices.PrivacyAdapters.Factory.IHttpClientFactory;
    using IPcfHttpClientFactory = Microsoft.PrivacyServices.CommandFeed.Client.IHttpClientFactory;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.CosmosHelpers;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.Common.Cosmos;

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
    ///     registers types in Unity 
    /// </summary>
    public sealed class DependencyManager : IDependencyManager
    {
        private static readonly IDictionary<string, (Type Req, Type Concrete)> ConfigTypeMap =
            new Dictionary<string, (Type Config, Type Worker)>(StringComparer.OrdinalIgnoreCase)
            {
                { "FileCompleteProcessor", (typeof(IFileCompleteProcessorConfig), typeof(FileCompleteProcessor)) },
                { "DataManifestProcessor", (typeof(IDataManifestProcessorConfig), typeof(DataManifestProcessor)) },
                { "CommandMonitor", (typeof(ICommandMonitorConfig), typeof(CommandMonitor)) },
                { "CosmosMonitor", (typeof(ICosmosMonitorConfig), typeof(CosmosMonitor)) },
                { "FileProcessor", (typeof(IFileProcessorConfig), typeof(FileProcessor)) },
                { "QueueMonitor", (typeof(IMonitorTaskConfig), typeof(QueueLengthMonitor)) },
                { "StateCleaner", (typeof(ICleanerConfig), typeof(TableRowCleaner)) },
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
            this.RegisterGlobalSingletons(this.container);
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
        ///     Registers an ITable instace of the specified type
        /// </summary>
        /// <typeparam name="T">row type</typeparam>
        /// <param name="container">unity container</param>
        /// <param name="storage">storage provider</param>
        /// <param name="tableId">table id</param>
        private void RegisterTable<T>(
            IUnityContainer container,
            IAzureStorageProvider storage,
            StateTable tableId)
            where T : class, ITableEntity, ITableEntityInitializer, new()
        {
            string tableName = typeof(T).Name.ToLowerInvariant();

            container.RegisterType<ITable<T>, AzureTable<T>>(
                new HierarchicalLifetimeManager(),
                new InjectionConstructor(storage, this.logger, tableName));

            container.RegisterType<ITable<BasicTableState>, AzureTable<BasicTableState>>(
                tableId.ToStringInvariant(),
                new HierarchicalLifetimeManager(),
                new InjectionConstructor(storage, this.logger, tableName));
        }

        /// <summary>
        ///     Registers an IQueue instace of the specified type
        /// </summary>
        /// <typeparam name="T">row type</typeparam>
        /// <param name="container">unity container</param>
        /// <param name="storage">storage provider</param>
        private void RegisterQueue<T>(
            IUnityContainer container,
            IAzureStorageProvider storage)
            where T : class, new()
        {
            container.RegisterType<IQueue<T>, AzureQueue<T>>(
                new HierarchicalLifetimeManager(),
                new InjectionConstructor(storage, this.logger, typeof(T).Name.ToLowerInvariant()));
        }

        /// <summary>
        ///      Adds an IQueue instace of the specified type to the provided partition queue
        /// </summary>
        /// <typeparam name="TData">queue item data type</typeparam>
        /// <typeparam name="TPartition">partition identifier type</typeparam>
        /// <param name="storage">storage provider</param>
        /// <param name="partition">partition</param>
        /// <param name="partitionedQueue">partitioned queue</param>
        /// <param name="skipPartitionSuffix">true to skip partition suffix; false otherwise</param>
        private void AddQueuePartition<TData, TPartition>(
            IAzureStorageProvider storage,
            TPartition partition,
            PartitionedQueue<TData, TPartition> partitionedQueue,
            bool skipPartitionSuffix)
            where TData : class, new()
        {
            IQueue<TData> queue;
            string queueName = typeof(TData).Name.ToLowerInvariant();

            if (skipPartitionSuffix == false)
            {
                queueName += "-" + partition.ToString().ToLowerInvariant();
            }

            queue = new AzureQueue<TData>(storage, this.logger, queueName);

            partitionedQueue.AddPartition(partition, queue);
        }

        /// <summary>
        ///     Setups the configuration dependent storage provider
        /// </summary>
        /// <param name="container">container</param>
        /// <param name="config">configuration</param>
        private void SetupConfigurationDependentStorage(
            IUnityContainer container,
            ICosmosExportAgentConfig config)
        {
            PartitionedQueue<PendingDataFile, FileSizePartition> partitionedQueue;
            ISecretStoreReader secretReader = container.Resolve<ISecretStoreReader>();
            AzureStorageProvider storage = new AzureStorageProvider(this.logger, secretReader);

            storage.InitializeAsync(config.AzureStorageConfiguration).GetAwaiter().GetResult();

            container.RegisterInstance<IAzureStorageProvider>(storage, new HierarchicalLifetimeManager());

            container.RegisterType<ITableManager, TableManager>();

            this.RegisterTable<ManifestFileSetState>(container, storage, StateTable.ManifestFile);
            this.RegisterTable<CommandFileState>(container, storage, StateTable.CommandFile);
            this.RegisterTable<CommandState>(container, storage, StateTable.Command);

            this.RegisterQueue<CompleteDataFile>(container, storage);
            this.RegisterQueue<ManifestFileSet>(container, storage);

            partitionedQueue = new PartitionedQueue<PendingDataFile, FileSizePartition>();

            this.AddQueuePartition(storage, FileSizePartition.Empty, partitionedQueue, false);
            this.AddQueuePartition(storage, FileSizePartition.Small, partitionedQueue, false);
            this.AddQueuePartition(storage, FileSizePartition.Medium, partitionedQueue, false);
            this.AddQueuePartition(storage, FileSizePartition.Oversize, partitionedQueue, false);

            // skip the partition suffix on the large queue to avoid backwards compatibility issues so we don't have to
            //  add more code to handle the old queue + new queues. We consider all files in this queue as 'large'
            //  becuase it would be bad to block up a lower size queue to handle any pending large files
            this.AddQueuePartition(storage, FileSizePartition.Large, partitionedQueue, true);

            partitionedQueue.SetQueueMode();

            container.RegisterInstance<IPartitionedQueue<PendingDataFile, FileSizePartition>>(
                partitionedQueue,
                new HierarchicalLifetimeManager());

            container.RegisterType<ILockManager, AzureTableLockManager>(
                new HierarchicalLifetimeManager(),
                new InjectionConstructor(
                    storage, 
                    this.logger, 
                    container.Resolve<IClock>(), 
                    config.LockTableName));
        }

        /// <summary>
        ///     Registers the configuration dependent singletons
        /// </summary>
        private void RegisterConfigurationDependentSingletons(
            IUnityContainer container,
            IPrivacyConfigurationManager config)
        {
            void RegisterTaskConfigType<T>(ICollection<ITaskConfig> configs)
                where T : ITaskConfig
            {
                T cfg = configs.OfType<T>().FirstOrDefault();
                if (cfg != null)
                {
                    container.RegisterInstance<T>(cfg);
                }
            }

            this.logger.Information(nameof(DependencyManager), "Begin dependent singleton configuration");

            container.RegisterInstance(config);
            container.RegisterType<ISecretStoreReader, AzureKeyVaultReader>(new HierarchicalLifetimeManager());
            container.RegisterInstance<IAadTokenAuthConfiguration>(config.AadTokenAuthGeneratorConfiguration);
            container.RegisterInstance<ICosmosExportAgentConfig>(config.CosmosExportAgentConfig);

            RegisterTaskConfigType<IDataManifestProcessorConfig>(config.CosmosExportAgentConfig.Tasks.Values);
            RegisterTaskConfigType<ICommandMonitorConfig>(config.CosmosExportAgentConfig.Tasks.Values);

            container.RegisterInstance<ICosmosRelativePathsAndExpiryTimes>(config.CosmosExportAgentConfig.CosmosPathsAndExpiryTimes);
            container.RegisterInstance<ICosmosFileSizeThresholds>(config.CosmosExportAgentConfig.FileSizeThresholds);

            this.SetupConfigurationDependentStorage(container, config.CosmosExportAgentConfig);

            container.RegisterType<IPcfHttpClientFactory, CommandHttpClientFactory>(
                new HierarchicalLifetimeManager(),
                new InjectionConstructor(
                    config.CosmosExportAgentConfig.PcfEndpointConfig,
                    container.Resolve<IPxsHttpClientFactory>(),
                    container.Resolve<ICounterFactory>()));

            container.RegisterType<IPeriodicFileWriterFactory, PeriodicFileWriterFactory>(new HierarchicalLifetimeManager());

            container.RegisterType<ICommandObjectFactory, CommandObjectFactory>(new HierarchicalLifetimeManager());
            
            this.container.RegisterType<IFileSystemManager, CosmosFileSystemManager>(new HierarchicalLifetimeManager());

            container.RegisterType<ICosmosExportPipelineFactory, CosmosExportPipelineFactory>(new HierarchicalLifetimeManager());
            container.RegisterType<ICommandDataWriterFactory, CommandDataWriterFactory>(new HierarchicalLifetimeManager());

            container.RegisterType<IAadAuthManager, AadAuthManager>(new HierarchicalLifetimeManager());
            container.RegisterType<IPcfAdapter, PcfAdapter>(new HierarchicalLifetimeManager());

            container.RegisterType<IFileProgressTrackerFactory, FileProgressTrackerFactory>(new HierarchicalLifetimeManager());
            
            // app config manager
            container.RegisterType<IAppConfiguration, AppConfiguration>(
                new ContainerControlledLifetimeManager(),
                new InjectionFactory(
                    c => AppConfigurationFactory.Create(c.Resolve<IPrivacyConfigurationManager>())));

            container.RegisterType<IRequestCommandUtilities, RequestCommandUtilities>(
                new HierarchicalLifetimeManager(),
                new InjectionConstructor(
                    container.Resolve<ICommandMonitorConfig>(),
                    container.Resolve<ICommandObjectFactory>(),
                    container.Resolve<ITable<CommandState>>(),
                    container.Resolve<IAppConfiguration>()));


            // register each of the tasks based on what we have in the config
            foreach (ITaskConfig taskConfig in config.CosmosExportAgentConfig.Tasks.Values)
            {
                (Type Config, Type Worker) types = DependencyManager.ConfigTypeMap[taskConfig.TaskType];

                container.RegisterType(
                    typeof(IBackgroundTask),
                    types.Worker,
                    taskConfig.Tag,
                    new HierarchicalLifetimeManager(),
                    new InjectionFactory(c => c.Resolve(types.Worker, new DependencyOverride(types.Config, taskConfig))));
            }

            this.logger.Information(nameof(DependencyManager), "Completed dependent singleton configuration");
        }

        /// <summary>
        ///     Registers the global singletons
        /// </summary>
        private void RegisterGlobalSingletons(IUnityContainer container)
        {
            // Global singletons should be 'ContainerControlled' so if config reloads they do not get recreated in a child container.

            this.logger.Information(nameof(DependencyManager), "Begin global singleton configuration");

            JsonSerializerSettingsForWorkers.SetupJsonSerializerSettings(container);

            container.RegisterType<ICosmosResourceFactory, CosmosResourceFactory>(new ContainerControlledLifetimeManager());

            container.RegisterType<ITelemetryLogger, TelemetryLogger>(new ContainerControlledLifetimeManager());

            container.RegisterType<ICounterFactory, NoOpCounterFactory> (new ContainerControlledLifetimeManager());

            container.RegisterType<ICertificateProvider, CertificateProvider>(new ContainerControlledLifetimeManager());

            container.RegisterInstance(Policies.Current, new ContainerControlledLifetimeManager());

            container.RegisterType<IExportStorageProvider, ExportStorageProvider>(new ContainerControlledLifetimeManager());

            container.RegisterType<ILoggingFilter, AlwaysSkipLoggingFilter>(new ContainerControlledLifetimeManager());
            container.RegisterType<IPxsHttpClientFactory, HttpClientFactoryPublic>(new ContainerControlledLifetimeManager());

            container.RegisterType<IRandom, ThreadSafeRandom>(new ContainerControlledLifetimeManager());
            container.RegisterType<IClock, Clock>(new ContainerControlledLifetimeManager());

            container.RegisterType<CommandFeedLogger, ExportCommandLogger>(new ContainerControlledLifetimeManager());

            container.RegisterType<ITokenManager, InstrumentedAadTokenManager>(new ContainerControlledLifetimeManager(), new InjectionConstructor(new AadTokenManager()));
            container.RegisterType<IAadJwtSecurityTokenHandler, AadJwtSecurityTokenHandler>(new ContainerControlledLifetimeManager());
            // Mise token validation
            this.container.RegisterType<IMiseTokenValidationUtility, MiseTokenValidationUtility>(new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<ILogger>(),
                    new ResolvedParameter<IPrivacyConfigurationManager>()));

            this.logger.Information(nameof(DependencyManager), "Completed global singleton configuration");
        }

        private class AlwaysSkipLoggingFilter : ILoggingFilter
        {
            public bool ShouldLogDetailsForUser(string identity) => false;
        }
    }
}
