namespace Microsoft.PrivacyServices.AzureFunctions.Common
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using global::Azure;
    using global::Azure.Core;
    using global::Azure.Identity;
    using global::Azure.Security.KeyVault.Certificates;
    using global::Azure.Security.KeyVault.Secrets;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.AzureServiceUrlValidator;

    /// <summary>
    /// Class to get secrets from key vault.
    /// </summary>
    public class SecretsReader
    {
        private readonly SecretClient secretClient;
        private readonly CertificateClient certClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecretsReader"/> class.
        /// </summary>
        /// <param name="clientId">Client accessing key vault.</param>
        /// <param name="tenantId">Tenant id of Client.</param>
        /// <param name="certificateSubjectName">Name of certificate needed to access key vault; should be in LocalMachine store.</param>
        /// <param name="keyVaultBaseUrl">The url for the key vault that holds the secrets.</param>
        /// <param name="relativePath">Relative path to the secret which needs to be validated.</param>
        public SecretsReader(string clientId, string tenantId, string certificateSubjectName, string keyVaultBaseUrl, string relativePath = "")
        {
            X509Certificate2 certificate;
            try
            {
                certificate = CertificateFinder.FindCertificateByName(certificateSubjectName);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error reading certificate {certificateSubjectName}: {e.Message}");
                throw e;
            }

            // Validate key vault url
            Task.Run(() =>
            {
                var result = KeyServiceUrlValidator.IsValidKeyServiceUrlAsync(new Uri(keyVaultBaseUrl + relativePath), "public", Guid.Parse(tenantId)).GetAwaiter().GetResult();
                if (!result.IsValid)
                {
                    throw new UnauthorizedAccessException($"The url to KeyVault {keyVaultBaseUrl}{relativePath} could not be validated. {result.Reason}");
                }
            }).Wait();

            var clientCredentials = new ClientCertificateCredential(tenantId, clientId, certificate, new ClientCertificateCredentialOptions { SendCertificateChain = true });

            // Create a secret client as a confidential client
            this.secretClient = new SecretClient(new Uri(keyVaultBaseUrl), clientCredentials);

            // Create a certificate client as a confidential client
            this.certClient = new CertificateClient(new Uri(keyVaultBaseUrl), clientCredentials);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecretsReader"/> class.
        /// </summary>
        /// <param name="keyVaultBaseUrl">The url for the key vault that holds the secrets.</param>
        /// <param name="clientCredentials">The token credentials for accesing the keyvault.</param>
        /// <param name="relativePath">Relative path to the secret which needs to be validated.</param>
        public SecretsReader(string keyVaultBaseUrl, TokenCredential clientCredentials, string relativePath = "")
        {
            // Validate key vault url
            Task.Run(() =>
            {
                var result = KeyServiceUrlValidator.IsValidKeyServiceUrlAsync(new Uri(keyVaultBaseUrl + relativePath), "public").GetAwaiter().GetResult();
                if (!result.IsValid)
                {
                    throw new UnauthorizedAccessException($"The url to KeyVault {keyVaultBaseUrl}{relativePath} could not be validated. {result.Reason}");
                }
            }).Wait();

            // Create a secret client as a confidential client
            this.secretClient = new SecretClient(new Uri(keyVaultBaseUrl), clientCredentials);

            // Create a certificate client as a confidential client
            this.certClient = new CertificateClient(new Uri(keyVaultBaseUrl), clientCredentials);
        }

        /// <summary>
        /// Get the secret by name.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <returns>The corresponding secret from KeyVault.</returns>
        public async Task<string> GetSecretByNameAsync(string secretName)
        {
            KeyVaultSecret secret = null;
            try
            {
                secret = await this.secretClient.GetSecretAsync(secretName);
            }
            catch (AuthenticationFailedException e)
            {
                Console.WriteLine($"Authentication Failed. {e.Message}");
            }

            return secret.Value;
        }

        /// <summary>
        /// Get the certificate by name.
        /// </summary>
        /// <param name="certificateName">The certificate name.</param>
        /// <returns>The corresponding certificate from KeyVault.</returns>
        public async Task<X509Certificate2> GetCertificateByNameAsync(string certificateName)
        {
            X509Certificate2 certificate = null;
            try
            {
                // Get cert with private key
                Response<X509Certificate2> response = await this.certClient.DownloadCertificateAsync(certificateName).ConfigureAwait(false);

                certificate = response.Value;
            }
            catch (AuthenticationFailedException e)
            {
                Console.WriteLine($"Authentication Failed. {e.Message}");
            }

            return certificate;
        }
    }
}