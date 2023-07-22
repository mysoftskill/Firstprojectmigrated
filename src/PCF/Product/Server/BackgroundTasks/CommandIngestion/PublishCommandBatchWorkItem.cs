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
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Work item that is responsible for taking a set of PXS commands, and enqueing separate <see cref="FilterAndRouteCommandWorkItem"/> instances.
    /// </summary>
    public class PublishCommandBatchWorkItem
    {
        /// <summary>
        /// The PXS command.
        /// </summary>
        public List<JObject> PxsCommands { get; set; }

        /// <summary>
        /// The PDMS data set version of the frontend.
        /// </summary>
        public long DataSetVersion { get; set; }

        /// <summary>
        /// If this work item is for ReplayForAll
        /// </summary>
        public bool IsReplayCommands { get; set; } = false;
    }

    /// <summary>
    /// Processes ExpandCommandBatchWorkItems out of an Azure Queue.
    /// </summary>
    public class PublishCommandBatchWorkItemHandler : IAzureWorkItemQueueHandler<PublishCommandBatchWorkItem>
    {
        private readonly IAzureWorkItemQueuePublisher<FilterAndRouteCommandWorkItem> workItemPublisher;
        private readonly IAzureWorkItemQueuePublisher<FilterAndRouteCommandWorkItem> whatIfWorkItemPublisher;

        public PublishCommandBatchWorkItemHandler(
            IAzureWorkItemQueuePublisher<FilterAndRouteCommandWorkItem> workItemPublisher,
            IAzureWorkItemQueuePublisher<FilterAndRouteCommandWorkItem> whatIfWorkItemPublisher)
        {
            this.workItemPublisher = workItemPublisher;
            this.whatIfWorkItemPublisher = whatIfWorkItemPublisher;
        }

        public SemaphorePriority WorkItemPriority => SemaphorePriority.Low;

        /// <summary>
        /// Publishes FilterAndRouteCommandWorkItems in batches of 20 at a time.
        /// </summary>
        public async Task<QueueProcessResult> ProcessWorkItemAsync(QueueWorkItemWrapper<PublishCommandBatchWorkItem> wrapper)
        {
            var workItem = wrapper.WorkItem;
            IEnumerable<JObject> commands = workItem.PxsCommands;

            IncomingEvent.Current?.SetProperty("IsForReplay", workItem.IsReplayCommands.ToString());
            IncomingEvent.Current?.SetProperty("CommandsCount", commands.Count().ToString());

            if (commands.Any())
            {
                var currentCommands = commands.Take(10);
                commands = commands.Skip(10);

                await this.PublishBatchAsync(currentCommands, workItem);

                // Update the contents of the work item to remove the items we've already published, then reexecute it immediately.
                workItem.PxsCommands = commands.ToList();
                return QueueProcessResult.RetryAfter(TimeSpan.Zero);
            }

            return QueueProcessResult.Success();
        }

        private async Task PublishBatchAsync(IEnumerable<JObject> commands, PublishCommandBatchWorkItem workItem)
        {
            List<Task> publishTasks = new List<Task>();

            foreach (var item in commands)
            {
                var (pcfCommand, pxsCommand) = PxsCommandParser.DummyParser.Process(item);

                FilterAndRouteCommandWorkItem filterWorkItem = new FilterAndRouteCommandWorkItem
                {
                    CommandId = pcfCommand.CommandId,
                    CommandType = pcfCommand.CommandType,
                    DataSetVersion = workItem.DataSetVersion,
                    PxsCommand = item,
                    IsReplayCommand = workItem.IsReplayCommands
                };

                TimeSpan? delay = null;

                if (workItem.IsReplayCommands || pcfCommand.CommandType == PrivacyCommandType.AccountClose)
                {
                    // Account close commands and Replay commands come in big bursts on a minute-by-minute basis. Apply some
                    // delay to smooth things out for PCF.
                    delay = TimeSpan.FromSeconds(RandomHelper.Next(0, Config.Instance.Worker.AccountCloseCommandMaxIngestionDelaySeconds));
                }

                publishTasks.Add(this.workItemPublisher.PublishAsync(filterWorkItem, delay));
                publishTasks.Add(this.whatIfWorkItemPublisher.PublishAsync(filterWorkItem, delay));
            }

            await Task.WhenAll(publishTasks);
        }
    }
}
