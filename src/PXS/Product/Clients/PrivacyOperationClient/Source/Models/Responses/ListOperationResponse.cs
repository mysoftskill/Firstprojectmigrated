// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Client.Models
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.PrivacyOperation.Contracts;

    /// <summary>
    ///     List Response.
    /// </summary>
    public class ListOperationResponse : BasePrivacyOperationResponse
    {
        /// <summary>
        ///     Gets or sets the list of operations.
        /// </summary>
        public IList<PrivacyRequestStatus> Operations { get; set; }
    }
}
