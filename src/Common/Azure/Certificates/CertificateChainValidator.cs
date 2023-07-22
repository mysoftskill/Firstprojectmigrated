//--------------------------------------------------------------------------------
// <copyright file="CertificateChainValidator.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure
{
    using System;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    ///     Checks if a certificate chain is valid using guidance from:
    ///         https://liquid.microsoft.com/Web/Object/Read/MS.Security/Requirements/Microsoft.Security.Cryptography.10015#guide
    /// </summary>
    public class CertificateChainValidator : ICertificateValidator
    {
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateChainValidator" /> class.
        /// </summary>
        /// <param name="logger">The logger</param>
        public CertificateChainValidator(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsAuthorized(X509Certificate2 cert)
        {
            bool isValid;

            // Offline check using cache
            string certSubject = cert.Subject;
            this.logger.Information(nameof(CertificateChainValidator), $"cert Subject is {certSubject}.");

            using (var chain = new X509Chain())
            {
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Offline;  // Try offline validation first
                chain.ChainPolicy.VerificationTime = DateTime.Now;  // Must be currently valid
                isValid = chain.Build(cert);

                // off-line check will fail the first time if the CRL is offline or unavailable (RevocationStatusUnknown)
                bool tryOnline = chain.ChainStatus.Any(cs => cs.Status.HasFlag(X509ChainStatusFlags.RevocationStatusUnknown));

                // If we don't have it cached, trying online will populate the cache
                if (!isValid && tryOnline)
                {
                    // Using default timeout, 15 seconds
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;

                    isValid = chain.Build(cert);
                }
            }

            this.logger.Information(nameof(CertificateChainValidator), $"The final validation result is : {isValid}.");
            return isValid;
        }
    }
}
