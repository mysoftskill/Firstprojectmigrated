using CertInstaller;
using Microsoft.PrivacyServices.Common.Azure;

namespace CertInstaller
{
    /// <summary>
    /// A factory interface that produces key vault clients.
    /// </summary>
    public interface IAzureKeyVaultClientFactory
    {
        /// <summary>
        /// Creates a new <see cref="IAzureKeyVaultClient"/> that targets the given base URL.
        /// </summary>
        IAzureKeyVaultClient CreateKeyVaultClient(string baseUrl, ILogger logger);

        /// <summary>
        /// Creates a new <see cref="IAzureKeyVaultClient"/> using default settings for this factory.
        /// </summary>
        IAzureKeyVaultClient CreateDefaultKeyVaultClient();
    }
}
