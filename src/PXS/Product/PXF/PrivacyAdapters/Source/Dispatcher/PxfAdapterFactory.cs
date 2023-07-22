// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Security.Cryptography.X509Certificates;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.BeaconAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.V2;
    using Microsoft.Windows.Services.AuthN.Client.S2S;

    using Osgs = Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Creates a PartnerAdapter based on configuration settings
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class PxfAdapterFactory : IPxfAdapterFactory
    {
        private readonly IHttpClientFactory httpClientFactory;

        /// <summary>
        ///     Initializes a new instance of the PxfAdapterFactory class
        /// </summary>
        /// <param name="httpClientFactory">HTTP client factory</param>
        public PxfAdapterFactory(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }
        
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
        /// <exception cref="System.NotSupportedException">Can happen if <see cref="AdapterVersion" /> specified is not supported.</exception>
        public PartnerAdapter Create(
            ICertificateProvider certProvider,
            IMsaIdentityServiceConfiguration msaIdentityConfig,
            IPxfPartnerConfiguration partnerConfig,
            IAadTokenProvider aadTokenProvider,
            ILogger logger,
            ICounterFactory counterFactory)
        {
            ValidateBasePartnerConfig(partnerConfig);

            // Create a specific adapter instance for this partner based on their current PXF API version in use
            IPxfAdapter adapter;

            Osgs.IHttpClient httpClient;

            switch (partnerConfig.PxfAdapterVersion)
            {
                case AdapterVersion.PxfV1:
                    throw new NotSupportedException($"{partnerConfig.PxfAdapterVersion} is not supported.");

                case AdapterVersion.PdApiV2:

                    X509Certificate2 cert = certProvider.GetClientCertificate(msaIdentityConfig.CertificateConfiguration);
                    httpClient = this.httpClientFactory.CreateHttpClient(partnerConfig, cert, counterFactory);

                    // Create the S2S Client
                    S2SAuthClient s2sAuthClient = S2SAuthClient.Create(
                        long.Parse(msaIdentityConfig.ClientId, CultureInfo.InvariantCulture),
                        cert,
                        new Uri(msaIdentityConfig.Endpoint));

                    adapter = new PdApiAdapterV2(
                        httpClient, 
                        s2sAuthClient, 
                        partnerConfig, 
                        certProvider, 
                        aadTokenProvider,
                        logger);
                    break;
  
                case AdapterVersion.BeaconV1:
                    httpClient = this.httpClientFactory.CreateHttpClient(partnerConfig, counterFactory);
                    adapter = new BeaconAdapter(
                        httpClient,
                        partnerConfig,
                        aadTokenProvider,
                        logger);
                    break;
                default:
                    throw new NotSupportedException(
                        $"Unknown {nameof(partnerConfig.PxfAdapterVersion)}: '{partnerConfig.PxfAdapterVersion}' specified for " +
                        $"PartnerId: '{partnerConfig.PartnerId}', " +
                        $"AgentFriendlyName: '{partnerConfig.AgentFriendlyName}', " +
                        $"Id: '{partnerConfig.Id}'");
            }

            // Return the partner adapater
            var partnerAdapter = new PartnerAdapter
            {
                Adapter = adapter,
                PartnerId = partnerConfig.PartnerId,
                RealTimeDelete = partnerConfig.RealTimeDelete,
                RealTimeView = partnerConfig.RealTimeView
            };
            return partnerAdapter;
        }

        private static string CreateErrorMessageSuffixForConfiguration(IPxfPartnerConfiguration config)
        {
            return $"PartnerId: '{config.PartnerId}', AgentFriendlyName: '{config.AgentFriendlyName}'";
        }

        private static void ValidateBasePartnerConfig(IPxfPartnerConfiguration config)
        {
            string errorMessageSuffix = CreateErrorMessageSuffixForConfiguration(config);

            Uri baseUri;
            if (!Uri.TryCreate(config.BaseUrl, UriKind.Absolute, out baseUri))
                throw new ArgumentException(
                    $"BaseUrl is not a valid Uri: '{config.BaseUrl}'. {errorMessageSuffix}");

            if (config.TimeoutInMilliseconds <= 0)
                throw new ArgumentException($"Invalid TimeoutInMilliseconds. Value should be greater than 0. Actual value: {config.TimeoutInMilliseconds}. {errorMessageSuffix}");
        }

        private static void ValidatePdpPartnerConfig(IPxfPartnerConfiguration config)
        {
            if (config.FacetDomain == FacetDomain.Unknown)
                throw new ArgumentException($"Unknown FacetDomain value. {CreateErrorMessageSuffixForConfiguration(config)}");
        }

        private static void ValidatePxfPartnerConfig(IPxfPartnerConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.MsaS2STargetSite))
                throw new ArgumentException($"Missing MsaS2STargetSite value. {CreateErrorMessageSuffixForConfiguration(config)}");
        }
    }
}
