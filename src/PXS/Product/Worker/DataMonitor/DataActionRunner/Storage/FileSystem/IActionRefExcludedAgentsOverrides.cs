// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Storage
{
    /// <summary>
    ///     IActionRefExcludedAgentsOverrides
    /// </summary>
    public interface IActionRefExcludedAgentsOverrides
    {
        /// <summary>
        ///     Merge original ActionRef Json file with excluded agents overrides from Azure app configuration.
        /// </summary>
        /// <param name="contents">Json content of original ActionRef.</param>
        /// <returns>Merged ActionRef.</returns>
        string MergeExcludedAgentsOverrides(string contents);
    }
}