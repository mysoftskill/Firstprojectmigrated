namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Newtonsoft.Json;
    
    /// <summary>
    /// A work item to delete items from our queues given a set of lease receipts.
    /// </summary>
    public class DeleteFromQueueWorkItem
    {
        public DeleteFromQueueWorkItem(AgentId agentId, LeaseReceipt leaseReceipt)
        {
            this.LeaseReceipts = new List<LeaseReceipt>
            {
                leaseReceipt
            };

            this.AgentId = agentId;
        }

        [Obsolete("Use the other constructor. This is here to make JSON.Net happy")]
        public DeleteFromQueueWorkItem()
        {
        }

        /// <summary>
        /// Store a list of items as a list for forward compatibility.
        /// </summary>
        public List<LeaseReceipt> LeaseReceipts { get; set; }

        /// <summary>
        /// The agent ID.
        /// </summary>
        public AgentId AgentId { get; set; }
    }

    /// <summary>
    /// Uses lease receipts to delete from agent queues.
    /// </summary>
    public class DeleteFromQueueWorkItemHandler : IAzureWorkItemQueueHandler<DeleteFromQueueWorkItem>
    {
        private readonly ICommandQueueFactory commandQueueFactory;

        public DeleteFromQueueWorkItemHandler(ICommandQueueFactory commandQueueFactory)
        {
            this.commandQueueFactory = commandQueueFactory;
        }

        public SemaphorePriority WorkItemPriority => SemaphorePriority.RealTime;

        public async Task<QueueProcessResult> ProcessWorkItemAsync(QueueWorkItemWrapper<DeleteFromQueueWorkItem> wrapper)
        {
            var workItem = wrapper.WorkItem;
            var assetGroupCommandIdPairs = workItem.LeaseReceipts.Select(x => $"{x.AssetGroupId},{x.CommandId}");

            IncomingEvent.Current?.SetProperty("AgentId", workItem.AgentId.Value);
            
            if (workItem.LeaseReceipts != null)
            {
                IncomingEvent.Current?.SetProperty("BatchSize", workItem.LeaseReceipts.Count.ToString());
                IncomingEvent.Current?.SetProperty("Identifiers", string.Join(";", assetGroupCommandIdPairs));

                List<Task> processTasks = new List<Task>();
                foreach (LeaseReceipt leaseReceipt in workItem.LeaseReceipts)
                {
                    processTasks.Add(this.ProcessItemAsync(leaseReceipt));
                }

                await Task.WhenAll(processTasks);
            }

            return QueueProcessResult.Success();
        }

        private async Task ProcessItemAsync(LeaseReceipt leaseReceipt)
        {
            var queue = this.commandQueueFactory.CreateQueue(leaseReceipt.AgentId, leaseReceipt.AssetGroupId, leaseReceipt.SubjectType, leaseReceipt.QueueStorageType);

            if (!queue.SupportsLeaseReceipt(leaseReceipt) && FlightingUtilities.IsEnabled(FlightingNames.DeleteFromQueueWorkItemSuppressInvalidLeaseReceipts))
            {
                return;
            }

            try
            {
                await queue.DeleteAsync(leaseReceipt);
            }
            catch (CommandFeedException ex)
            {
                if (ex.ErrorCode == CommandFeedInternalErrorCode.NotFound || 
                    ex.ErrorCode == CommandFeedInternalErrorCode.Conflict)
                {
                    // Already deleted or reissued. So, we're done.
                    return;
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
