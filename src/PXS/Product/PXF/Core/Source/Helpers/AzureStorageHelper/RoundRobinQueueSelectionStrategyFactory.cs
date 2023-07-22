// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper
{
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;

    public class RoundRobinQueueSelectionStrategyFactory<T> : IQueueSelectionStrategyFactory<T>
    {
        public IQueueSelectionStrategy<T> CreateQueueSelectionStrategy(IList<IQueue<T>> queues)
        {
            return new RoundRobinQueueSelectionStrategy<T>(queues);
        }
    }
}
