// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;

    public class RoundRobinQueueSelectionStrategy<T> : CustomOrderSelectionStrategy<T>
    {
        public RoundRobinQueueSelectionStrategy(IList<IQueue<T>> queues)
            : base(queues, GetRandomIndex)
        {
        }

        private static int GetRandomIndex(IList<IQueue<T>> queues)
        {
            return Math.Abs((int)DateTime.UtcNow.Ticks % queues.Count);
        }
    }
}
