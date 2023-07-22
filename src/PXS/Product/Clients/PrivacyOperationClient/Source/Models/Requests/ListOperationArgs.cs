// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Client.Models
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.PrivacyOperation.Contracts;

    /// <summary>
    ///     List Request Args.
    /// </summary>
    public class ListOperationArgs : BasePrivacyOperationArgs
    {
        /// <summary>
        ///     The types of requests to list. Null or empty lists all.
        /// </summary>
        public IList<PrivacyRequestType> RequestTypes { get; set; }
    }
}
