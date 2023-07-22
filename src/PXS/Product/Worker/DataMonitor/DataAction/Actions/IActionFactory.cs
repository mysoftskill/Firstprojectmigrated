// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Actions
{
    /// <summary>
    ///     contract for creating action objects
    /// </summary>
    public interface IActionFactory
    {
        /// <summary>
        ///     Creates the action from an action type
        /// </summary>
        /// <param name="type">action type</param>
        /// <returns>resulting action</returns>
        IAction Create(string type);
    }
}
