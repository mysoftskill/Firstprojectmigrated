// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService.Models
{
    /// <summary>
    ///     Aad Rvs Outcomes.
    ///     https://microsoft.sharepoint.com/:w:/t/DataScienceEngineering/ETThJvxBjyhPkpZjxbH8JuUBRoWCSekpQaB9GT-J6Tj7Pg?e=Hlxbic
    /// </summary>
    public enum AadRvsOutcome
    {
        /// <summary>
        ///     None.
        /// </summary>
        None,

        /// <summary>
        ///     OperationSuccess.
        /// </summary>
        OperationSuccess,

        /// <summary>
        ///     OperationSuccessPractice.
        /// </summary>
        OperationSuccessPractice,

        /// <summary>
        ///     OperationFailure
        /// </summary>
        OperationFailure,

        /// <summary>
        ///     AuthorizationValidationFailure
        /// </summary>
        AuthorizationValidationFailure,

        /// <summary>
        ///     AuthorizationMissingFailue
        /// </summary>
        AuthorizationMissingFailue,

        /// <summary>
        ///     RequestValidationFailure
        /// </summary>
        RequestValidationFailure,

        /// <summary>
        ///     DirectoryRequestFailure
        /// </summary>
        DirectoryRequestFailure,

        /// <summary>
        ///     TokenAuthorityRequestFailure
        /// </summary>
        TokenAuthorityRequestFailure
    }
}
