//--------------------------------------------------------------------------------
// <copyright file="ICertificateValidator.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure
{
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    ///     Authenticates a client certificate associated with an HTTP request.
    /// </summary>
    public interface ICertificateValidator
    {
        /// <summary>
        ///     Determines whether a certificate thumbprint is equal to a known thumbprint.
        /// </summary>
        /// <param name="cert">The certificate, whos thumbprint will be compared.</param>
        /// <returns>true if the <paramref name="cert" /> thumbprint is equal to the specified <paramref name="cert" />; otherwise false.</returns>
        bool IsAuthorized(X509Certificate2 cert);
    }
}
