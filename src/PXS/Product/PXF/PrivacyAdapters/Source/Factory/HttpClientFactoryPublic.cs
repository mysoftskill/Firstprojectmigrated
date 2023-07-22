// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Factory
{
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;

    using Microsoft.Membership.MemberServices.Common.Logging.LoggingFilter;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.OSGS.HttpClientCommon;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     public HTTP client factory
    /// </summary>
    public class HttpClientFactoryPublic : IHttpClientFactory
    {
        private readonly ILoggingFilter filter;

        private readonly ILogger logger;

        /// <summary>
        ///     Creates a new instance of HttpClientFactoryPublic
        /// </summary>
        /// <param name="filter">The logging filter</param>
        /// <param name="logger">The logger</param>
        public HttpClientFactoryPublic(ILoggingFilter filter, ILogger logger)
        {
            this.filter = filter;
            this.logger = logger;
        }

        /// <summary>
        ///     Creates a new HTTP client
        /// </summary>
        /// <param name="partnerConfig">partner configuration</param>
        /// <param name="counterFactory">counter factory</param>
        /// <returns>resulting value</returns>
        public IHttpClient CreateHttpClient(
            IPrivacyPartnerAdapterConfiguration partnerConfig,
            ICounterFactory counterFactory)
        {
            return HttpClientFactory.CreateHttpClient(partnerConfig, counterFactory, this.filter, this.logger);
        }

        /// <summary>
        ///     Creates a new HTTP client
        /// </summary>
        /// <param name="partnerConfig">partner configuration</param>
        /// <param name="cert">certificate to use in the request</param>
        /// <param name="counterFactory">counter factory</param>
        /// <returns>resulting value</returns>
        public IHttpClient CreateHttpClient(
            IPrivacyPartnerAdapterConfiguration partnerConfig,
            X509Certificate2 cert,
            ICounterFactory counterFactory)
        {
            return HttpClientFactory.CreateHttpClient(partnerConfig, cert, counterFactory, this.filter, this.logger);
        }

        /// <summary>
        ///     Creates a new HTTP client
        /// </summary>
        /// <param name="partnerConfig">partner configuration</param>
        /// <param name="webHandler">WebRequestHandler instance to use in the request</param>
        /// <param name="counterFactory">counter factory</param>
        /// <param name="includeOutgoingRequestHandler">true to include the outgoing request handler; false otherwise</param>
        /// <returns>resulting value</returns>
        public IHttpClient CreateHttpClient(
            IPrivacyPartnerAdapterConfiguration partnerConfig,
            WebRequestHandler webHandler,
            ICounterFactory counterFactory,
            bool includeOutgoingRequestHandler)
        {
            return HttpClientFactory.CreateHttpClient(
                partnerConfig,
                webHandler,
                counterFactory,
                this.filter,
                includeOutgoingRequestHandler,
                this.logger);
        }
    }
}
