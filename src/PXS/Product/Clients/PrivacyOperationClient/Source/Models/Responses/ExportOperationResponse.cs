// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Client.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Export Response.
    /// </summary>
    public class ExportOperationResponse : BasePrivacyOperationResponse
    {
        /// <summary>
        ///     Gets or sets the list of Ids of created export operations. This list should only have one element at this moment.
        /// </summary>
        public IList<Guid> Ids { get; set; }
    }
}
