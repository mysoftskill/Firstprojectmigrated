// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common
{
    using System.Security.Cryptography.X509Certificates;

    using Microsoft.Membership.MemberServices.Configuration;

    /// <summary>
    ///     Certificate provider interface.
    /// </summary>
    public interface ICertificateProvider
    {
        /// <summary>
        ///     Finds and returns a certificate installed on the machine.
        /// </summary>
        /// <param name="certConfig">Config containing details of the required cert</param>
        /// <returns>The required client certificate</returns>
        X509Certificate2 GetClientCertificate(ICertificateConfiguration certConfig = null);

        /// <summary>
        ///     Finds and returns the most recently issued certificate matching the subject.
        /// </summary>
        /// <param name="subject">The subject of the certificate.</param>
        /// <param name="storeLocation">The store location</param>
        /// <returns>The client certificate</returns>
        X509Certificate2 GetClientCertificate(string subject, StoreLocation storeLocation = StoreLocation.LocalMachine);
    }
}
