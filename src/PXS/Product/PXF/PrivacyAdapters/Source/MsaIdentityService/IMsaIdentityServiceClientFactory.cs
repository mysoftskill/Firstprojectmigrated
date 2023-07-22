// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService
{
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.ProfileIdentityService;

    /// <summary>
    ///     Interface for MsaIdentityServiceClientFactory
    /// </summary>
    public interface IMsaIdentityServiceClientFactory
    {
        /// <summary>
        ///     Creates the credential service client
        /// </summary>
        /// <param name="adapterConfig">the adapter config</param>
        /// <param name="msaSiteConfig">the msa site config</param>
        /// <param name="certProvider">the cert provider</param>
        /// <returns>The credential service client</returns>
        ICredentialServiceClient CreateCredentialServiceClient(
            IPrivacyPartnerAdapterConfiguration adapterConfig,
            IMsaIdentityServiceConfiguration msaSiteConfig,
            ICertificateProvider certProvider);

        /// <summary>
        ///     Create the profile service client
        /// </summary>
        /// <param name="adapterConfig">the adapter config</param>
        /// <param name="msaSiteConfig">the msa site config</param>
        /// <param name="certProvider">the cert provider</param>
        /// <returns>The profile service client</returns>
        IProfileServiceClient CreateProfileServiceClient(
            IPrivacyPartnerAdapterConfiguration adapterConfig,
            IMsaIdentityServiceConfiguration msaSiteConfig,
            ICertificateProvider certProvider);
    }
}
