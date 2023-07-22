//--------------------------------------------------------------------------------
// <copyright file="Sha256ThumbprintCertificateValidator.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure
{
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    ///     Certificate validator implementation.
    /// </summary>
    public class Sha256HashCertificateValidator : ICertificateValidator
    {
        private readonly List<string> allowedThumbprints;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Sha256HashCertificateValidator" /> class.
        /// </summary>
        /// <param name="allowedThumbprints">The allowed thumbprints.</param>
        public Sha256HashCertificateValidator(IEnumerable<string> allowedThumbprints)
        {
            this.allowedThumbprints = new List<string>(allowedThumbprints);
        }

        /// <summary>
        ///     Determines whether a certificate thumbprint is equal to a known thumbprint.
        /// </summary>
        /// <param name="cert">The certificate, who's thumbprint will be compared.</param>
        /// <returns>true if the <paramref name="cert" /> thumbprint is contained in the allowed certificate list otherwise false.</returns>
        public bool IsAuthorized(X509Certificate2 cert)
        {
            return this.allowedThumbprints.Contains(cert.GetCertHash256Base64String());
        }
    }
}
