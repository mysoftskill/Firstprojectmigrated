//--------------------------------------------------------------------------------
// <copyright file="CertificateSNIValidator.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Validates a certificate based on its Subject and Issuer
    /// </summary>
    public class CertificateSNIValidator : ICertificateValidator
    {
        private readonly ILogger logger;

        private readonly List<string> allowedSubjects;

        private readonly List<string> allowedIssuers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateSNIValidator" /> class.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="allowedSubjects">The allowed subjects.</param>
        /// <param name="allowedIssuers">The allowed issuers.</param>
        public CertificateSNIValidator(ILogger logger, IEnumerable<string> allowedSubjects, IEnumerable<string> allowedIssuers)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.allowedSubjects = new List<string>(allowedSubjects);
            this.allowedIssuers = new List<string>(allowedIssuers);
        }

        /// <summary>
        /// Determines whether a certificate is in our allowed list.
        /// </summary>
        /// <param name="cert">The certificate, who's Subject/Issuer will be compared.</param>
        /// <returns>true if the <paramref name="cert" /> Subject and Issuer are contained in the allowed certificate list otherwise false.</returns>
        public bool IsAuthorized(X509Certificate2 cert)
        {
            var inAllowedList = this.allowedSubjects.Any(subject => subject.Equals(cert.Subject, StringComparison.OrdinalIgnoreCase)) &&
                this.allowedIssuers.Any(issuer => issuer.Equals(cert.Issuer, StringComparison.OrdinalIgnoreCase));

            if (!inAllowedList)
            {
                this.logger.Error(nameof(CertificateSNIValidator), $"Invalid Vortex cert: Subject={cert.Subject}, Issuer={cert.Issuer}");
            }

            return inAllowedList;
        }
    }
}
