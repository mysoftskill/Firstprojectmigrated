// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.DataSubjectRight.Contracts.V1
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    ///     Public class for User.
    /// </summary>
    public class User
    {
        /// <summary>
        ///     Id.
        /// </summary>
        [Key]
        public string Id { get; set; }
    }
}
