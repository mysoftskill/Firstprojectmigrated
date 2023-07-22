using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Cloud.InstrumentationFramework;
using Microsoft.PrivacyServices.Common.Azure;
using Microsoft.Rest.Azure;

namespace CertInstaller
{
    /// <summary>
    /// Provides access to Azure Key Vault.
    /// </summary>
    public class AzureKeyVaultClient : IAzureKeyVaultClient
    {
        private readonly string keyVaultBaseUrl;
        private readonly KeyVaultClient keyVaultClient;
        private readonly IAzureKeyVaultAccessToken azureKeyVaultAccessToken;
        private readonly IAuditLogger auditLogger;
        private readonly ILogger traceLogger;

        private const string ComponentName = "PCD_CertInstaller_KeyVaultClient";
        private const string CallerName = "ADGCS PCD " + nameof(AzureKeyVaultClient);

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureKeyVaultClient"/> class.
        /// </summary>
        /// <param name="baseUrl">The Azure key vault base URL.</param>
        /// <param name="accessToken">The ManagedIdentity access Token instance</param>
        public AzureKeyVaultClient(string baseUrl, IAzureKeyVaultAccessToken accessToken, ILogger logger)
        {
            keyVaultBaseUrl = baseUrl;
            azureKeyVaultAccessToken = accessToken;
            keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureKeyVaultAccessToken.GetAccessTokenAsync));
            traceLogger = logger;
            auditLogger = AuditLoggerFactory.CreateAuditLogger(traceLogger);
        }
        /// <summary>
        /// Get the certificate by name.
        /// </summary>
        /// <param name="certificateName">The certificate name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The corresponding certificate from KeyVault.</returns>
        public async Task<X509Certificate2> GetCertificateByNameAsync(string certificateName, CancellationToken cancellationToken = default(CancellationToken))
        {
            Func<string, Task<X509Certificate2>> certificateRetrieveFunc =
                async (name) =>
                {
                    SecretBundle secretBundle = await keyVaultClient.GetSecretAsync(keyVaultBaseUrl, name, cancellationToken).ConfigureAwait(false);
                    var certificate = new X509Certificate2(
                        Convert.FromBase64String(secretBundle.Value),
                        (SecureString)null,
                        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
                    traceLogger.Information(ComponentName, certificate.ToLogMessage("PCD"));
                    return certificate;
                };

            return await GetKeyVaultEntityByNameAsync(certificateRetrieveFunc, certificateName, "certificate").ConfigureAwait(false);
        }

        /// <summary>
        /// Get the secret by name.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The corresponding secret from KeyVault.</returns>
        public async Task<string> GetSecretByNameAsync(string secretName, CancellationToken cancellationToken = default(CancellationToken))
        {
            Func<string, Task<string>> secretRetrieveFunc =
                async (name)
                =>
                {
                    SecretBundle secretBundle = await keyVaultClient.GetSecretAsync(keyVaultBaseUrl, name, cancellationToken).ConfigureAwait(false);
                    return secretBundle.Value;
                };

            return await GetKeyVaultEntityByNameAsync<string>(secretRetrieveFunc, secretName, "secret").ConfigureAwait(false);
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
            Func<SecretItem, Task<X509Certificate2>> getMachineKeyCertificateFromSecretItemAsync = async (secretItem) =>
            {
                Func<string, Task<X509Certificate2>> certificateRetrieveFunc =
                    async (n) =>
                    {
                        SecretBundle secretBundle = await keyVaultClient.GetSecretAsync(keyVaultBaseUrl, n, secretItem.Identifier.Version, cancellationToken).ConfigureAwait(false);

                        if (secretBundle == null)
                        {
                            traceLogger.Warning(ComponentName, "Secret bundle was null");
                            return null;
                        }

                        return new X509Certificate2(
                            Convert.FromBase64String(secretBundle.Value),
                            (SecureString)null,
                            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
                    };

                return await GetKeyVaultEntityByNameAsync<X509Certificate2>(certificateRetrieveFunc, name, "certificate").ConfigureAwait(false);
            };

            traceLogger.Information(ComponentName, $"Looking for certs for {name}");

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
                        if (string.IsNullOrWhiteSpace(secretPage?.NextPageLink))
                        {
                            return await keyVaultClient.GetSecretVersionsAsync(keyVaultBaseUrl, n, cancellationToken: cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            return await keyVaultClient.GetSecretVersionsNextAsync(secretPage.NextPageLink, cancellationToken: cancellationToken).ConfigureAwait(false);
                        }
                    };

                secretPage = await GetKeyVaultEntityByNameAsync<IPage<SecretItem>>(certificatePageRetrieveFunc, name, "certificate page").ConfigureAwait(false);

                foreach (SecretItem secretItem in secretPage)
                {
                    traceLogger.Information(ComponentName, $"Cert: {secretItem?.Id}, Enabled = {secretItem?.Attributes?.Enabled}, Expires = {secretItem?.Attributes?.Expires}");
                    if (secretItem?.Attributes?.Enabled != true)
                    {
                        traceLogger.Warning(ComponentName, "Not enabled.");
                        continue;
                    }

                    if (secretItem.Attributes.Expires != null && secretItem.Attributes.Expires < DateTimeOffset.UtcNow)
                    {
                        traceLogger.Warning(ComponentName, "Already expired.");
                        continue;
                    }

                    X509Certificate2 cert = await getMachineKeyCertificateFromSecretItemAsync(secretItem).ConfigureAwait(false);
                    if (cert != null)
                    {
                        traceLogger.Information(ComponentName, cert.ToLogMessage("PCD"));
                        certs.Add(cert);
                    }
                }
            }
            while (!string.IsNullOrWhiteSpace(secretPage.NextPageLink));

            // Return things with the latest expiration at the front of the list.
            return certs.OrderByDescending(x => x.NotAfter).ToList();
        }

        /// <summary>
        /// Get the secret by name.
        /// </summary>
        /// <typeparam name="T">The specific type got from key vault.</typeparam>
        /// <param name="keyVaultRetrieveFunc">The specific key vault function.</param>
        /// <param name="entityName">The entity name.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <returns>The corresponding secret from KeyVault.</returns>
        private async Task<T> GetKeyVaultEntityByNameAsync<T>(Func<string, Task<T>> keyVaultRetrieveFunc, string entityName, string entityType)
        {
            int nextBackoffMs = 1000;
            int retries = 0;
            while (true)
            {
                try
                {
                    retries++;
                    var keyVaultResult = await keyVaultRetrieveFunc(entityName).ConfigureAwait(false);

                    LogAccessToKeyVaultAuditData(keyVaultBaseUrl, entityName, $"Succesfully retrieved {entityType} {entityName}", OperationResult.Success);
                    return keyVaultResult;
                }
                catch (KeyVaultErrorException keyVaultException)
                {
                    if (retries >= 5)
                    {
                        traceLogger.Error(ComponentName, $"Tried 5 times but still failed to get {entityType} {entityName} from Azure KeyVault");
                        LogAccessToKeyVaultAuditData(
                          keyVaultBaseUrl,
                          entityName,
                          $"Failed to retrieve {entityType} {entityName} from Azure KeyVault. Exception is {keyVaultException.ToString()}", OperationResult.Failure);
                        throw;
                    }

                    double multiplier = 1.5 + RandomHelper.NextDouble();
                    nextBackoffMs = (int)(nextBackoffMs * multiplier);
                    if ((int)keyVaultException.Response.StatusCode == 429)
                    {
                        traceLogger.Error(ComponentName, $"Throttling error during getting Azure KeyVault {entityType}s; sleeping for {nextBackoffMs}ms");
                        await Task.Delay(nextBackoffMs).ConfigureAwait(false);
                    }
                    else
                    {
                        traceLogger.Error(ComponentName, $"Failed to get {entityType} {entityName} from Azure KeyVault");
                        LogAccessToKeyVaultAuditData(
                          keyVaultBaseUrl,
                          entityName,
                          $"Failed to retrieve {entityType} {entityName} from Azure KeyVault. Exception is {keyVaultException.ToString()}", OperationResult.Failure);

                        throw;
                    }
                }
            }
        }

        // Get AAD auth token for key vault call back
        private async Task<string> GetAccessTokenAsync(
            string authority,
            string resource,
            string scope)
        {           
            var result1 = await azureKeyVaultAccessToken.GetAccessTokenAsync(authority, resource, scope);
            return result1;
        }

        private void LogAccessToKeyVaultAuditData(string keyVaultBaseUrl, string name, string resultDescription, OperationResult operationResult)
        {
            var callerIdentities = new List<CallerIdentity> { new CallerIdentity(CallerIdentityType.ApplicationID, azureKeyVaultAccessToken.AppId) };
            var auditData = AuditData.BuildAccessToKeyVaultOperationAuditData(keyVaultBaseUrl, name, resultDescription, CallerName, callerIdentities, operationResult);
            auditLogger.Log(auditData);
        }
    }
}
