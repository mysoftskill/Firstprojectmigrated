// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter
{
    using System;
    using System.Globalization;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Windows.Services.AuthN.Client.S2S;

    /// <summary>
    /// CustomerMasterAdapter-Factory
    /// </summary>
    public class CustomerMasterAdapterFactory : ICustomerMasterAdapterFactory
    {
        private readonly IHttpClientFactory httpClientFactory;

        /// <summary>
        ///     Initializes a new instance of the CustomerMasterAdapterFactory class
        /// </summary>
        /// <param name="httpClientFactory">HTTP client factory</param>
        public CustomerMasterAdapterFactory(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        /// <summary>
        ///     Creates a <see cref="CustomerMasterAdapter" /> based on configuration settings
        /// </summary>
        /// <param name="certProvider">Certificate Provider</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="counterFactory">The counter factory.</param>
        /// <returns>CustomerMasterAdapter</returns>
        public CustomerMasterAdapter Create(
            ICertificateProvider certProvider, 
            IPrivacyConfigurationManager configurationManager,
            ILogger logger, 
            ICounterFactory counterFactory)
        {
            var msaIdentityConfig = configurationManager.MsaIdentityServiceConfiguration;
            var partnerConfig = configurationManager.AdaptersConfiguration.CustomerMasterAdapterConfiguration;

            X509Certificate2 cert = certProvider.GetClientCertificate(msaIdentityConfig.CertificateConfiguration);
            IHttpClient httpClient = this.httpClientFactory.CreateHttpClient(partnerConfig, cert, counterFactory);
            IS2SAuthClient s2sAuthClient = S2SAuthClient.Create(long.Parse(msaIdentityConfig.ClientId, CultureInfo.InvariantCulture), cert, new Uri(msaIdentityConfig.Endpoint));

            return new CustomerMasterAdapter(httpClient, partnerConfig, s2sAuthClient, logger);
        }
    }
}