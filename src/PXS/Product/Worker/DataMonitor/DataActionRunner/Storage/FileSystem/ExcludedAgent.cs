// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Storage
{
    /// <summary>
    ///    Excluded agent
    /// </summary>
    public class ExcludedAgent
    {
        /// <summary>
        ///     Gets or sets agent Id.
        /// </summary>
        public string AgentId { get; set; }

        /// <summary>
        ///     Gets or sets expiration dates.
        /// </summary>
        public string Expires { get; set; }
    }
}
