namespace Microsoft.Azure.ComplianceServices.Common.SecretClient
{
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    /// <summary>
    /// An interface capable of reading keys and certificates from an Azure Key Vault.
    /// </summary>
    public interface IAzureKeyVaultClient
    {
        /// <summary>
        /// Gets all versions of the named certificate.
        /// </summary>
        Task<IList<X509Certificate2>> GetCertificateVersionsAsync(string name);

        /// <summary>
        /// Gets only the current version of the named certificate.
        /// </summary>
        Task<X509Certificate2> GetCurrentCertificateAsync(string name);

        /// <summary>
        /// Gets the valeue of the named secret.
        /// </summary>
        Task<string> GetSecretAsync(string name);
    }
}
