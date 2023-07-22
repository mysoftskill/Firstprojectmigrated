// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Storage
{
    using System.Collections.Generic;

    /// <summary>
    ///    Excluded agents from Azure app configuration.
    /// </summary>
    public class ExcludedAgentsFromAppConfig
    {
        /// <summary>
        ///     Gets or sets Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     Gets or sets excluded agents.
        /// </summary>
        public ICollection<ExcludedAgent> ExcludedAgentsJson { get; set; }
    }


}
