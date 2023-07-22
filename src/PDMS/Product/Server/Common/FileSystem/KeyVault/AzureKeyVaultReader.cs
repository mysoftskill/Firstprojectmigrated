namespace Microsoft.PrivacyServices.DataManagement.Common.FileSystem.KeyVault
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Models;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.Rest.Azure;

    /// <summary>
    /// Contains methods for interacting with the Azure key vault.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AzureKeyVaultReader : IAzureKeyVaultReader
    {
        private readonly AzureServiceTokenProvider azureServiceTokenProvider;

        private readonly string keyVaultBaseUrl;
        private readonly KeyVaultClient keyVaultClient;

        private readonly IEventWriterFactory eventWriterFactory;
        private readonly IAuditLogger auditLogger;
        private readonly string subscriptionId;
        private const string CallerName = "ADGCS PDMS " + nameof(AzureKeyVaultReader);

        /// <summary>
        ///     Initializes a new instance of the <see cref="AzureKeyVaultReader"/> class.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="eventWriterFactory">The event writer factory.</param>
        public AzureKeyVaultReader(IPrivacyConfigurationManager configurationManager, IEventWriterFactory eventWriterFactory)
        {
            azureServiceTokenProvider = new AzureServiceTokenProvider(configurationManager.CoreConfiguration.AzureServicesAuthConnectionString);

            IAzureKeyVaultConfig keyVaultConfig = configurationManager.AzureKeyVaultConfig;

            this.keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(this.azureServiceTokenProvider.KeyVaultTokenCallback));
            this.keyVaultBaseUrl = keyVaultConfig.VaultBaseUrl;
            this.eventWriterFactory = eventWriterFactory;
            this.auditLogger = AuditLoggerFactory.CreateAuditLogger(DualLogger.Instance);
            this.subscriptionId = configurationManager.AzureActiveDirectoryProviderConfig.UAMISubscriptionId;
        }

        /// <summary>
        /// Get the certificate by name.
        /// </summary>
        /// <param name="certificateName">The certificate name.</param>
        /// <param name="includePrivateKey">include private key?</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The corresponding certificate from KeyVault.</returns>
        public async Task<X509Certificate2> GetCertificateByNameAsync(
            string certificateName,
            bool includePrivateKey = false, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                X509Certificate2 certificate;

                if (includePrivateKey)
                {
                    SecretBundle secretBundle = await this.keyVaultClient.GetSecretAsync(this.keyVaultBaseUrl, certificateName, cancellationToken)
                        .ConfigureAwait(false);
                    this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, certificateName, "Successfully retrieved secret.", OperationResult.Success);

                    certificate = new X509Certificate2(
                        Convert.FromBase64String(secretBundle.Value),
                        (SecureString)null,
                        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
                }
                else
                {
                    CertificateBundle certificateBundle = await this.keyVaultClient.GetCertificateAsync(this.keyVaultBaseUrl, certificateName, cancellationToken).ConfigureAwait(false);
                    this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, certificateName, "Successfully retrieved certificate.", OperationResult.Success);

                    certificate = new X509Certificate2(certificateBundle.Cer);
                }
                DualLogger.Instance.Information(nameof(AzureKeyVaultReader), certificate.ToLogMessage("PDMS"));
                return certificate;
            }
            catch (Exception ex)
            {
                DualLogger.Instance.Information(nameof(AzureKeyVaultReader), $"Failed to retrieve cert {certificateName} from key vault. Exception is {ex.ToString()}");
                this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, certificateName, $"Failed to retrieve secret/certificate by name from key vault. Exception is {ex.ToString()}", OperationResult.Failure);

                throw;
            }
        }

        /// <summary>
        /// Get the secret by name.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The corresponding secret from KeyVault.</returns>
        public async Task<string> GetSecretByNameAsync(string secretName, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                Func<string, Task<string>> secretRetrieveFunc =
                    async (name)
                    =>
                    {
                        SecretBundle secretBundle = await this.keyVaultClient.GetSecretAsync(this.keyVaultBaseUrl, name, cancellationToken).ConfigureAwait(false);
                        this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, name, "Successfully retrieved secret.", OperationResult.Success);
                        return secretBundle.Value;
                    };

                return await this.GetKeyVaultEntityByNameAsync<string>(secretRetrieveFunc, secretName, "secret").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, secretName, $"Failed to retrieve secret by name from key vault. Exception is {ex.ToString()}", OperationResult.Failure);
                throw;
            }
        }

        /// <summary>
        /// Gets a certificate from Key Vault for all enabled versions, not expired versions, with private keys, by name.
        /// </summary>
        /// <param name="name">The name of the certificate in Key Vault.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The certificates.</returns>
        /// <remarks>Loosely copied from PXS. Thanks, Doug.</remarks>
        public async Task<IList<X509Certificate2>> GetCertificateVersionsAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                Func<SecretItem, Task<X509Certificate2>> getMachineKeyCertificateFromSecretItemAsync = async (secretItem) =>
                {
                    Func<string, Task<X509Certificate2>> certificateRetrieveFunc =
                        async (n) =>
                        {
                            SecretBundle secretBundle = await this.keyVaultClient.GetSecretAsync(this.keyVaultBaseUrl, n, secretItem.Identifier.Version, cancellationToken).ConfigureAwait(false);
                            this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, n, $"Retrieved secret with version {secretItem.Identifier.Version} from key vault", OperationResult.Success);

                            if (secretBundle == null)
                            {
                                this.eventWriterFactory.Trace(nameof(AzureKeyVaultReader), "Secret bundle was null");
                                return null;
                            }

                            return new X509Certificate2(
                                Convert.FromBase64String(secretBundle.Value),
                                (SecureString)null,
                                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
                        };

                    return await this.GetKeyVaultEntityByNameAsync<X509Certificate2>(certificateRetrieveFunc, name, "certificate").ConfigureAwait(false);
                };

                this.eventWriterFactory.Trace(nameof(AzureKeyVaultReader), $"Looking for certs for {name}");

                List<X509Certificate2> certs = new List<X509Certificate2>();

                // In order to get the cert with private key, it must be read from GetSecretAsync
                // The method GetCertificateAsync method only returns the public cer (by Key Vault design)
                IPage<SecretItem> secretPage = null;
                do
                {
                    Func<string, Task<IPage<SecretItem>>> certificatePageRetrieveFunc =
                        async (n)
                        =>
                        {
                            IPage<SecretItem> page = null;
                            if (string.IsNullOrWhiteSpace(secretPage?.NextPageLink))
                            {
                                page = await this.keyVaultClient.GetSecretVersionsAsync(this.keyVaultBaseUrl, n, cancellationToken: cancellationToken).ConfigureAwait(false);
                                this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, n, "Successfully retrieved secret versions of current page from key vault.", OperationResult.Success);
                                return page;
                            }
                            else
                            {
                                page = await this.keyVaultClient.GetSecretVersionsNextAsync(secretPage.NextPageLink, cancellationToken: cancellationToken).ConfigureAwait(false);
                                this.LogAccessToKeyVaultAuditData(secretPage.NextPageLink, name, "Successfully retrieved the secret versions of next page from key vault.", OperationResult.Success);
                                return page;
                            }
                        };

                    secretPage = await this.GetKeyVaultEntityByNameAsync(certificatePageRetrieveFunc, name, "certificate page").ConfigureAwait(false);

                    foreach (SecretItem secretItem in secretPage)
                    {
                        this.eventWriterFactory.Trace(nameof(AzureKeyVaultReader), $"Cert: {secretItem?.Id}, Enabled = {secretItem?.Attributes?.Enabled}, Expires = {secretItem?.Attributes?.Expires}");
                        if (secretItem?.Attributes?.Enabled != true)
                        {
                            this.eventWriterFactory.Trace(nameof(AzureKeyVaultReader), "Not enabled.");
                            continue;
                        }

                        if (secretItem.Attributes.Expires != null && secretItem.Attributes.Expires < DateTimeOffset.UtcNow)
                        {
                            this.eventWriterFactory.Trace(nameof(AzureKeyVaultReader), "Already expired.");
                            continue;
                        }

                        X509Certificate2 cert = await getMachineKeyCertificateFromSecretItemAsync(secretItem).ConfigureAwait(false);
                        if (cert != null)
                        {
                            this.eventWriterFactory.Trace(nameof(AzureKeyVaultReader), cert.ToLogMessage("PDMS"));
                            certs.Add(cert);
                        }
                    }
                }
                while (!string.IsNullOrWhiteSpace(secretPage.NextPageLink));

                // Return things with the latest expiration at the front of the list.
                return certs.OrderByDescending(x => x.NotAfter).ToList();
            }
            catch (Exception ex)
            {
                this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, name, $"Failed to retrieve certs with versions from key vault. Exception is {ex.ToString()}", OperationResult.Failure);
                throw;
            }
        }

        /// <summary>
        ///     Get all of the certificates in the Key Vault, which includes all active/enabled versions that are not expired.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of certificates.</returns>
        public async Task<IList<X509Certificate2>> GetCertificatesAsync(CancellationToken cancellationToken)
        {
            var certItems = new List<CertificateItem>();
            var certificateResponse = new List<X509Certificate2>();
            IPage<CertificateItem> page = null;

            try
            {
                do
                {
                    if (string.IsNullOrWhiteSpace(page?.NextPageLink))
                    {
                        page = await this.keyVaultClient.GetCertificatesAsync(this.keyVaultBaseUrl, cancellationToken: cancellationToken).ConfigureAwait(false);
                        this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, string.Empty, "Successfully retrieved certs of current page from key vault.", OperationResult.Success);

                    }
                    else
                    {
                        page = await this.keyVaultClient.GetCertificatesNextAsync(page.NextPageLink, cancellationToken).ConfigureAwait(false);
                        this.LogAccessToKeyVaultAuditData(page.NextPageLink, string.Empty, "Successfully retrieved certs of next page from key vault.", OperationResult.Success);
                    }

                    certItems.AddRange(page);
                } while (!string.IsNullOrWhiteSpace(page.NextPageLink));
            }
            catch (Exception ex)
            {
                this.eventWriterFactory.Trace(nameof(AzureKeyVaultReader), $"Failed to retrieve certs from key vault. Exception is {ex.ToString()}");
                this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, string.Empty, $"Failed to retrieve certs from key vault. Exception is {ex.ToString()}", OperationResult.Failure);

                throw;
            }

            foreach (CertificateItem certItem in certItems)
            {
                IList<X509Certificate2> certVersions = await this.GetCertificateVersionsAsync(certItem.Identifier.Name, cancellationToken: cancellationToken).ConfigureAwait(false);

                foreach (X509Certificate2 cert in certVersions)
                {
                    this.eventWriterFactory.Trace(nameof(AzureKeyVaultReader), cert.ToLogMessage("PDMS"));
                    certificateResponse.Add(cert);
                }
            }

            return certificateResponse;
        }

        /// <summary>
        /// Get the secret by name.
        /// </summary>
        /// <typeparam name="T">The specific type got from key vault.</typeparam>
        /// <param name="keyVaultRetrieveFunc">The specific key vault function.</param>
        /// <param name="entityName">The entity name.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <returns>The corresponding secret from KeyVault.</returns>
        public async Task<T> GetKeyVaultEntityByNameAsync<T>(Func<string, Task<T>> keyVaultRetrieveFunc, string entityName, string entityType)
        {
            int nextBackoffMs = 1000;
            int retries = 0;
            while (true)
            {
                try
                {
                    retries++;
                    return await keyVaultRetrieveFunc(entityName).ConfigureAwait(false);
                }
                catch (KeyVaultErrorException keyVaultException)
                {
                    if (retries >= 5)
                    {
                        this.eventWriterFactory.Trace(nameof(AzureKeyVaultReader), $"Tried 5 times but still failed to get {entityType} {entityName} from Azure Key Vault");
                        throw;
                    }

                    double multiplier = 1.5 + RandomHelper.NextDouble();
                    nextBackoffMs = (int)(nextBackoffMs * multiplier);
                    if ((int)keyVaultException.Response.StatusCode == 429)
                    {
                        this.eventWriterFactory.Trace(nameof(AzureKeyVaultReader), $"Throttling error during getting Azure Key Vault {entityType}s; sleeping for {nextBackoffMs}ms");
                        await Task.Delay(nextBackoffMs).ConfigureAwait(false);
                    }
                    else
                    {
                        this.eventWriterFactory.Trace(nameof(AzureKeyVaultReader), $"Failed to get {entityType} {entityName} from Azure Key Vault");
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Get certificate with longest expiration date.
        /// </summary>
        /// <param name="name">The certificate name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The certificate with longest expiration date.</returns>
        public async Task<X509Certificate2> GetCertificateWithLongestExpirationDateAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            IList<X509Certificate2> certs = await this.GetCertificateVersionsAsync(name, cancellationToken).ConfigureAwait(false);

            if (certs?.Count == 0)
            {
                throw new InvalidOperationException($"No certs found in KV with secret name: {name}.");
            }

            X509Certificate2 latest = null;

            foreach (X509Certificate2 cert in certs)
            {
                if (latest == null)
                {
                    latest = cert;
                }

                if (cert.NotAfter > latest.NotAfter)
                {
                    latest = cert;
                }
            }

            DualLogger.Instance.Information(nameof(AzureKeyVaultReader), latest.ToLogMessage("PDMS"));
            return latest;
        }

        private void LogAccessToKeyVaultAuditData(string keyVaultBaseUrl, string name, string resultDescription, OperationResult operationResult)
        {
            var callerIdentities = new List<CallerIdentity>();
            callerIdentities.Add(new CallerIdentity(CallerIdentityType.SubscriptionID, this.subscriptionId));

            var auditData = AuditData.BuildAccessToKeyVaultOperationAuditData(keyVaultBaseUrl, name, resultDescription, CallerName, callerIdentities, operationResult);
            this.auditLogger.Log(auditData);
        }
    }
}