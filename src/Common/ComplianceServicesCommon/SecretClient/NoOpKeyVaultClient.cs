namespace Microsoft.Azure.ComplianceServices.Common.SecretClient
{
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    /// <summary>
    /// An implementation of IAzureKeyVaultClient that returns null for all operations. It's intended
    /// as a test hook or a object to use in place of a real implementation of IAzureKeyVaultClient
    /// when we don't yet know which keyvault we should load.
    /// </summary>
    public class NoOpKeyVaultClient : IAzureKeyVaultClient, IAzureKeyVaultClientFactory
    {
        public IAzureKeyVaultClient CreateDefaultKeyVaultClient() => this;

        public IAzureKeyVaultClient CreateKeyVaultClient(string baseUrl) => this;

        public Task<IList<X509Certificate2>> GetCertificateVersionsAsync(string name) => Task.FromResult<IList<X509Certificate2>>(null);

        public Task<X509Certificate2> GetCurrentCertificateAsync(string name) => Task.FromResult<X509Certificate2>(null);

        public Task<string> GetSecretAsync(string name) => Task.FromResult(string.Empty);
    }
}
