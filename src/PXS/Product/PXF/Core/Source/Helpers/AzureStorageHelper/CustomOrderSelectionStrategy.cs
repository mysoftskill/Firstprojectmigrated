// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;

    public class CustomOrderSelectionStrategy<T> : IQueueSelectionStrategy<T>
    {
        /// <summary>
        ///     Returns the index of the chosen queue
        /// </summary>
        /// <param name="queues">The queues to choose from</param>
        /// <returns>The index of the queue to select</returns>
        public delegate int IndexSelector(IList<IQueue<T>> queues);

        private readonly IndexSelector indexGenerator;

        private readonly IList<IQueue<T>> queues;

        /// <summary>
        ///     Creates an instance of <see cref="CustomOrderSelectionStrategy{T}" />.
        /// </summary>
        /// <param name="queues">The queues to select from</param>
        /// <param name="indexSelector">The selector that is used for determining the next queue index to select</param>
        public CustomOrderSelectionStrategy(IList<IQueue<T>> queues, IndexSelector indexSelector)
        {
            this.queues = new List<IQueue<T>>(queues ?? throw new ArgumentNullException(nameof(queues)));
            this.indexGenerator = indexSelector ?? throw new ArgumentNullException(nameof(indexSelector));
        }

        public IList<IQueue<T>> GetAllQueues()
        {
            return this.queues;
        }

        public IQueue<T> GetRandomQueue()
        {
            return this.queues?.Count > 0 ? this.queues[this.indexGenerator(this.queues)] : null;
        }

        public bool TryGetNextQueueAndRemove(out IQueue<T> queue)
        {
            if (this.queues == null || this.queues.Count == 0)
            {
                queue = null;
                return false;
            }

            int index = this.indexGenerator(this.queues);

            queue = this.queues[index];
            this.queues.RemoveAt(index);
            return true;
        }
    }
}
