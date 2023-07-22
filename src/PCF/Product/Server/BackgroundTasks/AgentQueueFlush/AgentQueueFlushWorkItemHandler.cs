namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    public class AgentQueueFlushWorkItemHandler : IAzureWorkItemQueueHandler<AgentQueueFlushWorkItem>
    {
        private readonly ICommandQueueFactory commandQueueFactory;
        private readonly IDataAgentMapFactory dataAgentMapFactory;

        public AgentQueueFlushWorkItemHandler(ICommandQueueFactory commandQueueFactory, IDataAgentMapFactory dataAgentMapFactory)
        {
            this.commandQueueFactory = commandQueueFactory;
            this.dataAgentMapFactory = dataAgentMapFactory;
        }

        public SemaphorePriority WorkItemPriority => SemaphorePriority.Background;

        public async Task<QueueProcessResult> ProcessWorkItemAsync(QueueWorkItemWrapper<AgentQueueFlushWorkItem> wrapper)
        {
            if (FlightingUtilities.IsEnabled(FlightingNames.AgentQueueFlushDrainAzureQueue))
            {
                IncomingEvent.Current?.SetProperty("FlushStatus", "DrainAzureQueue");
                return QueueProcessResult.Success();
            }
            else if (FlightingUtilities.IsEnabled(FlightingNames.AgentQueueFlushDelayQueueProcess))
            {
                // Flight control to delay process the item for 60min in case of bug or other issues
                // which will likely required a hotfix before retry.
                IncomingEvent.Current?.SetProperty("FlushStatus", "DelayProcess");
                return QueueProcessResult.RetryAfter(TimeSpan.FromMinutes(60));
            }

            var agentQueueFlushWorkItem = wrapper.WorkItem;
            var agentId = agentQueueFlushWorkItem.AgentId;
            var flushDate = agentQueueFlushWorkItem.FlushDate;

            IncomingEvent.Current?.SetProperty("AgentId", agentQueueFlushWorkItem.AgentId.Value);
            IncomingEvent.Current?.SetProperty("FlushDate", agentQueueFlushWorkItem.FlushDate.ToString());

            ICommandQueue commandQueue = new DataAgentCommandQueue(
                agentId,
                this.commandQueueFactory,
                this.dataAgentMapFactory.GetDataAgentMap());

            CancellationTokenSource taskCancellationSource = new CancellationTokenSource();

            taskCancellationSource.CancelAfter(TimeSpan.FromMinutes(10));

            await commandQueue.FlushAgentQueueAsync(flushDate, taskCancellationSource.Token);

            if (taskCancellationSource.IsCancellationRequested)
            {
                IncomingEvent.Current?.SetProperty("FlushStatus", "ForcedRetryDueToTimeLimit");
                return QueueProcessResult.RetryAfter(TimeSpan.FromMinutes(1));
            }

            IncomingEvent.Current?.SetProperty("FlushStatus", "Completed");
            return QueueProcessResult.Success();
        }
    }
}
