// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataSubjectRight.Contracts.V1
{
    using System.ComponentModel.DataAnnotations;
    using Microsoft.AspNet.OData.Builder;

    /// <summary>
    ///     Deleted Item.
    /// </summary>
    public class DeletedItem
    {
        /// <summary>
        ///     Gets or sets Directory.
        /// </summary>
        [Singleton]
        public Directory Directory { get; set; }

        /// <summary>
        ///     Id.
        /// </summary>
        [Key]
        public string Id { get; set; }
    }
}
