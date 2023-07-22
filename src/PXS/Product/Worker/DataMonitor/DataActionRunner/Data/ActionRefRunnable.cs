// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Data
{
    using System;
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;

    /// <summary>
    ///     An action ref with an associated expected max runtime
    /// </summary>
    public class ActionRefRunnable : ActionRef
    {
        /// <summary>
        ///     Gets or sets extension properties
        /// </summary>
        public IDictionary<string, string> Templates { get; set;  }

        /// <summary>
        ///     Gets or sets the number of seconds to allow the task to run for
        /// </summary>
        public TimeSpan MaxRuntime { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this reference should be run as a simulation
        /// </summary>
        public bool IsSimulation { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating to perform verbose logging or not
        /// </summary>
        public bool EmitVerboseLogging { get; set; }
    }
}
