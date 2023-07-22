// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Client.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Delete Request Response.
    /// </summary>
    public class DeleteOperationResponse : BasePrivacyOperationResponse
    {
        /// <summary>
        ///     Gets or sets the list of Ids of created delete operations. This list should only have one element at the moment.
        /// </summary>
        public IList<Guid> Ids { get; set; }
    }
}
