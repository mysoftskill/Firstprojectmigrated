namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Work item triggered to insert a command into a set of destination queues.
    /// </summary>
    public class InsertIntoQueueWorkItem
    {
        /// <summary>
        /// The PXS command.
        /// </summary>
        public JObject PxsCommand { get; set; }

        /// <summary>
        /// The command ID.
        /// </summary>
        public CommandId CommandId { get; set; }

        /// <summary>
        /// A hint about how to parse the PXS command.
        /// </summary>
        public PrivacyCommandType CommandType { get; set; }

        /// <summary>
        /// If this work item is for ReplayForAll
        /// </summary>
        public bool IsReplayCommand { get; set; } = false;

        /// <summary>
        /// If this work item is for IngestionRecovery
        /// </summary>
        public bool IsIngestionRecovery { get; set; } = false;

        /// <summary>
        /// The Pdms config data version used for this command.
        /// </summary>
        public long? DataSetVersion { get; set; }

        /// <summary>
        /// List of destinations that need a copy of this command.
        /// </summary>
        public List<PxsFilteredCommandDestination> Destinations { get; set; }
    }

    /// <summary>
    /// Handles InsertIntoQueueWorkItem instances.
    /// </summary>
    public class InsertIntoQueueWorkItemHandler : IAzureWorkItemQueueHandler<InsertIntoQueueWorkItem>
    {
        // The maximum number of async calls it is reasonable to do in a "For" loop.
        public const int ReasonableLoopThreshold = 10;

        private readonly ICommandLifecycleEventPublisher eventPublisher;
        private readonly ICommandQueueFactory queueFactory;
        private readonly IDataAgentMapFactory dataAgentMapFactory;

        // Publisher for recursive publishes.
        private readonly IAzureWorkItemQueuePublisher<InsertIntoQueueWorkItem> workItemPublisher;

        public InsertIntoQueueWorkItemHandler(
            IAzureWorkItemQueuePublisher<InsertIntoQueueWorkItem> workItemPublisher,
            ICommandLifecycleEventPublisher eventPublisher,
            ICommandQueueFactory queueFactory,
            IDataAgentMapFactory dataAgentMapFactory)
        {
            this.workItemPublisher = workItemPublisher;
            this.eventPublisher = eventPublisher;
            this.queueFactory = queueFactory;
            this.dataAgentMapFactory = dataAgentMapFactory;
        }

        public SemaphorePriority WorkItemPriority => SemaphorePriority.Low;

        /// <summary>
        /// Depending on the number of items, either inserts into agent queues or publishes sub work items.
        /// </summary>
        public async Task<QueueProcessResult> ProcessWorkItemAsync(QueueWorkItemWrapper<InsertIntoQueueWorkItem> wrapper)
        {
            IncomingEvent.Current?.SetProperty("CommandId", wrapper.WorkItem.CommandId.Value);
            IncomingEvent.Current?.SetProperty("CommandType", wrapper.WorkItem.CommandType.ToString());
            IncomingEvent.Current?.SetProperty("IsIngestionRecovery", wrapper.WorkItem.IsIngestionRecovery.ToString());
            IncomingEvent.Current?.SetProperty("IsReplay", wrapper.WorkItem.IsReplayCommand.ToString());
            IncomingEvent.Current?.SetProperty("DestinationCount", wrapper.WorkItem.Destinations.Count.ToString());

            DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(InsertIntoQueueWorkItem), $"Processing InsertIntoWorkItem for CommandId={wrapper.WorkItem.CommandId.Value} with IsIngestionRecovery={wrapper.WorkItem.IsIngestionRecovery.ToString()}");

            if (wrapper.WorkItem.Destinations.Count <= ReasonableLoopThreshold)
            {
                // If we're under the threshold, then we insert directly into queues.
                return await this.InsertItemsIntoQueueAsync(wrapper);
            }
            else
            {
                // If we're over the threshold, then we split further.
                return await this.SubdivideDestinationsAsync(wrapper);
            }
        }

        private async Task<QueueProcessResult> SubdivideDestinationsAsync(QueueWorkItemWrapper<InsertIntoQueueWorkItem> wrapper)
        {
            InsertIntoQueueWorkItem[] subItems = new InsertIntoQueueWorkItem[ReasonableLoopThreshold];

            // Fill the array with empty items.
            for (int i = 0; i < subItems.Length; ++i)
            {
                subItems[i] = new InsertIntoQueueWorkItem
                {
                    CommandId = wrapper.WorkItem.CommandId,
                    CommandType = wrapper.WorkItem.CommandType,
                    IsIngestionRecovery = wrapper.WorkItem.IsIngestionRecovery,
                    IsReplayCommand = wrapper.WorkItem.IsReplayCommand,
                    PxsCommand = wrapper.WorkItem.PxsCommand,
                    Destinations = new List<PxsFilteredCommandDestination>(),
                    DataSetVersion = wrapper.WorkItem.DataSetVersion,
                };
            }

            int destinationIndex = 0;
            int subItemIndex = 0;

            // Round-robins through the sub-items and adds destinations in groups of 10.
            // This produces roughly evenly sized batches, while minimizing the number of batches
            // when there are only a handful of destinations.
            while (destinationIndex < wrapper.WorkItem.Destinations.Count)
            {
                int itemsRemaining = ReasonableLoopThreshold;
                while (itemsRemaining > 0 && destinationIndex < wrapper.WorkItem.Destinations.Count)
                {
                    subItems[subItemIndex].Destinations.Add(wrapper.WorkItem.Destinations[destinationIndex]);
                    itemsRemaining--;
                    destinationIndex++;
                }

                subItemIndex++;

                if (subItemIndex >= subItems.Length)
                {
                    subItemIndex = 0;
                }
            }

            List<Task> publishTasks = new List<Task>();
            foreach (var subItem in subItems)
            {
                if (subItem.Destinations.Count > 0)
                {
                    publishTasks.Add(this.workItemPublisher.PublishAsync(subItem));
                }
            }

            await Task.WhenAll(publishTasks);
            return QueueProcessResult.Success();
        }

        private async Task<QueueProcessResult> InsertItemsIntoQueueAsync(QueueWorkItemWrapper<InsertIntoQueueWorkItem> wrapper)
        {
            string assetGroupStreamName = string.Empty;
            string variantStreamName = string.Empty;

            if (wrapper.WorkItem.DataSetVersion != null)
            {
                IDataAgentMap dataAgentMap = await this.dataAgentMapFactory.GetDataAgentMapAsync(wrapper.WorkItem.DataSetVersion.Value);
                assetGroupStreamName = dataAgentMap.AssetGroupInfoStreamName;
                variantStreamName = dataAgentMap.VariantInfoStreamName;
            }

            var enqueueTasks = new List<(PxsFilteredCommandDestination destination, Task task)>();
            foreach (var item in wrapper.WorkItem.Destinations)
            {
                Task insertTask = CommandIngester.AddCommandAsync(
                    item.AgentId,
                    item.AssetGroupId,
                    item.DataTypes,
                    item.AssetGroupQualifier,
                    this.queueFactory,
                    wrapper.WorkItem.PxsCommand,
                    this.eventPublisher,
                    assetGroupStreamName,
                    variantStreamName,
                    item.TargetMoniker,
                    item.QueueStorageType);

                enqueueTasks.Add((item, insertTask));
            }

            try
            {
                // Wait for all, but deal with partial success.
                await Task.WhenAll(enqueueTasks.Select(x => x.task));
                DualLogger.Instance.LogInformationForCommandLifeCycle(nameof(InsertIntoQueueWorkItem), $"Enqueued command={wrapper.WorkItem.CommandId} for all destinations");
            }
            catch
            {
                // Swallow failures; we handle partial success explicitly.
                IncomingEvent.Current?.SetProperty("PartialSuccess", "true");
                DualLogger.Instance.LogWarningForCommandLifeCycle(nameof(InsertIntoQueueWorkItem), $"Partial success in enqueuing command={wrapper.WorkItem.CommandId}");
            }

            // Look for failures and retry with just those.
            wrapper.WorkItem.Destinations = enqueueTasks.Where(x => x.task.IsFaulted).Select(x => x.destination).ToList();

            // Retry if there is any work left to do.
            if (wrapper.WorkItem.Destinations.Count > 0)
            {
                var maxRetryWaitTime = FlightingUtilities.GetConfigValue<int>(ConfigNames.PCF.InsertIntoQueue_MaxRetryWaitTimeInSeconds, defaultValue: 300);
                return QueueProcessResult.RetryAfter(TimeSpan.FromSeconds(RandomHelper.Next(10, maxRetryWaitTime)));
            }
            else
            {
                return QueueProcessResult.Success();
            }
        }
    }
}
