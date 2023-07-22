// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Storage
{
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Data;

    /// <summary>
    ///     contract for classes that implement store managers
    /// </summary>
    public interface IActionAccessor
    {
        /// <summary>
        ///     Retrieves the set of actions, templates, and action references from the store
        /// </summary>
        /// <returns>resulting value</returns>
        ActionStoreContents EnumerateContents();

        /// <summary>
        ///     Retrieves the actions from storage
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="preserveDefinitions">true to preserve definitions; false otherwise</param>
        /// <returns>resulting value</returns>
        Task<bool> InitializeAndRetrieveActionsAsync(
            IParseContext context,
            bool preserveDefinitions);

        /// <summary>
        ///     Gets the actions to execute
        /// </summary>
        /// <param name="queue">queue to add actions to</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>resulting value</returns>
        Task EnqueueActionsToExecuteAsync(
            IQueue<JobWorkItem> queue,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Updates the store
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="changes">changes to update store with</param>
        /// <returns>resulting value</returns>
        Task<bool> UpdateAsync(
            IParseContext context,
            ActionStoreUpdate changes);
    }
}
