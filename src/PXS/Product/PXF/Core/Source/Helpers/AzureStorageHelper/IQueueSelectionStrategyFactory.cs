// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper
{
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;

    public interface IQueueSelectionStrategyFactory<T>
    {
        /// <summary>
        ///     Creates a queue selection strategy that uses the provided queues
        /// </summary>
        /// <param name="queues">The queues to choose from</param>
        /// <returns>A queue selection strategy</returns>
        IQueueSelectionStrategy<T> CreateQueueSelectionStrategy(IList<IQueue<T>> queues);
    }
}
