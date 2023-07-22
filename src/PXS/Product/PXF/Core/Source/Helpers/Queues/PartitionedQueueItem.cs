// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues
{
    /// <summary>
    ///     Partitioned queue item
    /// </summary>
    public class PartitionedQueueItem<TData, TPartitionId>
    {
        /// <summary>
        ///    Initializes a new instance of the PartitionedQueueItem class
        /// </summary>
        /// <param name="item">queue item</param>
        /// <param name="partitionId">id of the partition that the queue item came from</param>
        public PartitionedQueueItem(
            IQueueItem<TData> item, 
            TPartitionId partitionId)
        {
            this.PartitionId = partitionId;
            this.Item = item;
        }

        /// <summary>
        ///      Gets the queue item
        /// </summary>
        public IQueueItem<TData> Item { get; }

        /// <summary>
        ///      Gets the id of the partition that the queue item came from
        /// </summary>
        public TPartitionId PartitionId { get; }
    }
}
