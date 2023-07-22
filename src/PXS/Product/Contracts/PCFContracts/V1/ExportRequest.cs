// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PXS.Command.Contracts.V1
{
    using System;

    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    ///     Export Request
    /// </summary>
    public class ExportRequest : PrivacyRequest
    {
        /// <summary>
        ///     Gets and sets storage URI for the export request
        /// </summary>
        [JsonProperty("storageUri")]
        public Uri StorageUri { get; set; }

        /// <summary>
        ///     Gets or sets the set of data types for this export request.
        /// </summary>
        [JsonProperty("privacyDataTypes")]
        public IEnumerable<string> PrivacyDataTypes { get; set; }
    }
}
