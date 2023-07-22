// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues
{
    /// <summary>
    ///     represents a single partiton in a partitioned queue
    /// </summary>
    public class QueuePartition<TPartitionId>
    {
        /// <summary>
        ///    Initializes a new instance of the QueuePartition{TPartitionId} class
        /// </summary>
        /// <param name="id">partition id</param>
        /// <param name="name">partition name</param>
        public QueuePartition(
            TPartitionId id, 
            string name)
        {
            this.Id = id;
            this.Name = name;
        }

        /// <summary>
        ///      Gets the partition id
        /// </summary>
        public TPartitionId Id { get; }

        /// <summary>
        ///      Gets the name of the internal queue representing the partition
        /// </summary>
        public string Name { get; }
    }
}
