// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues
{
    /// <summary>
    ///     represents the size of a particular queue partition
    /// </summary>
    public class QueuePartitionSize<TPartitionId> : QueuePartition<TPartitionId>
    {
        /// <summary>
        ///    Initializes a new instance of the QueuePartitionSize struct
        /// </summary>
        /// <param name="id">partition id</param>
        /// <param name="name">partition name</param>
        /// <param name="count">item count</param>
        public QueuePartitionSize(
            TPartitionId id, 
            string name,
            ulong count) :
            base(id, name)
        {
            this.Count = count;
        }

        /// <summary>
        ///      Gets the approximate count of items in the partition
        /// </summary>
        public ulong Count { get; }
    }
}
