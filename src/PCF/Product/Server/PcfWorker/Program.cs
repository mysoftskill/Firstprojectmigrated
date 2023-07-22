namespace Microsoft.PrivacyServices.CommandFeed.Service.PcfWorker
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.NonWindowsDeviceDeleteWorker;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.CommandStatusAggregation;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.DeleteExportArchive;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandHistory;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue.QueueStorageCommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandReplay;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Cosmos;
    using Microsoft.PrivacyServices.CommandFeed.Service.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.CommandFeedv2.Common.Storage;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Common.Cosmos;

    /// <summary>
    /// The Pcf worker.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public sealed class Program : PrivacyApplication
    {
        private Program() : base(CommandFeedService.Worker)
        {
        }

        protected override void OnStart()
        {
            base.OnStart();

            GenevaHelper.Initialize(TraceTagPrefixes.ADGCS_PCF);

            var logger = DualLogger.Instance;
            DualLogger.AddTraceListener();

            logger.Information(nameof(Program), "Start PcfWorker.");

            logger.Information(nameof(Program), "Create SllLogger.");

            var sllLogger = new SllLogger();
            Logger.Instance = sllLogger;

            logger.Information(nameof(Program), "Initialize Cosmos Client");
            ICosmosClient cosmosClient = CosmosClientFactory.InitializeCosmosClientAsync(PcfWorkerGlobals.CosmosResourceFactory).GetAwaiter().GetResult();

            logger.Information(nameof(Program), "Create Event publisher ...");
            PcfWorkerGlobals.EventPublisher = new CommandLifecycleEventPublisher();

            logger.Information(nameof(Program), "Create Data agent map...");
            var docDb = new OnDiskAssetGroupInfoCollection(DocDbAssetGroupInfoCollection.CreateAsync().Result);
            PcfWorkerGlobals.DataAgentMapFactory = new DataAgentMapFactory(docDb);
            PcfWorkerGlobals.DataAgentMapFactory.InitializeAsync().Wait();

            logger.Information(nameof(Program), "Create the app configuration");
            PcfWorkerGlobals.AppConfiguration = EnvironmentInfo.HostingEnvironment.CreateAppConfiguration(DualLogger.Instance);

            logger.Information(nameof(Program), "Create the cosmos db client factory");
            PcfWorkerGlobals.CosmosDbClientFactory = new CosmosDbClientFactory();

            logger.Information(nameof(Program), "Create CommandReplay repository");
            PcfWorkerGlobals.ReplayJobRepo = new CommandReplayJobRepository();
            PcfWorkerGlobals.ReplayJobRepo.InitializeAsync().Wait();

            logger.Information(nameof(Program), "Create cosmos DB context");
            PcfWorkerGlobals.CosmosDbContext = CosmosDbContext.FromConfiguration().Result;

            logger.Information(nameof(Program), "Creating Azure Queue Storage Context");
            PcfWorkerGlobals.AzureQueueStorageCommandContext = new AzureQueueStorageContext();

            logger.Information(nameof(Program), "Create Command Queue Factory");
            PcfWorkerGlobals.CommandQueueFactory = new CommandQueueFactory(PcfWorkerGlobals.CosmosDbContext, PcfWorkerGlobals.AzureQueueStorageCommandContext, PcfWorkerGlobals.Clock, PcfWorkerGlobals.QueueTrackerCache);

            logger.Information(nameof(Program), "Create command history transition repository");
            PcfWorkerGlobals.CommandHistory = CommandHistoryRepository.CreateAsync(PcfWorkerGlobals.CommandQueueFactory, PcfWorkerGlobals.AppConfiguration).Result;

            logger.Information(nameof(Program), "Create Redis Client");
            var redisConnection = new RedisConnection(
                "PCF worker",
                Config.Instance.AzureManagement.RedisCacheEndpoint,
                (int)Config.Instance.AzureManagement.RedisCachePort,
                Config.Instance.AzureManagement.RedisCachePassword,
                logger);
            PcfWorkerGlobals.RedisClient = new RedisClient(redisConnection, logger);

            logger.Information(nameof(Program), "Initialize validation service...");
            PcvEnvironment validationEnvironment = PcvEnvironment.Production;
            if (!Config.Instance.Common.IsProductionEnvironment)
            {
                validationEnvironment = PcvEnvironment.Preproduction;
            }

            // todo refactor this so we dont need to enumerate but use the CloudInstance.All
            // Bug# 17299120
            PcfWorkerGlobals.CommandValidationService = new ValidationService(validationEnvironment);

            logger.Information(nameof(Program), "Start Pdms Cache refresh thread.");
            this.AddTask(PcfWorkerGlobals.DataAgentMapFactory.RefreshAsync(this.CancellationToken));

            logger.Information(nameof(Program), "Create ExportStorageCleanupTask.");
            var exportStorageCleanupTask = new ExportStorageCleanupTask();

            logger.Information(nameof(Program), "Create ForceCompleteOldExportCommandsTask.");
            var forceCompleteExportTask = new OldExportForceCompleteTask(
                PcfWorkerGlobals.CommandHistory,
                PcfWorkerGlobals.EventPublisher,
                PcfWorkerGlobals.DataAgentMapFactory,
                PcfWorkerGlobals.CosmosDbClientFactory,
                PcfWorkerGlobals.AppConfiguration);

            PcfWorkerGlobals.KustoClient = new KustoClient(Config.Instance.Kusto.ClusterName, Config.Instance.Kusto.DatabaseName);

            CosmosStreamWriter.InitializeStreamWriters(cosmosClient);

            logger.Information(nameof(Program), "Create PDMS refresh task");
            var pdmsRefreshTask = new PdmsDataRefreshTask(cosmosClient, PcfWorkerGlobals.CosmosResourceFactory, PcfWorkerGlobals.AppConfiguration);

            logger.Information(nameof(Program), "Build command cold storage aggregation pipeline");
            this.AddTask(ConfigureAzureQueuesAndEventHubs(this.CancellationToken));

            if (Config.Instance.Worker.CosmosPipelineEnabled)
            {
                logger.Information(nameof(Program), "Build cosmos pipeline");
                this.AddTask(StartCosmosPipelineAsync());
                logger.Information(nameof(Program), "Create Cosmos Stream Hourly Completion task");
                var cosmosStreamHourlyCompletionTask = new CosmosStreamHourlyCompletionTask();

                logger.Information(nameof(Program), "Start CosmosStreamHourlyCompletionTask.");
                this.AddTask(cosmosStreamHourlyCompletionTask.StartAsync(
                    () => null,
                    this.CancellationToken));
            }
            else
            {
                logger.Information(nameof(Program), "Skip cosmos pipeline");
            }

            logger.Information(nameof(Program), "Start PdmsCacheRefreshTask.");
            this.AddTask(pdmsRefreshTask.StartAsync(
                () => null,
                this.CancellationToken));

            logger.Information(nameof(Program), "Start ExportStorageCleanupTask.");
            this.AddTask(exportStorageCleanupTask.StartAsync(
                () => null,
                this.CancellationToken));

            logger.Information(nameof(Program), "Start ForceCompleteOldExportCommandsTask");
            this.AddTask(forceCompleteExportTask.StartAsync(
                () => null,
                this.CancellationToken));

            logger.Information(nameof(Program), "Build command replay pipeline");
            this.AddTask(StartCommandReplayPipeline(this.CancellationToken));

            logger.Information(nameof(Program), "Build command ingestion recovery pipeline");
            this.AddTask(StartCommandIngestionRecoveryPipeline(this.CancellationToken));

            // Do not start NonWindowsDeviceDelete if disabled.
            if (NonWindowsDeviceDeleteHelpers.IsNonWindowsDeviceDeleteEnabled())
            {
                // Run NonWindowsDevice Deletes Worker in non-PROD until its ready
                this.AddTask(StartNonWindowsDeviceDeletePipeline(this.CancellationToken));
            }

            // Temporary hotfix to drain AuditLogger retry queue
            logger.Information(nameof(Program), "Drain AuditLogger retry queue.");
            this.AddTask(StartProcessEventHubRetryQueue(this.CancellationToken));
        }

        private static Task StartProcessEventHubRetryQueue(CancellationToken cancellationToken)
        {
            List<Task> taskItems = new List<Task>();
            
            var handler = new EventHubRetryQueueHandler("auditlog");
            ICommandLifecycleCheckpointProcessor processor = new AuditLogReceiver();

            taskItems.Add(handler.RunEventHubRetryQueueBackupAsync(cancellationToken));
            taskItems.Add(handler.ProcessEventHubRetryQueueAsync(processor, cancellationToken));

            if (FlightingUtilities.IsEnabled(FeatureNames.PCF.RawCommandProcessor))
            {
                handler = new EventHubRetryQueueHandler("rawcommand");
                processor = new CommandRawDataReceiver();

                taskItems.Add(handler.RunEventHubRetryQueueBackupAsync(cancellationToken));
                taskItems.Add(handler.ProcessEventHubRetryQueueAsync(processor, cancellationToken));
            }

            return Task.WhenAll(taskItems);
        }

        /// <summary>
        /// The cold storage aggregation pipeline proceeds as follows:
        /// 1) Lifecycle events are written to event hub
        /// 2) We consume them from event hub, and aggregate them into smaller work items on a per-command ID basis
        /// 3) These work items are inserted into Azure queues so that they can be retried independently (event hub is bad at this).
        /// 4) These work items run, and if it looks like everything has finished for a given command, then they queue another (different!) work
        ///    item that runs in the future to check for completion and raise a notification.
        ///    
        /// Does this sound complicated? Think about it as map reduce. Event Hub is the reducer / aggregator, which then schedules the reduced work for processing.
        /// </summary>
        private static async Task ConfigureAzureQueuesAndEventHubs(CancellationToken cancellationToken)
        {
            // Initialize our queues. 
            var checkCompletionQueue = new AzureWorkItemQueue<CheckCompletionWorkItem>();
            var statusQueue = new AzureWorkItemQueue<CommandStatusBatchWorkItem>();
            var filterQueue = new AzureWorkItemQueue<FilterAndRouteCommandWorkItem>(BaseFilterAndRouteCommandWorkItemHandler.QueueName);
            var whatIfFilterQueue = new AzureWorkItemQueue<FilterAndRouteCommandWorkItem>(BaseFilterAndRouteCommandWorkItemHandler.WhatIfQueueName);
            var expandQueue = new AzureWorkItemQueue<PublishCommandBatchWorkItem>();
            var deleteQueue = new AzureWorkItemQueue<DeleteFromQueueWorkItem>();
            var insertIntoQueue = new AzureWorkItemQueue<InsertIntoQueueWorkItem>();
            var batchCheckpointCompleteQueue = new AzureWorkItemQueue<BatchCheckpointCompleteWorkItem>();
            var deleteFullExportArchiveQueue = new AzureWorkItemQueue<DeleteFullExportArchiveWorkItem>();

            // todo rework this and put the default least period in config
            var flushQueue = new AzureWorkItemQueue<AgentQueueFlushWorkItem>(TimeSpan.FromMinutes(15));

            // Initialize the queue handlers. These do the work of processing the queue messages.
            var checkCompletionQueueHandler = new CheckCompletionWorkItemQueueHandler(PcfWorkerGlobals.CommandHistory, PcfWorkerGlobals.KustoClient, PcfWorkerGlobals.AppConfiguration, PcfWorkerGlobals.CosmosDbClientFactory, new PCFV2ExportInfoProvider());
            var statusQueueHandler = new CommandStatusBatchWorkItemQueueHandler(checkCompletionQueue, PcfWorkerGlobals.CommandHistory);
            var filterHandler = new FilterAndRouteCommandWorkItemHandler(PcfWorkerGlobals.CommandHistory, PcfWorkerGlobals.EventPublisher, PcfWorkerGlobals.DataAgentMapFactory, insertIntoQueue, PcfWorkerGlobals.RedisClient);
            var expandQueueHandler = new PublishCommandBatchWorkItemHandler(filterQueue, whatIfFilterQueue);
            var deleteQueueHandler = new DeleteFromQueueWorkItemHandler(PcfWorkerGlobals.CommandQueueFactory);
            var batchCheckpointCompleteHandler = new BatchCheckpointCompleteWorkItemHandler(batchCheckpointCompleteQueue, deleteQueue);
            var agentFlushHandler = new AgentQueueFlushWorkItemHandler(PcfWorkerGlobals.CommandQueueFactory, PcfWorkerGlobals.DataAgentMapFactory);
            var insertIntoQueueHandler = new InsertIntoQueueWorkItemHandler(insertIntoQueue, PcfWorkerGlobals.EventPublisher, PcfWorkerGlobals.CommandQueueFactory, PcfWorkerGlobals.DataAgentMapFactory);
            var deleteFullExportArchiveHandler = new DeleteFullExportArchiveWorkItemHandler(PcfWorkerGlobals.CommandHistory);

            List<Task> items = new List<Task>();

            items.Add(checkCompletionQueue.BeginProcessAsync(checkCompletionQueueHandler, cancellationToken));
            items.Add(statusQueue.BeginProcessAsync(statusQueueHandler, cancellationToken));
            items.Add(filterQueue.BeginProcessAsync(filterHandler, cancellationToken));
            items.Add(expandQueue.BeginProcessAsync(expandQueueHandler, cancellationToken));
            items.Add(deleteQueue.BeginProcessAsync(deleteQueueHandler, cancellationToken));
            items.Add(flushQueue.BeginProcessAsync(agentFlushHandler, cancellationToken));
            items.Add(insertIntoQueue.BeginProcessAsync(insertIntoQueueHandler, cancellationToken));
            items.Add(batchCheckpointCompleteQueue.BeginProcessAsync(batchCheckpointCompleteHandler, cancellationToken));
            items.Add(deleteFullExportArchiveQueue.BeginProcessAsync(deleteFullExportArchiveHandler,cancellationToken));

            Configuration_CommandLifecycle_EventHub config = Config.Instance.CommandLifecycle.EventHub;
            foreach (var instance in config.Instances)
            {
                // Initialize Event Hub receivers
                CommandLifecycleEventReceiver receiver = new CommandLifecycleEventReceiver(
                    instance.LeaseContainerName,
                    "coldstorage",
                    instance,
                    () => new CommandHistoryAggregationReceiver(statusQueue,instance.Moniker, cancellationToken),
                    SemaphorePriority.RealTime);

                items.Add(receiver.StartReceivingAsync());
            }

            await Task.WhenAll(items);
        }

        private static Task StartCosmosPipelineAsync()
        {
            List<Task> items = new List<Task>();
            Configuration_CommandLifecycle_EventHub config = Config.Instance.CommandLifecycle.EventHub;
            foreach (var instance in config.Instances)
            {
                // Initialize Event Hub receivers
                CommandLifecycleEventReceiver auditLogReceiver = new CommandLifecycleEventReceiver(
                    instance.LeaseContainerName,
                    "auditlog",
                    instance,
                    () => new AuditLogReceiver());

                items.Add(auditLogReceiver.StartReceivingAsync());

                if (FlightingUtilities.IsEnabled(FeatureNames.PCF.RawCommandProcessor))
                {
                    // Initialize Event Hub receivers
                    CommandLifecycleEventReceiver commandRawDataReceiver = new CommandLifecycleEventReceiver(
                    instance.LeaseContainerName,
                    "rawcommand",
                    instance,
                    () => new CommandRawDataReceiver());

                    items.Add(commandRawDataReceiver.StartReceivingAsync());
                }
            }

            return Task.WhenAll(items);
        }

        private static Task StartCommandReplayPipeline(CancellationToken cancellationToken)
        {
            // Initialize our queues. 
            // Set BatchSize to 1 to avoid too many instance updating the same Docdb record at same time
            var replayRequestQueue = new AzureWorkItemQueue<ReplayRequestWorkItem> { BatchSize = 1 };
            var commandEnqueueBatchQueue = new AzureWorkItemQueue<EnqueueBatchReplayCommandsWorkItem>(TimeSpan.FromMinutes(20));

            // Initialize the queue handlers. These do the work of processing the queue messages.
            var replayRequestQueueHandler = new ReplayRequestWorkItemHandler(PcfWorkerGlobals.ReplayJobRepo);
            var commandEnqueueBatchHandler = new EnqueueBatchReplayCommandsWorkItemHandler(
                PcfWorkerGlobals.DataAgentMapFactory,
                PcfWorkerGlobals.CommandQueueFactory,
                PcfWorkerGlobals.EventPublisher,
                PcfWorkerGlobals.AzureQueueStorageCommandContext);

            // Initialize the command replay worker
            var replayWorker = new ReplayWorker(
                PcfWorkerGlobals.DataAgentMapFactory,
                PcfWorkerGlobals.ReplayJobRepo,
                PcfWorkerGlobals.CommandHistory,
                PcfWorkerGlobals.CommandValidationService,
                commandEnqueueBatchQueue);

            List<Task> items = new List<Task>();
            items.Add(replayRequestQueue.BeginProcessAsync(replayRequestQueueHandler, cancellationToken));
            items.Add(commandEnqueueBatchQueue.BeginProcessAsync(commandEnqueueBatchHandler, cancellationToken));
            items.Add(replayWorker.BeginProcessAsync(cancellationToken));

            return Task.WhenAll(items);
        }

        private static Task StartCommandIngestionRecoveryPipeline(CancellationToken cancellationToken)
        {
            var taskItems = new List<Task>();
            var ingestionRecoveryQueue = new AzureWorkItemQueue<IngestionRecoveryWorkItem>();
            var insertIntoQueue = new AzureWorkItemQueue<InsertIntoQueueWorkItem>();
            var ingestionRecoveryQueueHandler = new IngestionRecoveryWorkItemHandler(insertIntoQueue, PcfWorkerGlobals.CommandHistory, PcfWorkerGlobals.DataAgentMapFactory, PcfWorkerGlobals.KustoClient);
            taskItems.Add(ingestionRecoveryQueue.BeginProcessAsync(ingestionRecoveryQueueHandler, cancellationToken));

            DualLogger.Instance.Information(nameof(Program), "Create IngestionRecoveryTask");
            var ingestionRecoveryScheduledTask = new IngestionRecoveryTask(ingestionRecoveryQueue);

            DualLogger.Instance.Information(nameof(Program), "Start IngestionRecoveryTask");
            taskItems.Add(ingestionRecoveryScheduledTask.StartAsync(() => null, cancellationToken));
            return Task.WhenAll(taskItems);
        }

        private static Task StartNonWindowsDeviceDeletePipeline(CancellationToken cancellationToken)
        {
            DualLogger.Instance.Information(nameof(StartNonWindowsDeviceDeletePipeline), "Starting NonWindowsDevice Deletes Pipeline");

            var deviceIdCache = new DeviceIdCache(TimeSpan.FromMinutes(Config.Instance.Worker.Tasks.NonWindowsDeviceWorker.DeviceIdExpirationMinutes));
            IDeleteRequestsProcessor deleteRequestsProcessor = new DeleteRequestsProcessor(
                commandBatchWorkItemPublisher: PcfWorkerGlobals.CommandBatchWorkItemPublisher,
                dataAgentMapFactory: PcfWorkerGlobals.DataAgentMapFactory,
                lifecycleEventPublisher: PcfWorkerGlobals.EventPublisher,
                deviceIdCache: deviceIdCache);

            IEventHubConfig eventHubConfig = Config.Instance.Worker.Tasks.NonWindowsDeviceWorker.EventHubConfig;
            IEventHubProcessor eventHubProcessor = new EventHubProcessor(eventHubConfig);
            IEventHubProcessorHandler eventHubProcessorHandler = new EventHubProcessorHandler(deleteRequestsProcessor, eventHubConfig);

            Task processorTask = eventHubProcessor.RunAsync(eventHubProcessorHandler, cancellationToken);
            return processorTask;
        }

        public static void Main(string[] args)
        {
            Program program = new Program();
            program.Run(args);
        }
    }
}
