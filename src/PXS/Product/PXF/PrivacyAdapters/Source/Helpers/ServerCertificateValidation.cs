// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers
{
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     ServerCertificateValidation
    /// </summary>
    public static class ServerCertificateValidation
    {
        /// <summary>
        ///     Globally ignore server certificate validation. Used by PartnerTestClient.
        /// </summary>
        public static bool GlobalSkipServerCertificateValidation { get; set; } = false;

        internal static bool PerformServerCertificateValidation(
            IPrivacyPartnerAdapterConfiguration partnerConfig,
            SslPolicyErrors sslPolicyErrors,
            ILogger logger,
            X509Chain chain)
        {
            if (GlobalSkipServerCertificateValidation)
            {
                return true;
            }

            if (partnerConfig.SkipServerCertValidation)
            {
                return true;
            }

            // Ensure the certificate validates according to normal PKI checks.
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                logger.Error(nameof(ServerCertificateValidation), "SSL Policy error occurred: {0}", sslPolicyErrors.ToString());
                return false;
            }

            if (chain == null || chain.ChainElements == null)
            {
                logger.Error(nameof(ServerCertificateValidation), "SSL chain is null or chain elements is null.");
                return false;
            }

            if (chain.ChainElements.Count < 2)
            {
                logger.Error(nameof(ServerCertificateValidation), "SSL chain contains less than 2 elements.");
                return false;
            }

            // Ensure the root of the CA chain is one of the known CA subject names.
            X509ChainElement rootChainElement = chain.ChainElements[chain.ChainElements.Count - 1];

            if (rootChainElement == null)
            {
                logger.Error(nameof(ServerCertificateValidation), "Root chain element is null.");
                return false;
            }

            if (rootChainElement.Certificate == null)
            {
                logger.Error(nameof(ServerCertificateValidation), "Root chain element certificate is null.");
                return false;
            }

            return true;
        }
    }
}
