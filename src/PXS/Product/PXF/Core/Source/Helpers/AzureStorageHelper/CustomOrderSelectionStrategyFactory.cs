// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;

    public class CustomOrderSelectionStrategyFactory<T> : IQueueSelectionStrategyFactory<T>
    {
        private readonly CustomOrderSelectionStrategy<T>.IndexSelector indexSelector;

        public CustomOrderSelectionStrategyFactory(CustomOrderSelectionStrategy<T>.IndexSelector indexSelector)
        {
            this.indexSelector = indexSelector ?? throw new ArgumentNullException(nameof(indexSelector));
        }

        public IQueueSelectionStrategy<T> CreateQueueSelectionStrategy(IList<IQueue<T>> queues)
        {
            return new CustomOrderSelectionStrategy<T>(queues, this.indexSelector);
        }
    }
}
