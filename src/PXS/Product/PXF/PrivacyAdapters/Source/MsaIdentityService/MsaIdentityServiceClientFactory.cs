// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService
{
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.ProfileIdentityService;

    /// <summary>
    ///     Msa Identity Service Client Factory
    /// </summary>
    public class MsaIdentityServiceClientFactory : IMsaIdentityServiceClientFactory
    {
        /// <inheritdoc />
        public ICredentialServiceClient CreateCredentialServiceClient(
            IPrivacyPartnerAdapterConfiguration adapterConfig,
            IMsaIdentityServiceConfiguration msaSiteConfig,
            ICertificateProvider certProvider)
        {
            return new CredentialServiceClient(adapterConfig, msaSiteConfig, certProvider);
        }

        public IProfileServiceClient CreateProfileServiceClient(
            IPrivacyPartnerAdapterConfiguration adapterConfig,
            IMsaIdentityServiceConfiguration msaSiteConfig,
            ICertificateProvider certProvider)
        {
            return new ProfileServiceClient(adapterConfig, msaSiteConfig, certProvider);
        }
    }
}
