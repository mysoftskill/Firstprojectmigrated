// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper
{
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;

    /// <summary>
    ///     Interface for Queue Selection Strategy
    /// </summary>
    public interface IQueueSelectionStrategy<T>
    {
        /// <summary>
        ///     Get all the queues
        /// </summary>
        /// <returns>A collection of queues</returns>
        IList<IQueue<T>> GetAllQueues();

        /// <summary>
        ///     Gets a random queue
        /// </summary>
        /// <returns>A queue</returns>
        IQueue<T> GetRandomQueue();

        /// <summary>
        ///     Tries to get the next queue and removes it from available queues
        /// </summary>
        /// <param name="queue">The queue</param>
        /// <returns>bool if a queue is available, else false</returns>
        bool TryGetNextQueueAndRemove(out IQueue<T> queue);
    }
}
