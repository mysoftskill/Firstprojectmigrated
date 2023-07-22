// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Client.Models
{
    using System;

    /// <summary>
    ///     Export Request Args.
    /// </summary>
    public class ExportOperationArgs : BasePrivacyOperationArgs
    {
        /// <summary>
        ///     Gets or sets the storage location Uri.
        /// </summary>
        public Uri StorageLocationUri { get; set; }
    }
}
