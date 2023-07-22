using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace CertInstaller
{
    /// <summary>
    /// Provides access to Azure Key Vault.
    /// </summary>
    public interface IAzureKeyVaultClient
    {
        /// <summary>
        /// Get the certificate by name.
        /// </summary>
        /// <param name="certificateName">The certificate name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The corresponding certificate from KeyVault.</returns>
        Task<X509Certificate2> GetCertificateByNameAsync(string certificateName, CancellationToken cancellationToken = default(CancellationToken));

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
    }
}
