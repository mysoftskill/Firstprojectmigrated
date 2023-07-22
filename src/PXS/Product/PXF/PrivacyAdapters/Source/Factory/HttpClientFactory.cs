// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Factory
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Logging.LoggingFilter;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Converters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Handlers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.Common.Azure;

    using HttpClient = Microsoft.OSGS.HttpClientCommon.HttpClient;

    /// <summary>
    ///     HttpClientFactory
    /// </summary>
    internal static class HttpClientFactory
    {
        internal static IHttpClient CreateHttpClient(
            IPrivacyPartnerAdapterConfiguration partnerConfig,
            WebRequestHandler webHandler,
            ICounterFactory counterFactory,
            ILoggingFilter loggingFilter,
            bool includeOutgoingRequestHandler,
            ILogger logger)
        {
            // Validate the Certificate Root CA matches the allowed value in the config.
            webHandler.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                ServerCertificateValidation.PerformServerCertificateValidation(partnerConfig, sslPolicyErrors, logger, chain);

            var retryManager = new RetryManager(partnerConfig.RetryStrategyConfiguration, logger);

            var delegatingHandlers = new List<DelegatingHandler>
            {
                // Create RetryHandler and insert at beginning of pipeline
                new RetryHandler(retryManager),
                // Retry 429 errors if they pop up
                new RetryTooManyRequestHandler(),
            };

            if (includeOutgoingRequestHandler)
                delegatingHandlers.Add(
                    new OutgoingRequestHandler(counterFactory, logger, partnerConfig.CounterCategoryName, loggingFilter));

            var httpClient = new HttpClient(webHandler, delegatingHandlers);
            httpClient.Timeout = TimeSpan.FromMilliseconds(partnerConfig.TimeoutInMilliseconds);

            if (partnerConfig.ServicePointConfiguration != null && !string.IsNullOrWhiteSpace(partnerConfig.BaseUrl))
            {
                ServicePoint servicePoint = ServicePointManager.FindServicePoint(new Uri(partnerConfig.BaseUrl));
                servicePoint.ConnectionLimit = partnerConfig.ServicePointConfiguration.ConnectionLimit;
                servicePoint.ConnectionLeaseTimeout = partnerConfig.ServicePointConfiguration.ConnectionLeaseTimeout;
                servicePoint.MaxIdleTime = partnerConfig.ServicePointConfiguration.MaxIdleTime;
            }

            return httpClient;
        }

        internal static IHttpClient CreateHttpClient(
            IPrivacyPartnerAdapterConfiguration partnerConfig,
            X509Certificate2 cert,
            ICounterFactory counterFactory,
            ILoggingFilter loggingFilter,
            ILogger logger)
        {
            partnerConfig.ThrowOnNull("partnerConfig");
            cert.ThrowOnNull("cert");

            var webHandler = new WebRequestHandler();
            webHandler.ClientCertificates.Add(cert);

            return CreateHttpClient(partnerConfig, webHandler, counterFactory, loggingFilter, true, logger);
        }

        internal static IHttpClient CreateHttpClient(
            IPrivacyPartnerAdapterConfiguration partnerConfig,
            ICounterFactory counterFactory,
            ILoggingFilter loggingFilter,
            ILogger logger)
        {
            partnerConfig.ThrowOnNull("partnerConfig");

            return CreateHttpClient(partnerConfig, new WebRequestHandler(), counterFactory, loggingFilter, true, logger);
        }
    }
}
