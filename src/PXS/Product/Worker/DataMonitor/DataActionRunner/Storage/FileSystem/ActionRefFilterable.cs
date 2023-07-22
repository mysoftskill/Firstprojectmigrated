// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Storage.FileSystem
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.DataMonitor.Runner.Data;

    using Newtonsoft.Json;

    /// <summary>
    ///     extension of ActionRefRunnable to allow for filtering
    /// </summary>
    public class ActionRefFilterable : 
        ActionRefRunnable,
        IFilterableLibraryObject<ActionRefRunnable>
    {
        /// <summary>
        ///     Gets or sets filter tags to exclude action references from an environment
        /// </summary>
        public ICollection<string> FilterTags { get; set; }

        /// <summary>
        ///     Gets the base object
        /// </summary>
        [JsonIgnore]
        public ActionRefRunnable BaseObject => this;
    }
}
