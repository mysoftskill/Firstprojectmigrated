// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher
{
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Creates a PartnerAdapter based on configuration settings
    /// </summary>
    public interface IPxfAdapterFactory
    {
        /// <summary>
        ///     Creates a PartnerAdapter based on configuration settings
        /// </summary>
        /// <param name="certProvider">Certificate Provider</param>
        /// <param name="msaIdentityConfig">MSA S2S auth config</param>
        /// <param name="partnerConfig">Partner Configuration</param>
        /// <param name="aadTokenProvider">The AAD token provider</param>
        /// <param name="logger">The logger.</param>
        /// <param name="counterFactory">The counter factory.</param>
        /// <returns>PartnerAdapter</returns>
        PartnerAdapter Create(
            ICertificateProvider certProvider,
            IMsaIdentityServiceConfiguration msaIdentityConfig,
            IPxfPartnerConfiguration partnerConfig,
            IAadTokenProvider aadTokenProvider,
            ILogger logger,
            ICounterFactory counterFactory);
    }
}
