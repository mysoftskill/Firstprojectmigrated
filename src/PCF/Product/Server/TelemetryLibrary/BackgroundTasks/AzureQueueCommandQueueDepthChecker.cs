namespace Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.BackgroundTasks
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.DistributedLocking;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.Repository;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// A periodic tasks that checks the queue depths for the agent azure storage command queue,
    /// then write into kusto table.
    /// </summary>
    public class AzureQueueCommandQueueDepthChecker : WorkerTaskBase<object, AzureQueueCommandQueueDepthChecker.LockState>
    {
        private readonly IDataAgentMapFactory dataAgentFactory;
        private readonly ICommandQueueFactory commandQueueFactory;
        private readonly ITelemetryRepository telemetryRepo;
        private readonly IClock clock;

        public AzureQueueCommandQueueDepthChecker(
            IDataAgentMapFactory dataAgentMapFactory,
            ICommandQueueFactory commandQueueFactory,
            ITelemetryRepository telemetryRepo,
            IClock clock)
            : this(dataAgentMapFactory, commandQueueFactory, telemetryRepo, clock, null)
        {
            this.dataAgentFactory = dataAgentMapFactory;
            this.commandQueueFactory = commandQueueFactory;
            this.telemetryRepo = telemetryRepo;
            this.clock = clock;
        }

        public AzureQueueCommandQueueDepthChecker(
            IDataAgentMapFactory dataAgentMapFactory,
            ICommandQueueFactory commandQueueFactory,
            ITelemetryRepository telemetryRepo,
            IClock clock,
            IDistributedLockPrimitives<LockState> lockPrimitives)
            : base(Config.Instance.Worker.Tasks.AzureQueueCommandQueueDepth.CommonConfig, nameof(AzureQueueCommandQueueDepthChecker), lockPrimitives)
        {
            this.dataAgentFactory = dataAgentMapFactory;
            this.commandQueueFactory = commandQueueFactory;
            this.telemetryRepo = telemetryRepo;
            this.clock = clock;
        }

        /// <inheritdoc />
        protected override LockState GetFinalLockState(out TimeSpan leaseTime)
        {
            var nextStartTime = this.clock.UtcNow.AddMinutes(Config.Instance.Worker.Tasks.AzureQueueCommandQueueDepth.FrequencyMinutes);

            leaseTime = nextStartTime - this.clock.UtcNow + TimeSpan.FromSeconds(30);
            return new LockState { NextStartTime = nextStartTime };
        }

        /// <inheritdoc />
        protected override bool ShouldRun(LockState lockState)
        {
            DateTimeOffset nextTimeToRun = lockState?.NextStartTime ?? DateTimeOffset.MinValue;
            return nextTimeToRun <= this.clock.UtcNow;
        }

        /// <inheritdoc />
        protected override IEnumerable<Func<Task>> GetTasksAsync(LockState state, object parameters)
        {
            return new List<Func<Task>>()
            {
                async () =>
                {
                    ConcurrentBag<AgentQueueStatistics> queueDepthResult = new ConcurrentBag<AgentQueueStatistics>();

                    List<ICommandQueue> allQueues = this.GetAllAgentQueues();
                    var pendingTasks = new List<Task>();

                    while (allQueues.Any() || pendingTasks.Any())
                    {
                        while(pendingTasks.Count < 30 && allQueues.Any())
                        {
                            pendingTasks.Add(this.GetCommandQueueDepthAsync(allQueues.First(), queueDepthResult));
                            allQueues = allQueues.Skip(1).ToList();
                        }

                        var completedTask = await Task.WhenAny(pendingTasks);
                        pendingTasks.Remove(completedTask);
                    }

                    IncomingEvent.Current?.SetProperty("QueueDepthResultCount", queueDepthResult.Count.ToString());
                    DualLogger.Instance.Verbose(nameof(AzureQueueCommandQueueDepthChecker), $"Queue depth result count: {queueDepthResult.Count}");
                    if (queueDepthResult.Count > 0)
                    {
                        await this.telemetryRepo.AddAzureStorageQueueDepthAsync(queueDepthResult.ToList());
                    }
                }
            };
        }

        private List<ICommandQueue> GetAllAgentQueues()
        {
            var queues = new List<ICommandQueue>();
            IDataAgentMap dataAgentMap = this.dataAgentFactory.GetDataAgentMap();

            foreach (var assetGroupInfo in dataAgentMap.AssetGroupInfos)
            {
                foreach (SubjectType subjectType in Enum.GetValues(typeof(SubjectType)))
                {
                    var queue = this.commandQueueFactory.CreateQueue(assetGroupInfo.AgentId, assetGroupInfo.AssetGroupId, subjectType, QueueStorageType.AzureQueueStorage);
                    queues.Add(queue);
                }
            }

            IncomingEvent.Current?.SetProperty("TotalScanQueueCount", queues.Count.ToString());
            DualLogger.Instance.Verbose(nameof(AzureQueueCommandQueueDepthChecker), $"Total logical queue count: {queues.Count}");
            return queues;
        }

        private async Task GetCommandQueueDepthAsync(ICommandQueue agentQueue, ConcurrentBag<AgentQueueStatistics> queueDepthResult)
        {
            try
            {
                await agentQueue.AddQueueStatisticsAsync(queueDepthResult, true, new CancellationTokenSource().Token);
            }
            catch (Exception)
            {
                // Log and ignore.
                // Exception details is handled in outgoing events.
                IncomingEvent.Current?.SetProperty("PartiallyCompleted", "True");
            }
        }

        public class LockState
        {
            public DateTimeOffset NextStartTime { get; set; }
        }
    }
}
