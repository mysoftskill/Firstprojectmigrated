// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Contracts.PrivacySubject
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Represents privacy subject operation response.
    /// </summary>
    public class OperationResponse
    {
        /// <summary>
        ///     Gets or sets a list of DSR IDs.
        /// </summary>
        public IList<Guid> Ids { get; set; } = new List<Guid>();
    }
}
