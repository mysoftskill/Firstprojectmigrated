// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Store
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;

    /// <summary>
    ///     contract for storage of actions
    /// </summary>
    public interface IActionStore : IActionFetcher
    {
        /// <summary>
        ///     Enumerates the list of contained actions
        /// </summary>
        /// <returns>list of contained actions</returns>
        ICollection<ActionDef> EnumerateActions();

        /// <summary>
        ///     Populates the store
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="preserveDefinitions">true to preserve definitions; false otherwise</param>
        /// <returns>true if the store was refreshed successfully; false otherwise</returns>
        /// <remarks>
        ///     if preserveDefinitions is false, the EnumerateActions method will return an empty list as EnumerateActions
        ///      cannot reconstruct the definitions from the parsed and processed actions and the original definitions are not
        ///      preserved if by default as they would otherwise not be required
        /// </remarks>
        Task<bool> RefreshAsync(
            IParseContext context,
            bool preserveDefinitions);

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
            ICollection<ActionDef> toAddOrUpdate);

        /// <summary>
        ///     Retrieves an action from the store and executes it
        /// </summary>
        /// <param name="context">execution context</param>
        /// <param name="actionRef">action to invoke</param>
        /// <returns>result object or null if none found</returns>
        Task<object> ExecuteActionAsync(
            IExecuteContext context,
            ActionRef actionRef);

        /// <summary>
        ///     Retrieves an action from the store and executes it
        /// </summary>
        /// <param name="context">execution context</param>
        /// <param name="tag">tag of the action to fetch</param>
        /// <returns>resulting value</returns>
        Task ExecuteActionAsync(
            IExecuteContext context,
            string tag);

        /// <summary>
        ///     Validates the action reference exists and has the correct parameters
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="actionRef">action reference</param>
        /// <returns>true if it validates successfully, false otherwise</returns>
        bool ValidateReference(
            IParseContext context,
            ActionRef actionRef);
    }
}
