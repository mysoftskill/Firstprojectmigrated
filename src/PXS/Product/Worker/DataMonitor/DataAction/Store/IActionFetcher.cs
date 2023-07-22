// ---------------------------------------------------------------------------
// <copyright file="IActionFetcher.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Store
{
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Actions;

    /// <summary>
    ///     contract for fetching actions
    /// </summary>
    public interface IActionFetcher
    {
        /// <summary>
        ///     Retrieves an action from the store
        /// </summary>
        /// <param name="tag">tag of the action to fetch</param>
        /// <returns>requested action</returns>
        IAction GetAction(string tag);
    }
}
