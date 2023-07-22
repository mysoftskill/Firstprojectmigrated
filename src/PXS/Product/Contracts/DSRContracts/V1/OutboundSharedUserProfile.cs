// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.DataSubjectRight.Contracts.V1
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    ///     Public class for OutboundSharedUserProfile.
    /// </summary>
    public class OutboundSharedUserProfile
    {
        /// <summary>
        ///     Gets or sets the tenant references.
        /// </summary>
        public ICollection<TenantReference> Tenants { get; set; }

        /// <summary>
        ///     Subject Id.
        /// </summary>
        [Key]
        public string UserId { get; set; }
    }
}
