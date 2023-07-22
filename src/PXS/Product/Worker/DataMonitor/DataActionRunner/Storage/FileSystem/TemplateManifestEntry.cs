// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Storage
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.DataMonitor.Runner.Storage.FileSystem;

    using Newtonsoft.Json;

    /// <summary>
    ///     represents a pointer to a template file
    /// </summary>
    /// <remarks>
    ///     template files are arbitrary text files, and as such, are more convieniently operated on as files than encoded
    ///      into JSON
    /// </remarks>
    public class TemplateManifestEntry
    {
        /// <summary>
        ///     Gets or sets the template tag
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        ///     Gets or sets name of the template file in the local directory
        /// </summary>
        public string LocalName { get; set; }
    }

    /// <summary>
    ///     represents a pointer to a template file
    /// </summary>
    /// <remarks>
    ///     template files are arbitrary text files, and as such, are more convieniently operated on as files than encoded
    ///      into JSON
    /// </remarks>
    public class TemplateManifestEntryFilterable :
        TemplateManifestEntry,
        IFilterableLibraryObject<TemplateManifestEntry>
    {
        /// <summary>
        ///     Gets or sets filter tags to exclude templates from an environment
        /// </summary>
        public ICollection<string> FilterTags { get; set; }

        /// <summary>
        ///     Gets the base object
        /// </summary>
        [JsonIgnore]
        public TemplateManifestEntry BaseObject => this;
    }
}
