// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService
{
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.OSGS.HttpClientCommon;

    /// <summary>
    ///     Public class for AadRequestVerificationServiceAdapterFactory.
    /// </summary>
    public class AadRequestVerificationServiceAdapterFactory : IAadRequestVerficationServiceAdapterFactory
    {
        private readonly IAadAuthManager aadAuthManager;

        private readonly IAadRequestVerificationServiceAdapterConfiguration configuration;

        private readonly ICounterFactory counterFactory;

        private readonly IHttpClientFactory httpClientFactory;

        public AadRequestVerificationServiceAdapterFactory(
            IHttpClientFactory httpClientFactory,
            IPrivacyConfigurationManager configurationManager,
            ICounterFactory counterFactory,
            IAadAuthManager aadAuthManager)
        {
            this.configuration = configurationManager.AdaptersConfiguration.AadRequestVerificationServiceAdapterConfiguration;
            this.httpClientFactory = httpClientFactory;
            this.counterFactory = counterFactory;
            this.aadAuthManager = aadAuthManager;
        }

        /// <inheritdoc />
        public AadRequestVerificationServiceAdapter Create()
        {
            IHttpClient httpClient = this.httpClientFactory.CreateHttpClient(this.configuration, this.counterFactory);
            return new AadRequestVerificationServiceAdapter(httpClient, this.configuration, this.aadAuthManager, this.counterFactory);
        }
    }
}
