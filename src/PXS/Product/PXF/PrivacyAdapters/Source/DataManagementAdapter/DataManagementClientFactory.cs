// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.DataManagementAdapter
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;
    using Microsoft.Windows.Services.AuthN.Client.S2S;

    /// <summary>
    ///     DataManagementClientFactory
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DataManagementClientFactory : IDataManagementClientFactory
    {
        private readonly IHttpClientFactory httpClientFactory;

        /// <summary>
        ///     Initializes a new instance of the DataManagementClientFactory class
        /// </summary>
        /// <param name="httpClientFactory">HTTP client factory</param>
        public DataManagementClientFactory(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        /// <summary>
        ///     Creates the <see cref="DataManagementClient" />.
        /// </summary>
        /// <param name="certProvider">The cert provider.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="counterFactory">The counter factory.</param>
        /// <returns>DataManagementClient</returns>
        public IDataManagementClient Create(
            ICertificateProvider certProvider,
            IPrivacyConfigurationManager configurationManager,
            ICounterFactory counterFactory)
        {
            IMsaIdentityServiceConfiguration msaIdentityConfig = configurationManager.MsaIdentityServiceConfiguration;
            IPrivacyPartnerAdapterConfiguration partnerConfig = configurationManager.AdaptersConfiguration.DataManagementAdapterConfiguration;

            X509Certificate2 cert = certProvider.GetClientCertificate(msaIdentityConfig.CertificateConfiguration);
            IHttpClient httpClient = this.httpClientFactory.CreateHttpClient(partnerConfig, cert, counterFactory);
            httpClient.BaseAddress = new Uri(partnerConfig.BaseUrl);

            ServicePoint servicePoint = ServicePointManager.FindServicePoint(httpClient.BaseAddress);
            servicePoint.MaxIdleTime = partnerConfig.ServicePointConfiguration.MaxIdleTime;
            servicePoint.ConnectionLeaseTimeout = partnerConfig.ServicePointConfiguration.ConnectionLeaseTimeout;
            servicePoint.ConnectionLimit = partnerConfig.ServicePointConfiguration.ConnectionLimit;

            IS2SAuthClient s2sAuthClient = S2SAuthClient.Create(long.Parse(msaIdentityConfig.ClientId, CultureInfo.InvariantCulture), cert, new Uri(msaIdentityConfig.Endpoint));
            IHttpServiceProxy httpServiceProxy = new DataManagementHttpServiceProxy(httpClient, s2sAuthClient, partnerConfig);
            return new DataManagementClient(httpServiceProxy);
        }

        /// <summary>
        ///     Creates the <see cref="DataManagementClient" />.
        /// </summary>
        /// <param name="aadAuthManager">AAD auth manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="counterFactory">The counter factory.</param>
        /// <returns>DataManagementClient</returns>
        public IDataManagementClient Create(
            IAadAuthManager aadAuthManager,
            IPrivacyConfigurationManager configurationManager,
            ICounterFactory counterFactory)
        {
            IPrivacyPartnerAdapterConfiguration partnerConfig = configurationManager.AdaptersConfiguration.DataManagementAdapterConfiguration;

            IHttpClient httpClient = this.httpClientFactory.CreateHttpClient(partnerConfig, counterFactory);
            httpClient.BaseAddress = new Uri(partnerConfig.BaseUrl);

            ServicePoint servicePoint = ServicePointManager.FindServicePoint(httpClient.BaseAddress);
            servicePoint.MaxIdleTime = partnerConfig.ServicePointConfiguration.MaxIdleTime;
            servicePoint.ConnectionLeaseTimeout = partnerConfig.ServicePointConfiguration.ConnectionLeaseTimeout;
            servicePoint.ConnectionLimit = partnerConfig.ServicePointConfiguration.ConnectionLimit;

            IHttpServiceProxy httpServiceProxy = new DataManagementHttpServiceProxy(httpClient, aadAuthManager, partnerConfig);
            return new DataManagementClient(httpServiceProxy);
        }

    }
}
