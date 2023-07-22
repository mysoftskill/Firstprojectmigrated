// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Models
{
    /// <summary>
    /// Delete Resource Response
    /// </summary>
    public class DeleteResourceResponse
    {
        /// <summary>
        /// Partner Id
        /// </summary>
        public string PartnerId { get; set; }

        /// <summary>
        /// Gets or sets the resource status.
        /// </summary>
        public ResourceStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}