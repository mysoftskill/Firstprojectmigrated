// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Storage
{
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;

    /// <summary>
    ///     contract for objects that can execute actions
    /// </summary>
    public interface IActionExecutor
    {
        /// <summary>
        ///     Executes the specified action
        /// </summary>
        /// <param name="context">execution context</param>
        /// <param name="actionRef">action to execute</param>
        /// <returns>data object produced by the execution</returns>
        Task<object> ExecuteActionAsync(
            IExecuteContext context,
            ActionRef actionRef);
    }
}
