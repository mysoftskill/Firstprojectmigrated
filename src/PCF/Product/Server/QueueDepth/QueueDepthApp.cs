namespace Microsoft.PrivacyServices.CommandFeed.Service.QueueDepth
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.ServiceModel.Syndication;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.QueueDepth;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue.QueueStorageCommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen;
    using Microsoft.PrivacyServices.CommandFeed.Service.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.Repository;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// PCF Autopilot App
    /// Hosting Command Queue Depth Calculation Tasks
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class QueueDepthApp : PrivacyApplication
    {
        // Service parameters
        private static readonly TimeSpan LifecycleCheckpointFrequency = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan KustoAggregationWorkerFrequency = TimeSpan.FromMinutes(5);

        private const string BaselineSchedulerTaskName = "BaselineScheduler";
        private const string KustoAggregationWorkerTaskName = "KustoAggregationWorker";
        private const string CosmosDbPartitionSizeWorkerTaskName = "CosmosDbPartitionSizeWorker";

        /// <summary>
        /// Private constructor
        /// </summary>
        private QueueDepthApp() : base(CommandFeedService.QueueDepth)
        {
        }

        /// <summary>
        /// Run Autopilot app
        /// </summary>
        public static void Main(string[] args)
        {
            Console.WriteLine("*****************************************************************");
            Console.WriteLine($"PCF_EnvironmentName:{Environment.GetEnvironmentVariable("PCF_EnvironmentName")}");
            Console.WriteLine("*****************************************************************");
            QueueDepthApp queueDepthApp = new QueueDepthApp();
            queueDepthApp.Run(args);
        }

        /// <inheritdoc />
        protected override void OnStart()
        {
            Logger.Instance = new SllLogger(10L * 1024 * 1024, 10);

            var clock = new Clock();
            var daFactory = this.InitializeDataAgentMapFactory();
            var telemetryRepository = InitializeTelemetryRepository();
            var cqFactory = this.InitializeCommandQueueFactory(clock);

            this.InitializeKustoAggregationWorker(telemetryRepository, daFactory);
            this.InitializeBaselineWorker(telemetryRepository, daFactory);
            this.InitializeCommandLifecycleProcessor(telemetryRepository);
            this.InitializeAzureStorageQueueDepthChecker(daFactory, cqFactory, telemetryRepository, clock);
            this.InitializeCosmosDbPartitionSizeWorker(telemetryRepository);
        }

        private static ITelemetryRepository InitializeTelemetryRepository()
        {
            DualLogger.Instance.Information(nameof(QueueDepthApp), "Initialize telemetry repository.");
            ITelemetryRepository telemetryRepository = new KustoTelemetryRepository();

            // reduce Kusto tracing noise in debug logs
            Kusto.Cloud.Platform.Utils.TraceSourceManager.SetTraceVerbosityForAll(
                Kusto.Cloud.Platform.Utils.TraceVerbosity.Error);

            return telemetryRepository;
        }

        private IDataAgentMapFactory InitializeDataAgentMapFactory()
        {
            DualLogger.Instance.Information(nameof(QueueDepthApp), "Initialize PDMS cache.");

            var docDb = DocDbAssetGroupInfoCollection.CreateAsync().Result;
            var daFactory = new DataAgentMapFactory(docDb);
            this.AddTask(daFactory.RefreshAsync(this.CancellationToken));
            daFactory.InitializeAsync().Wait();

            return daFactory;
        }

        private ICommandQueueFactory InitializeCommandQueueFactory(IClock clock)
        {
            DualLogger.Instance.Information(nameof(QueueDepthApp), "Initialize command queue factory.");

            var context = CosmosDbContext.FromConfiguration().Result;
            return new CommandQueueFactory(context, new AzureQueueStorageContext(), clock, new AssetGroupAzureQueueTrackerCache());
        }

        private void InitializeAzureStorageQueueDepthChecker(IDataAgentMapFactory daFactory, ICommandQueueFactory cqFactory, ITelemetryRepository telRepo, IClock clock)
        {
            if (Config.Instance.Worker.Tasks.AzureQueueCommandQueueDepth.CommonConfig.Enabled)
            {
                DualLogger.Instance.Information(nameof(QueueDepthApp), "Initialize azure storage command queue depth checker.");
                var checker = new AzureQueueCommandQueueDepthChecker(daFactory, cqFactory, telRepo, clock);
                this.AddTask(checker.StartAsync(() => null, this.CancellationToken));
            }
            else
            {
                DualLogger.Instance.Information(nameof(QueueDepthApp), "Skip azure storage command queue depth checker.");
            }
        }

        private void InitializeBaselineWorker(ITelemetryRepository telemetryRepository, IDataAgentMapFactory daFactory)
        {
            var baselineRefreshFrequency = TimeSpan.FromMinutes(Config.Instance.Worker.Tasks.BaselineQueueDepthTaskScheduler.FrequencyMinutes);

            DualLogger.Instance.Information(nameof(QueueDepthApp), $"Initialize queue depth base line worker. Baseline refresh frequency={baselineRefreshFrequency}");

            var publishers = new Dictionary<SubjectType, IAzureWorkItemQueuePublisher<QueueDepthWorkItem>>();
            foreach (var subject in (SubjectType[])Enum.GetValues(typeof(SubjectType)))
            {
                var queue = new AzureWorkItemQueue<QueueDepthWorkItem>(QueueDepthWorkItem.BaselineQueueLeaseTime, $"{QueueDepthWorkItem.BaselineTasksQueueName}{subject}")
                {
                    // number of async tasks
                    SoftPendingWorkItemLimit = 20,
                };
                this.AddTask(queue.BeginProcessAsync(
                    new QueueDepthWorkItemHandler(telemetryRepository),
                    this.CancellationToken));
                publishers.Add(subject, queue);
            }

            var config = Config.Instance.Worker.Tasks.BaselineQueueDepthTaskScheduler.CommonConfig;
            config.BatchSize = Math.Max(config.BatchSize, Environment.ProcessorCount);
            var baselineTasksScheduler = new QueueDepthTaskScheduler(
                BaselineSchedulerTaskName,
                config,
                baselineRefreshFrequency)
            {
                Publishers = publishers,
                DataAgentFactory = daFactory,
                TelemetryRepository = telemetryRepository,
            };

            this.AddTask(baselineTasksScheduler.StartAsync(() => null, this.CancellationToken));
        }

        private void InitializeCommandLifecycleProcessor(ITelemetryRepository telemetryRepository)
        {
            DualLogger.Instance.Information(nameof(QueueDepthApp), $"Initialize Telemetry CommandLifecycleProcessor(s). Checkpoint frequency={LifecycleCheckpointFrequency}.");

            // Initialize Event Hub receivers
            Configuration_CommandLifecycle_EventHub config = Config.Instance.CommandLifecycle.EventHub;
            foreach (var instance in config.Instances)
            {
                // Initialize Event Hub receivers
                CommandLifecycleEventReceiver receiver = new CommandLifecycleEventReceiver(
                    instance.LeaseContainerName,
                    "telemetry",
                    instance,
                    () => new TelemetryLifecycleCheckpointProcessor(telemetryRepository, LifecycleCheckpointFrequency));

                this.AddTask(receiver.StartReceivingAsync());
            }
        }

        private void InitializeKustoAggregationWorker(ITelemetryRepository telemetryRepository, IDataAgentMapFactory dataAgentMapFactory)
        {
            var frequency = KustoAggregationWorkerFrequency;

            DualLogger.Instance.Information(nameof(QueueDepthApp), $"Initialize KustoAggregationWorker. Frequency={frequency}.");

            var config = Config.Instance.Worker.Tasks.KustoAggregationWorker.CommonConfig;
            config.BatchSize = Math.Max(config.BatchSize, Environment.ProcessorCount);

            var worker = new KustoAggregationWorker(
                KustoAggregationWorkerTaskName,
                config,
                frequency)
            {
                TelemetryRepository = telemetryRepository,
                DataAgentMapFactory = dataAgentMapFactory,
            };

            this.AddTask(worker.StartAsync(() => null, this.CancellationToken));
        }

        private void InitializeCosmosDbPartitionSizeWorker(ITelemetryRepository telemetryRepository)
        {
            DualLogger.Instance.Information(nameof(QueueDepthApp), $"Initialize CosmosDbPartitionSizeWorker.");

            var redisConnection = new RedisConnection(
                "CosmosDb PartitionSizeWorker",
                Config.Instance.AzureManagement.RedisCacheEndpoint,
                (int)Config.Instance.AzureManagement.RedisCachePort,
                Config.Instance.AzureManagement.RedisCachePassword,
                DualLogger.Instance);

            var redisClient = new RedisClient(redisConnection, DualLogger.Instance);
            redisClient.SetDatabaseNumber(RedisDatabaseId.CosmosDbPartitionSizeWorkerData);

            var worker = new CosmosDbPartitionSizeWorker(
                redisClient,
                telemetryRepository,
                workspaceId: Config.Instance.Worker.Tasks.CosmosDbPartitionSizeWorker.AzureLogAnalyticsWorkspaceId,
                dbResourceGroup: Config.Instance.Worker.Tasks.CosmosDbPartitionSizeWorker.CosmosDbResourceGroup,
                taskConfig: Config.Instance.Worker.Tasks.CosmosDbPartitionSizeWorker.CommonConfig,
                taskName: CosmosDbPartitionSizeWorkerTaskName,
                workerFrequency: TimeSpan.FromMinutes(Config.Instance.Worker.Tasks.CosmosDbPartitionSizeWorker.WorkerFrequencyMinutes),
                globalFrequency: TimeSpan.FromMinutes(Config.Instance.Worker.Tasks.CosmosDbPartitionSizeWorker.GlobalFrequencyMinutes));

            this.AddTask(worker.StartAsync(() => null, this.CancellationToken));
        }
    }
}