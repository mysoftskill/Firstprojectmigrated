namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Web.Http;

    using global::Owin;
    using Microsoft.Azure.ComplianceServices.Common.Owin;
    using Microsoft.Owin.Builder;
    using Microsoft.Owin.Host.HttpListener;
    using Microsoft.Owin.Hosting;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.DeleteExportArchive;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandHistory;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue.QueueStorageCommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Cosmos;
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor.ApiTrafficThrottling;
    using Microsoft.PrivacyServices.CommandFeed.Service.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.Repository;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.CommandFeedv2.Common.Storage;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Entry for PCF service.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    [ExcludeFromCodeCoverage]
    public class Program : PrivacyApplication
    {
        private readonly List<IDisposable> webapiServers = new List<IDisposable>();

        /// <summary>
        /// Initializes a new instance of the class <see cref="Program" />.
        /// </summary>
        protected Program() : base(CommandFeedService.Frontdoor)
        {
        }

        /// <summary>
        /// Main function.
        /// </summary>
        /// <param name="args">Command line args.</param>
        public static void Main(string[] args)
        {
            Program program = new Program();
            program.Run(args);
        }

        /// <inheritdoc />
        [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "server")]
        protected override void OnStart()
        {
            var logger = DualLogger.Instance;
            DualLogger.AddTraceListener();
            logger.Information(nameof(Program), "OnStart");
            base.OnStart();

            logger.Information(nameof(Program), "Create SllLogger.");

            var sllLogger = new SllLogger();
            Logger.Instance = sllLogger;

            logger.Information(nameof(Program), "Create CosmosDbCommandQueueContext.");
            var context = CosmosDbContext.FromConfiguration().Result;
            CommandFeedGlobals.CosmosDbCommandQueueContext = context;
            CommandFeedGlobals.AzureQueueStorageCommandContext = new AzureQueueStorageContext();
            CommandFeedGlobals.CommandQueueFactory = new CommandQueueFactory(context, CommandFeedGlobals.AzureQueueStorageCommandContext, CommandFeedGlobals.Clock, CommandFeedGlobals.QueueTrackerCache);

            logger.Information(nameof(Program), "Create the cosmos db client factory");
            CommandFeedGlobals.CosmosDbClientFactory = new CosmosDbClientFactory();
            
            logger.Information(nameof(Program), "Create the app configuration");
            CommandFeedGlobals.AppConfiguration = EnvironmentInfo.HostingEnvironment.CreateAppConfiguration(DualLogger.Instance);
            
            logger.Information(nameof(Program), "Create Event publisher ...");
            CommandFeedGlobals.EventPublisher = new CommandLifecycleEventPublisher();

            logger.Information(nameof(Program), "Initialize Cosmos client.");
            CosmosClientFactory.InitializeCosmosClientAsync(CommandFeedGlobals.CosmosResourceFactory).GetAwaiter().GetResult();

            logger.Information(nameof(Program), "Create Data agent map...");
            CommandFeedGlobals.AssetGroupInfoReader = new OnDiskAssetGroupInfoCollection(DocDbAssetGroupInfoCollection.CreateAsync().Result);
            CommandFeedGlobals.DataAgentMapFactory = new DataAgentMapFactory(CommandFeedGlobals.AssetGroupInfoReader);
            logger.Information(nameof(Program), "Initialize DataAgentMapFactory.");
            CommandFeedGlobals.DataAgentMapFactory.InitializeAsync().Wait();
            logger.Information(nameof(Program), $"DataAgentMap type name: {CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap().GetType().Name}");

            logger.Information(nameof(Program), "Start Pdms Cache refresh thread.");
            this.AddTask(CommandFeedGlobals.DataAgentMapFactory.RefreshAsync(this.CancellationToken));

            logger.Information(nameof(Program), "Create S2S Authenticator");
            CommandFeedGlobals.Authenticator = new MsaAuthenticator(new AadAuthenticator(null));

#if INCLUDE_TEST_HOOKS
            if (Config.Instance.Common.IsStressEnvironment)
            {
                CommandFeedGlobals.Authenticator = new StressDelegatedAuthenticator();
            }
#endif

            logger.Information(nameof(Program), "Create ServiceAuthorizer.");
            CommandFeedGlobals.ServiceAuthorizer = new ServiceAuthorizer(
                CommandFeedGlobals.DataAgentMapFactory,
                CommandFeedGlobals.Authenticator);

            logger.Information(nameof(Program), "Create command history transition repository");
            CommandFeedGlobals.CommandHistory = CommandHistoryRepository.CreateAsync(CommandFeedGlobals.CommandQueueFactory, CommandFeedGlobals.AppConfiguration).Result;

            logger.Information(nameof(Program), "Create filter work item publisher");
            CommandFeedGlobals.ExpandCommandBatchWorkItemPublisher = new AzureWorkItemQueue<PublishCommandBatchWorkItem>();

            logger.Information(nameof(Program), "Create delete from queue work item publisher");
            CommandFeedGlobals.DeleteFromQueuePublisher = new AzureWorkItemQueue<DeleteFromQueueWorkItem>();

            logger.Information(nameof(Program), "Create full export archive delete work item publisher");
            CommandFeedGlobals.DeleteFullExportArchivePublisher = new AzureWorkItemQueue<DeleteFullExportArchiveWorkItem>();

            logger.Information(nameof(Program), "Create batch checkpoint complete queue work item publisher");
            CommandFeedGlobals.BatchCheckpointCompleteQueuePublisher = new AzureWorkItemQueue<BatchCheckpointCompleteWorkItem>();

            logger.Information(nameof(Program), "Create replay request work item publisher");
            CommandFeedGlobals.InsertReplayRequestWorkItemPublisher = new AzureWorkItemQueue<ReplayRequestWorkItem>();

            logger.Information(nameof(Program), "Create enqueue replay commands work item publisher");
            CommandFeedGlobals.EnqueueReplayCommandsWorkItemPublisher = new AzureWorkItemQueue<EnqueueBatchReplayCommandsWorkItem>();

            logger.Information(nameof(Program), "Create agent queue flush work item publisher");
            CommandFeedGlobals.AgentQueueFlushWorkItemPublisher = new AzureWorkItemQueue<AgentQueueFlushWorkItem>();

            logger.Information(nameof(Program), "Create ingestion recovery work item publisher");
            CommandFeedGlobals.IngestionRecoveryWorkItemPublisher = new AzureWorkItemQueue<IngestionRecoveryWorkItem>();

            logger.Information(nameof(Program), "Create api traffic handlder");
            CommandFeedGlobals.ApiTrafficHandler = new ApiTrafficHandler();

            logger.Information(nameof(Program), "Initialize validation service...");
            PcvEnvironment validationEnvironment = PcvEnvironment.Production;
            if (!Config.Instance.Common.IsProductionEnvironment)
            {
                validationEnvironment = PcvEnvironment.Preproduction;
            }

            CommandFeedGlobals.CommandValidationService = new ValidationService(validationEnvironment);

            logger.Information(nameof(Program), "Create Kusto telemetry repository");
            CommandFeedGlobals.KustoTelemetryRepository = new KustoTelemetryRepository();

            CustomHttpConfiguration config = new CustomHttpConfiguration("https://+:443");

            OwinHttpListener listener = null;
            logger.Information(nameof(Program), $"Listening for HTTP requests at {config.BaseAddress}");

            config.MapHttpAttributeRoutes();

            StartOptions options = new StartOptions(config.BaseAddress);

            var server = WebApp.Start(
                options,
                builder => BuildOwinServer(config, builder, out listener));

            listener.SetRequestProcessingLimits(Environment.ProcessorCount * 5, int.MaxValue);
            listener.SetRequestQueueLimit(10000);

            logger.Information(nameof(Program), "Reduce header wait time to 5 seconds");
            listener.Listener.TimeoutManager.HeaderWait = TimeSpan.FromSeconds(5);

            this.webapiServers.Add(server);
        }

        private static void BuildOwinServer(CustomHttpConfiguration config, IAppBuilder builder, out OwinHttpListener listener)
        {
            builder.Use(typeof(DisallowHttpMethodHeadMiddleware));
            builder.Use(typeof(CurrentRequestsCountMiddleware));
            builder.Use(typeof(CorrelationVectorMiddleware));
            builder.Use(typeof(CorrelationContextMiddleware));
            builder.Use(typeof(RandomConnectionCloseMiddleware));
            builder.Use(typeof(NoSniffXContentTypeOptionsMiddleware));
            builder.Use(typeof(StrictTransportSecurityMiddleware));

            global::Owin.WebApiAppBuilderExtensions.UseWebApi(builder, config);
            listener = builder.Properties[typeof(OwinHttpListener).FullName] as OwinHttpListener;
        }

        private class CustomHttpConfiguration : HttpConfiguration
        {
            public CustomHttpConfiguration(string baseAddress)
            {
                this.BaseAddress = baseAddress;
            }

            public string BaseAddress { get; }
        }
    }
}
