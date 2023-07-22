// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts
{
    using System;
    using System.Globalization;
    using System.Security.Cryptography.X509Certificates;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.Windows.Services.AuthN.Client.S2S;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Public class for XboxAccountsAdapterFactory
    /// </summary>
    public class XboxAccountsAdapterFactory : IXboxAcountsAdapterFactory
    {
        private readonly IHttpClientFactory httpClientFactory;

        /// <summary>
        ///     Initializes a new instance of the XboxAccountsAdapterFactory class
        /// </summary>
        /// <param name="httpClientFactory">HTTP client factory</param>
        public XboxAccountsAdapterFactory(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        /// <summary>
        ///     Creates a <see cref="XboxAccountsAdapter" /> based on configuration settings
        /// </summary>
        /// <param name="certProvider">Certificate Provider</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="counterFactory">The counter factory.</param>
        /// <param name="clock">The clock</param>
        public XboxAccountsAdapter Create(
            ICertificateProvider certProvider,
            IPrivacyConfigurationManager configurationManager,
            ILogger logger,
            ICounterFactory counterFactory,
            IClock clock)
        {
            IMsaIdentityServiceConfiguration msaIdentityConfig = configurationManager.MsaIdentityServiceConfiguration;
            IXboxAccountsAdapterConfiguration partnerConfig = configurationManager.AdaptersConfiguration.XboxAccountsAdapterConfiguration;

            X509Certificate2 cert = certProvider.GetClientCertificate(partnerConfig.S2SCertificateConfiguration);
            IHttpClient httpClient = this.httpClientFactory.CreateHttpClient(partnerConfig, cert, counterFactory);
            IS2SAuthClient s2sAuthClient = S2SAuthClient.Create(long.Parse(msaIdentityConfig.ClientId, CultureInfo.InvariantCulture), cert, new Uri(msaIdentityConfig.Endpoint));

            return new XboxAccountsAdapter(httpClient, partnerConfig, s2sAuthClient, logger, clock);
        }
    }
}
