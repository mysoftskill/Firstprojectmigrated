// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Azure.ComplianceServices.Common
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;

    public static class CertHelper
    {
        /// <summary>
        ///     Get certificates exactly match the given subject name
        ///     X509FindType.FindBySubjectName uses substring match (i.e. "a.subject", "subject.b", "subject" all match "subject")
        ///     This method make sure only certs with the exact subject name remain
        /// </summary>
        /// <param name="certs">The certs</param>
        /// <param name="subject">The subject name to match.</param>
        /// <returns>A collection of <see cref="X509Certificate2" /> match the given subject name</returns>
        public static IEnumerable<X509Certificate2> GetCertsWithExactSubjectName(IEnumerable<X509Certificate2> certs, string subject)
        {
            return certs?.Where(c => string.Compare(subject, c.GetNameInfo(X509NameType.SimpleName, false), System.StringComparison.OrdinalIgnoreCase) == 0);
        }

        /// <summary>
        ///     Get the certificate with the most recent issue date
        /// </summary>
        /// <param name="certs">The certs</param>
        /// <param name="requirePrivateKey">If true, private key is required.</param>
        /// <returns>The <see cref="X509Certificate2" /> with the most recent issue date</returns>
        public static X509Certificate2 GetCertWithMostRecentIssueDate(IEnumerable<X509Certificate2> certs, bool requirePrivateKey = true)
        {
            return requirePrivateKey ?
                certs?.Where(HasPrivateKeyAndAccess).OrderByDescending(c => c.NotBefore).FirstOrDefault() :
                certs?.OrderByDescending(c => c.NotBefore).FirstOrDefault();
        }

        private static bool HasPrivateKeyAndAccess(X509Certificate2 cert)
        {
            try
            {
                // By evaluating cert.PrivateKey, access is checked to read the key.
                if (cert != null && cert.HasPrivateKey)
                {
                    return true;
                }
            }
            catch (CryptographicException e)
            {
                // If there's no access to read the key, this exception is thrown
                Trace.TraceWarning(
                    "No read access to private key. This is an expected exception if NetworkService doesn't have read access. " +
                    "This might happen if a new certificate was installed with private key, but it does not have the correct ACL configured." +
                    $"Exception: {e}");
            }

            return false;
        }
    }
}
