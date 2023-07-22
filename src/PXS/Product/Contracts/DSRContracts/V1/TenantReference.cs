// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataSubjectRight.Contracts.V1
{
    using System.ComponentModel.DataAnnotations;
    using Microsoft.AspNet.OData.Builder;

    /// <summary>
    ///     Tenant Reference.
    /// </summary>
    public class TenantReference
    {
        /// <summary>
        ///     Gets or sets Directory.
        /// </summary>
        [Singleton]
        public Directory Directory { get; set; }

        /// <summary>
        ///     Tenant Id.
        /// </summary>
        [Key]
        public string TenantId { get; set; }
    }
}
