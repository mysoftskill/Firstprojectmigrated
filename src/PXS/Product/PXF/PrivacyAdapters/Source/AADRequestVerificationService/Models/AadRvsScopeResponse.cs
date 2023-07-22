// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService.Models
{
    /// <summary>
    ///     AAD RVS scope response body.
    /// </summary>
    public class AadRvsScopeResponse : AadRvsResponse
    {
        /// <summary>
        ///     Gets or set the scopes
        /// </summary>
        public string Scopes { get; set; }
    }
}
