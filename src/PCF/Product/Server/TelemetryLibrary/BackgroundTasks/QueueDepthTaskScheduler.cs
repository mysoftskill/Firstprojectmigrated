namespace Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.BackgroundTasks
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen;
    using Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.Repository;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// A periodic task that create queue depth calculation tasks in Azure Queue.
    /// </summary>
    [ExcludeFromCodeCoverage] // Justification: runs as a periodic background operation. Not really coverable.
    public class QueueDepthTaskScheduler : WorkerTaskBase<object, QueueDepthTaskScheduler.LockState>
    {
        private readonly TimeSpan frequency;
        private DateTimeOffset startTime;

        /// <summary>
        /// Create background worker
        /// </summary>
        public QueueDepthTaskScheduler(
            string taskName,
            ITaskConfiguration taskConfig,
            TimeSpan frequency)
            : base(taskConfig, taskName, null)
        {
            this.frequency = frequency;
        }

        /// <summary>
        /// Publishers.
        /// </summary>
        public Dictionary<SubjectType, IAzureWorkItemQueuePublisher<QueueDepthWorkItem>> Publishers { get; set; }

        public IDataAgentMapFactory DataAgentFactory { get; set; }

        public ITelemetryRepository TelemetryRepository { get; set; }

        public static List<QueueDepthWorkItem> CreateAgentWorkItems(AgentId agentId, IDataAgentInfo dataAgentInfo, DateTimeOffset startTime)
        {
            List<QueueDepthWorkItem> workItems = new List<QueueDepthWorkItem>();
            var assetGroupInfos = dataAgentInfo.AssetGroupInfos;

            var databases = DatabaseConnectionInfo.GetDatabaseConnectionInfosFromConfig();
            foreach (var assetGroupInfo in assetGroupInfos)
            {
                foreach (var subject in (SubjectType[])Enum.GetValues(typeof(SubjectType)))
                {
                    foreach (var db in databases)
                    {
                        var workItem = new QueueDepthWorkItem()
                        {
                            TaskId = Guid.NewGuid(),
                            CreateTime = startTime,
                            AgentId = agentId,
                            AssetGroupId = assetGroupInfo.AssetGroupId,
                            CommandTypeCountDictionary = new Dictionary<PrivacyCommandType, int>()
                                {
                                    { PrivacyCommandType.AccountClose, 0 },
                                    { PrivacyCommandType.Delete, 0 },
                                    { PrivacyCommandType.Export, 0 },
                                },
                            ContinuationToken = null,
                            DatabaseId = db.DatabaseId,
                            MaxItemsCount = Config.Instance.Telemetry.MaxItemCount,
                            SubjectType = subject,
                            CollectionId = CosmosDbQueueCollection.GetQueueCollectionId(subject),
                            DbMoniker = db.DatabaseMoniker,
                            BatchSize = 1000,
                        };

                        workItems.Add(workItem);
                    }
                }
            }

            return workItems;
        }

        /// <summary>
        /// Create worker tasks
        /// </summary>
        protected override IEnumerable<Func<Task>> GetTasksAsync(
            LockState state,
            object parameters)
        {
            IDataAgentMap dataAgentMap = this.DataAgentFactory.GetDataAgentMap();
            List<QueueDepthWorkItem> workItems = new List<QueueDepthWorkItem>();

            List<Func<Task>> tasks = new List<Func<Task>>();
            this.startTime = DateTimeOffset.UtcNow;

            // create tasks list
            foreach (var agentId in dataAgentMap.GetAgentIds())
            {
                workItems.AddRange(CreateAgentWorkItems(agentId, dataAgentMap[agentId], this.startTime));
            }

            ConcurrentQueue<QueueDepthWorkItem> workItemQueue = new ConcurrentQueue<QueueDepthWorkItem>(workItems);

            // add Kusto thread
            tasks.Add(() => this.TelemetryRepository.AddTasksAsync(workItems, TaskActionName.Create));

            // add Azure threads
            var config = Config.Instance.Worker.Tasks.BaselineQueueDepthTaskScheduler.CommonConfig;
            for (int i = 0; i < config.BatchSize; i++)
            {
                tasks.Add(() => this.ScheduleQueueDepthTasksAsync(workItemQueue));
            }

            return tasks;
        }

        private async Task ScheduleQueueDepthTasksAsync(ConcurrentQueue<QueueDepthWorkItem> workItemQueue)
        {
            while (workItemQueue.TryDequeue(out var workItem))
            {
                // publish queue depth task
                await this.Publishers[workItem.SubjectType].PublishAsync(workItem);
            }
        }

        protected override bool ShouldRun(LockState lockState)
        {
            DateTimeOffset nextTimeToRun = lockState?.NextStartTime ?? DateTimeOffset.MinValue;
            return nextTimeToRun <= DateTimeOffset.UtcNow;
        }

        protected override LockState GetFinalLockState(out TimeSpan leaseTime)
        {
            DateTimeOffset nextTimeToRun = this.startTime + this.frequency;
            leaseTime = nextTimeToRun > DateTimeOffset.UtcNow ? nextTimeToRun - DateTimeOffset.UtcNow : TimeSpan.Zero;
            DualLogger.Instance.Information(nameof(AzureQueueCommandQueueDepthChecker), $"TaskName={this.TaskName}, NextTimeToRun={nextTimeToRun.ToString("o")}");
            return new LockState { NextStartTime = nextTimeToRun };
        }

        // State stored along with the lock.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public class LockState
        {
            public DateTimeOffset NextStartTime { get; set; }
        }
    }
}
