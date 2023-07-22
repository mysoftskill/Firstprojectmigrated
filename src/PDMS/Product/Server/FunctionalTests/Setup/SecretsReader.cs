namespace Microsoft.PrivacyServices.DataManagement.FunctionalTests.Setup
{
    using global::Azure.Security.KeyVault.Secrets;
    using global::Azure.Identity;

    using System;
    using System.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.ComplianceServices.Common;

    public class SecretsReader
    {

        private readonly SecretClient keyVaultClient;

        public SecretsReader(string KeyVaultBaseUrl, string TenantId, string AppId, string CertificateSubjectName)
        {
            X509Certificate2 certificate = CertificateFinder.FindCertificateByName(CertificateSubjectName);
            this.keyVaultClient = new SecretClient(new Uri(KeyVaultBaseUrl), new ClientCertificateCredential(TenantId, AppId, certificate, new ClientCertificateCredentialOptions { SendCertificateChain = true }));
        }

        /// <summary>
        /// Get the secret by name.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <returns>The corresponding secret from KeyVault.</returns>
        public async Task<string> GetSecretByNameAsync(string secretName)
        {
            var secret = (await this.keyVaultClient.GetSecretAsync(secretName).ConfigureAwait(false)).Value;

            return secret.Value;
        }

        public async Task<X509Certificate2> GetCertificateByNameAsync(string certificateName)
        {
            CancellationToken cancellationToken = default(CancellationToken);
            var secret = (await this.keyVaultClient.GetSecretAsync(certificateName).ConfigureAwait(false)).Value;

            X509Certificate2 cert = new X509Certificate2(
                Convert.FromBase64String(secret.Value),
                (SecureString)null,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

            return cert;
        }
    }
}
