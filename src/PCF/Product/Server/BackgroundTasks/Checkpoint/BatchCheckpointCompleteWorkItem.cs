namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// A work item to delete items from our queues given a set of lease receipts.
    /// </summary>
    public class BatchCheckpointCompleteWorkItem
    {
        /// <summary>
        /// Store a list of items as a list for forward compatibility.
        /// </summary>
        public IList<LeaseReceipt> LeaseReceipts { get; set; }

        /// <summary>
        /// The agent ID.
        /// </summary>
        public AgentId AgentId { get; set; }
    }

    /// <summary>
    /// Uses lease receipts to delete from agent queues.
    /// </summary>
    public class BatchCheckpointCompleteWorkItemHandler : IAzureWorkItemQueueHandler<BatchCheckpointCompleteWorkItem>
    {
        // The maximum number of async calls it is reasonable to do in a "For" loop.
        public const int ReasonableLoopThreshold = 10;

        private readonly IAzureWorkItemQueuePublisher<DeleteFromQueueWorkItem> deleteFromQueuePublisher;

        private readonly IAzureWorkItemQueuePublisher<BatchCheckpointCompleteWorkItem> workItemPublisher;

        public BatchCheckpointCompleteWorkItemHandler(
            IAzureWorkItemQueuePublisher<BatchCheckpointCompleteWorkItem> workItemPublisher,
            IAzureWorkItemQueuePublisher<DeleteFromQueueWorkItem> deleteFromQueuePublisher)
        {
            this.workItemPublisher = workItemPublisher;
            this.deleteFromQueuePublisher = deleteFromQueuePublisher;
        }

        public SemaphorePriority WorkItemPriority => SemaphorePriority.RealTime;

        public async Task<QueueProcessResult> ProcessWorkItemAsync(QueueWorkItemWrapper<BatchCheckpointCompleteWorkItem> wrapper)
        {
            BatchCheckpointCompleteWorkItem workItem = wrapper.WorkItem;

            IncomingEvent.Current?.SetProperty("AgentId", workItem.AgentId.Value);
            IncomingEvent.Current?.SetProperty("LeaseReceiptCount", workItem.LeaseReceipts.Count.ToString());
            IncomingEvent.Current?.SetProperty("CommandIds", string.Join(",", workItem.LeaseReceipts.Select(lr => lr.CommandId.Value)));

            if (wrapper.WorkItem.LeaseReceipts.Count <= ReasonableLoopThreshold)
            {
                // If we're under the threshold, then we insert directly into the delete queue.
                return await this.ProcessItemAsync(wrapper);
            }
            else
            {
                // If we're over the threshold, then we split this queue further.
                return await this.SubdivideDestinationsAsync(wrapper);
            }
        }

        private async Task<QueueProcessResult> SubdivideDestinationsAsync(QueueWorkItemWrapper<BatchCheckpointCompleteWorkItem> wrapper)
        {
            BatchCheckpointCompleteWorkItem[] subItems = new BatchCheckpointCompleteWorkItem[ReasonableLoopThreshold];

            // Fill the array with empty items.
            for (int i = 0; i < subItems.Length; ++i)
            {
                subItems[i] = new BatchCheckpointCompleteWorkItem
                {
                    AgentId = wrapper.WorkItem.AgentId,
                    LeaseReceipts = new List<LeaseReceipt>()
                };
            }

            int index = 0;
            int subItemIndex = 0;

            // Round-robins through the sub-items and adds lease receipts in groups of 10.
            // This produces roughly evenly sized batches, while minimizing the number of batches
            // when there are only a handful of receipts.
            while (index < wrapper.WorkItem.LeaseReceipts.Count)
            {
                int itemsRemaining = ReasonableLoopThreshold;
                while (itemsRemaining > 0 && index < wrapper.WorkItem.LeaseReceipts.Count)
                {
                    subItems[subItemIndex].LeaseReceipts.Add(wrapper.WorkItem.LeaseReceipts[index]);
                    itemsRemaining--;
                    index++;
                }

                subItemIndex++;

                if (subItemIndex >= subItems.Length)
                {
                    subItemIndex = 0;
                }
            }

            List<Task> publishTasks = new List<Task>();
            foreach (BatchCheckpointCompleteWorkItem subItem in subItems)
            {
                if (subItem.LeaseReceipts.Count > 0)
                {
                    // The random delay below of a maximum 120s may be tuned in the future.
                    // It's important to take the minimum lease receipt time of 15mins in consideration when doing so.
                    // If the leases expire before the command is completed, duplicated commands may be sent to the data-agents.
                    var randomDelay = TimeSpan.FromSeconds(120 * RandomHelper.NextDouble());
                    publishTasks.Add(this.workItemPublisher.PublishAsync(subItem, randomDelay));
                }
            }

            await Task.WhenAll(publishTasks);
            return QueueProcessResult.Success();
        }

        private async Task<QueueProcessResult> ProcessItemAsync(QueueWorkItemWrapper<BatchCheckpointCompleteWorkItem> wrapper)
        {
            var enqueueTasks = new List<(LeaseReceipt receipt, Task task)>();
            AgentId agentId = wrapper.WorkItem.AgentId;
            
            foreach (LeaseReceipt receipt in wrapper.WorkItem.LeaseReceipts)
            {
                if (receipt == null)
                {
                    continue;
                }

                // The value here is random across the range from [0, 0.75 * time until expiration). This is to allow us to have time to replay in case we fail the first time.
                // 6 hours
                int maxVisiblityDelaySeconds = 6 * 60 * 60;
                    
                // Cap the delay time at Lease Time * 75% or 6 hours, whichever is smaller
                int visibilityDelaySeconds = (int)((receipt.ApproximateExpirationTime - DateTimeOffset.UtcNow).TotalSeconds * 0.75);
                visibilityDelaySeconds = Math.Min(visibilityDelaySeconds, maxVisiblityDelaySeconds);
                    
                // Generate a random delay time that's less than the maximum
                visibilityDelaySeconds = (int)(visibilityDelaySeconds * RandomHelper.NextDouble());
                    
                // Require a minimum 5-second delay
                visibilityDelaySeconds = Math.Max(visibilityDelaySeconds, 5);
                    
                Task deleteTask = this.deleteFromQueuePublisher.PublishAsync(new DeleteFromQueueWorkItem(agentId, receipt), TimeSpan.FromSeconds(visibilityDelaySeconds));
                enqueueTasks.Add((receipt, deleteTask));
            }

            try
            {
                // Wait for all, but deal with partial success.
                await Task.WhenAll(enqueueTasks.Select(x => x.task));
            }
            catch
            {
                // Swallow failures; we handle partial success explicitly.
                IncomingEvent.Current?.SetProperty("PartialSuccess", "true");
            }

            // Look for failures and retry with just those.
            wrapper.WorkItem.LeaseReceipts = enqueueTasks.Where(x => x.task.IsFaulted).Select(x => x.receipt).ToList();

            // Retry if there is any work left to do.
            if (wrapper.WorkItem.LeaseReceipts.Count > 0)
            {
                return QueueProcessResult.RetryAfter(TimeSpan.FromSeconds(RandomHelper.Next(1, 5)));
            }
            else
            {
                return QueueProcessResult.Success();
            }
        }
    }
}
