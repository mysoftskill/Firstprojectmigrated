using System.Security.Cryptography.X509Certificates;

namespace Microsoft.PrivacyServices.UX.Core.Security
{
    /// <summary>
    /// Provides a shortcut for retrieving certificates.
    /// </summary>
    public interface ICertificateFinder
    {
        /// <summary>
        /// Finds certificate by subject name.
        /// </summary>
        /// <param name="subjectName">The serial number of the certificate.</param>
        /// <returns>The certificate.</returns>
        X509Certificate2 FindBySubjectName(string subjectName);
    }
}
