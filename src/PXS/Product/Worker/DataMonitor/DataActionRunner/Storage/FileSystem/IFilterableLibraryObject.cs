// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Storage.FileSystem
{
    using System.Collections.Generic;

    /// <summary>
    ///     contract for library objects that want to allow filtering
    /// </summary>
    public interface IFilterableLibraryObject<T>
    {
        /// <summary>
        ///     Gets or sets filter tags to exclude a library object from an environment
        /// </summary>
        ICollection<string> FilterTags { get; set; }

        /// <summary>
        ///     Gets the base object
        /// </summary>
        T BaseObject { get; }
    }
}
