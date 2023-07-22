namespace Microsoft.PrivacyServices.CommandFeed.Service.WhatIfFilterAndRouteWorkItemHost
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue.QueueStorageCommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// The Pcf worker.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public sealed class Program : PrivacyApplication
    {
        private Program() : base(CommandFeedService.WhatIfFilterAndRouteWorkItemHost)
        {
        }

        [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "t")]
        protected override void OnStart()
        {
            base.OnStart();

            GenevaHelper.Initialize(TraceTagPrefixes.ADGCS_PCF);
            DualLogger.AddTraceListener();

            // This is a background service that doesn't generate a ton of logs. In order
            // to make sure they (mostly) get put in the same hour as the real logs we need to join against,
            // reduce the size of the log files to be smaller.
            Logger.Instance = new SllLogger(10L * 1024 * 1024, 100);

            DualLogger.Instance.Information(nameof(Program), "Create Data agent map...");
            var docDb = DocDbAssetGroupInfoCollection.CreateAsync().Result;
            var dataAgentMapFactory = new DataAgentMapFactory(docDb);
            dataAgentMapFactory.InitializeAsync().Wait();

            var whatIfQueue = new AzureWorkItemQueue<FilterAndRouteCommandWorkItem>(BaseFilterAndRouteCommandWorkItemHandler.WhatIfQueueName);
            Task t = whatIfQueue.BeginProcessAsync(new WhatIfFilterAndRouteWorkItemHandler(dataAgentMapFactory, new AzureQueueStorageContext()), this.CancellationToken);
        }
        
        public static void Main(string[] args)
        {
            Program program = new Program();
            program.Run(args);
        }
    }
}
