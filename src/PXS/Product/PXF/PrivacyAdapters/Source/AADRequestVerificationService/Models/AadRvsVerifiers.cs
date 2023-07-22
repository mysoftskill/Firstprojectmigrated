// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService.Models
{
    /// <summary>
    /// Verifiers returned by AAD RVS
    /// </summary>
    public class AadRvsVerifiers
    {
        /// <summary>
        ///     Gets or sets a Version 2 verifier
        /// </summary>
        public string V2 { get; set; }

        /// <summary>
        ///     Gets or sets a group of Version 3 verifiers
        /// </summary>
        public string[] V3 { get; set; }
    }
}
