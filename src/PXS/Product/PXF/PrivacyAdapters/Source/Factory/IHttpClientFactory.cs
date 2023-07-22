// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Factory
{
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;

    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.OSGS.HttpClientCommon;

    /// <summary>
    ///     contract for classes that implement an HTTPClient factory
    /// </summary>
    public interface IHttpClientFactory
    {
        /// <summary>
        ///     Creates a new HTTP client
        /// </summary>
        /// <param name="partnerConfig">partner configuration</param>
        /// <param name="counterFactory">counter factory</param>
        /// <returns>resulting value</returns>
        IHttpClient CreateHttpClient(
            IPrivacyPartnerAdapterConfiguration partnerConfig,
            ICounterFactory counterFactory);

        /// <summary>
        ///     Creates a new HTTP client
        /// </summary>
        /// <param name="partnerConfig">partner configuration</param>
        /// <param name="cert">certificate to use in the request</param>
        /// <param name="counterFactory">counter factory</param>
        /// <returns>resulting value</returns>
        IHttpClient CreateHttpClient(
            IPrivacyPartnerAdapterConfiguration partnerConfig, 
            X509Certificate2 cert, 
            ICounterFactory counterFactory);

        /// <summary>
        ///     Creates a new HTTP client
        /// </summary>
        /// <param name="partnerConfig">partner configuration</param>
        /// <param name="webHandler">WebRequestHandler instance to use in the request</param>
        /// <param name="counterFactory">counter factory</param>
        /// <param name="includeOutgoingRequestHandler">true to include the outgoing request handler; false otherwise</param>
        /// <returns>resulting value</returns>
        IHttpClient CreateHttpClient(
            IPrivacyPartnerAdapterConfiguration partnerConfig,
            WebRequestHandler webHandler,
            ICounterFactory counterFactory,
            bool includeOutgoingRequestHandler);
    }
}
