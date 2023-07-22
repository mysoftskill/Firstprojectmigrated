// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.DataSubjectRight.Contracts.V1
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    ///     Public class for InboundSharedUserProfile.
    /// </summary>
    public class InboundSharedUserProfile
    {
        /// <summary>
        ///     Subject Id.
        /// </summary>
        [Key]
        public string UserId { get; set; }
    }
}
