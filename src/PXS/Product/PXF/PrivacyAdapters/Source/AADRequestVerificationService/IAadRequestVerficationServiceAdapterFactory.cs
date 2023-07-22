// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService
{
    /// <summary>
    ///     Public interface for IAadRequestVerficationServiceAdapterFactory.
    /// </summary>
    public interface IAadRequestVerficationServiceAdapterFactory
    {
        /// <summary>
        ///     Creates an instance of AadRequestVerificationServiceAdapter.
        /// </summary>
        /// <returns>An instance of AadRequestVerificationServiceAdapter.</returns>
        AadRequestVerificationServiceAdapter Create();
    }
}
