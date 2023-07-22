// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Storage
{
    /// <summary>
    ///     contract for objects that can create store managers
    /// </summary>
    public interface IActionManagerFactory
    {
        /// <summary>
        ///     Creates a store manager instance
        /// </summary>
        /// <returns>resulting instance</returns>
        IActionAccessor CreateStoreManager();
    }
}
