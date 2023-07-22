// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.QueueProcessor
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    /// <summary>
    ///     Round-Robin Aad-Account-Close-Queue-Strategy
    /// </summary>
    public class AadAccountCloseRoundRobinQueueStrategy : IQueueSelectionStrategy<AccountCloseRequest>
    {
        private readonly IList<IQueue<AccountCloseRequest>> queues;

        /// <summary>
        ///     Creates a new instance of Round-Robin Aad-Account-Close-Queue
        /// </summary>
        /// <param name="queues">The collection of queues</param>
        public AadAccountCloseRoundRobinQueueStrategy(IList<IQueue<AccountCloseRequest>> queues)
        {
            if (queues == null)
            {
                throw new ArgumentNullException(nameof(queues));
            }

            this.queues = new List<IQueue<AccountCloseRequest>>(queues);
        }

        /// <inheritdoc />
        public IList<IQueue<AccountCloseRequest>> GetAllQueues()
        {
            return this.queues ?? null;
        }

        /// <summary>
        ///     Gets a random queue from the available queues.
        /// </summary>
        /// <returns>A queue</returns>
        public IQueue<AccountCloseRequest> GetRandomQueue()
        {
            return this.queues?.Count > 0 ? this.queues?[GetRandomIndex(this.queues)] : null;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Tries to get the next available queue and removes it from available queues
        /// </summary>
        /// <param name="queue">The queue</param>
        /// <returns>bool if able to get a queue, false if not</returns>
        public bool TryGetNextQueueAndRemove(out IQueue<AccountCloseRequest> queue)
        {
            if (this.queues == null || this.queues?.Count == 0)
            {
                queue = null;
                return false;
            }

            int chosenQueueIndex = GetRandomIndex(this.queues);
            queue = this.queues[chosenQueueIndex];
            this.queues.RemoveAt(chosenQueueIndex);
            return true;
        }

        private static int GetRandomIndex<T>(IList<IQueue<T>> queues)
        {
            return Math.Abs((int)DateTime.UtcNow.Ticks % queues.Count);
        }
    }
}
