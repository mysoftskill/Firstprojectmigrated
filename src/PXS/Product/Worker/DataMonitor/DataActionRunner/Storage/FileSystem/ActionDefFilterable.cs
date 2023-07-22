// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Storage.FileSystem
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;

    using Newtonsoft.Json;

    /// <summary>
    ///     extension of ActionDef to allow for filtering
    /// </summary>
    public class ActionDefFilterable :
        ActionDef,
        IFilterableLibraryObject<ActionDef>
    {
        /// <summary>
        ///     Gets or sets filter tags to exclude action definitions from an environment
        /// </summary>
        public ICollection<string> FilterTags { get; set; }

        /// <summary>
        ///     Gets the base object
        /// </summary>
        [JsonIgnore]
        public ActionDef BaseObject => this;
    }
}
