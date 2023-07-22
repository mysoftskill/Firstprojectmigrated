namespace Microsoft.PrivacyServices.DataManagement.Common.FileSystem.KeyVault
{
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// An interface that defines methods for interacting with Azure key vault.
    /// </summary>
    public interface IAzureKeyVaultReader
    {
        /// <summary>
        /// Get the certificate by name.
        /// </summary>
        /// <param name="certificateName">The certificate name.</param>
        /// <param name="includePrivateKey">include private key?</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The corresponding certificate from KeyVault.</returns>
        Task<X509Certificate2> GetCertificateByNameAsync(
            string certificateName,
            bool includePrivateKey = false, 
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get the secret by name.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The corresponding secret from KeyVault.</returns>
        Task<string> GetSecretByNameAsync(string secretName, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets a certificate from Key Vault for all enabled versions, not expired versions, with private keys, by name.
        /// </summary>
        /// <param name="name">The name of the certificate in Key Vault.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The certificates.</returns>
        /// <remarks>Loosely copied from PXS. Thanks, Doug.</remarks>
        Task<IList<X509Certificate2>> GetCertificateVersionsAsync(string name, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Get all of the certificates in the Key Vault, which includes all active/enabled versions that are not expired.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of certificates.</returns>
        Task<IList<X509Certificate2>> GetCertificatesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Get certificate with longest expiration date.
        /// </summary>
        /// <param name="name">The certificate name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The certificate with longest expiration date.</returns>
        Task<X509Certificate2> GetCertificateWithLongestExpirationDateAsync(string name, CancellationToken cancellationToken = default(CancellationToken));
    }
}