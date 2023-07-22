// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Storage
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Data;

    /// <summary>
    ///     contract for storage of action references
    /// </summary>
    public interface IActionRefStore
    {
        /// <summary>
        ///     Enumerates the list of contained actions
        /// </summary>
        /// <returns>list of contained actions</returns>
        ICollection<ActionRefRunnable> EnumerateReferences();

        /// <summary>
        ///     Populates the store
        /// </summary>
        /// <param name="context">parse context</param>
        /// <returns>true if the store was refreshed successfully; false otherwise</returns>
        Task<bool> RefreshAsync(IParseContext context);

        /// <summary>
        ///     Updates the actions in the store by adding, updated, or removing them
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="toRemove">set of actions to remove</param>
        /// <param name="toAddOrUpdate">set of actions to add or update</param>
        /// <returns>resulting value</returns>
        Task<bool> UpdateAsync(
            IParseContext context,
            ICollection<string> toRemove,
            ICollection<ActionRefRunnable> toAddOrUpdate);
    }
}
