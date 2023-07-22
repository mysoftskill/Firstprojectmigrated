namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.SecretClient;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Models;
    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.SecretClient;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Rest.Azure;

    /// <summary>
    /// The secret client against Azure key vault service
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public class AzureKeyVaultClient : IAzureKeyVaultClient
    {
        private readonly IKeyVaultClient keyVaultClient;
        private readonly string keyVaultBaseUrl;
        private const string CallerName = "ADGCS PCF " + nameof(AzureKeyVaultClient);
        private readonly IAzureKeyVaultAccessToken azureKeyVaultAccessToken;
        private readonly IAuditLogger auditLogger;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#")]
        public AzureKeyVaultClient(string keyVaultBaseUrl, IAzureKeyVaultAccessToken azureKeyVaultAccessToken)
        {
            this.keyVaultBaseUrl = keyVaultBaseUrl;
            this.keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureKeyVaultAccessToken.GetAccessTokenAsync));
            this.auditLogger = AuditLoggerFactory.CreateAuditLogger(DualLogger.Instance);
            this.azureKeyVaultAccessToken = azureKeyVaultAccessToken;
        }

        /// <inheritdoc/>
        public async Task<string> GetSecretAsync(string name)
        {
            DualLogger.Instance.Information(nameof(AzureKeyVaultClient), $"KeyVaultUrl: {this.keyVaultBaseUrl}.");
            DualLogger.Instance.Information(nameof(AzureKeyVaultClient), $"SecretName: {name}.");
            string result = null;

            try
            {
                result = await WithBackoffAsync(async () =>
                {
                    var secret = await this.keyVaultClient.GetSecretAsync(this.keyVaultBaseUrl, name);
                    this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, name, "Successfully retrieved secret.", OperationResult.Success);
                    return secret.Value;
                });
            }
            catch (Exception ex)
            {
                this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, name, $"Failed to retrieve secret by name from key vault. Exception is {ex.ToString()}", OperationResult.Failure);
                throw;
            }

            return result;
        }

        /// <summary>
        /// Fetches the latest (current) version of the named certificate.
        /// </summary>
        public async Task<X509Certificate2> GetCurrentCertificateAsync(string name)
        {
            X509Certificate2 cert = null;

            try
            {
                var secretBundle = await this.keyVaultClient.GetSecretAsync(this.keyVaultBaseUrl, name);
                this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, name, "Successfully retrieved cert from key vault.", OperationResult.Success);

                if (secretBundle == null)
                {
                    DualLogger.Instance.Information(nameof(AzureKeyVaultClient), $"Unable to find named certificate '{name}'");
                    return null;
                }

                cert = new X509Certificate2(
                    Convert.FromBase64String(secretBundle.Value),
                    (SecureString)null,
                    X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);
            }
            catch (Exception ex)
            {
                this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, name, $"Failed to retrieve cert from key vault. Exception is {ex.ToString()}", OperationResult.Failure);
                throw;
            }

            return cert;
        }

        /// <summary>
        /// Gets a certificate from Key Vault for all enabled versions, not expired versions, with private keys, by name.
        /// </summary>
        /// <param name="name">The name of the certificate in Key Vault.</param>
        /// <returns>The certificates.</returns>
        /// <remarks>To whoever previously thanked me in code history - I urge you to consider sharing code via libraries instead of copying...</remarks>
        public async Task<IList<X509Certificate2>> GetCertificateVersionsAsync(string name)
        {
            List<X509Certificate2> certs = new List<X509Certificate2>();

            try
            {
                async Task<X509Certificate2> GetMachineKeyCertificateFromSecretItemAsync(SecretItem secretItem)
                {
                    SecretBundle secretBundle = await this.keyVaultClient.GetSecretAsync(this.keyVaultBaseUrl, name, secretItem.Identifier.Version);
                    this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, name, $"Retrieved secret with version {secretItem.Identifier.Version} from key vault", OperationResult.Success);

                    if (secretBundle == null)
                    {
                        DualLogger.Instance.Information(nameof(AzureKeyVaultClient), "Secret bundle was null");
                        return null;
                    }

                    return new X509Certificate2(
                        Convert.FromBase64String(secretBundle.Value),
                        (SecureString)null,
                        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
                }

                DualLogger.Instance.Information(nameof(AzureKeyVaultClient), $"Looking for certs for {name}");

                certs = await WithBackoffAsync(async () =>
                {

                    // In order to get the cert with private key, it must be read from GetSecretAsync
                    // The method GetCertificateAsync method only returns the public cer (by Key Vault design)
                    IPage<SecretItem> secretPage = null;
                    do
                    {
                        if (string.IsNullOrWhiteSpace(secretPage?.NextPageLink))
                        {
                            secretPage = await this.keyVaultClient.GetSecretVersionsAsync(this.keyVaultBaseUrl, name);
                            this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, name, "Successfully retrieved secret versions of current page from key vault.", OperationResult.Success);
                        }
                        else
                        {
                            secretPage = await this.keyVaultClient.GetSecretVersionsNextAsync(secretPage.NextPageLink);
                            this.LogAccessToKeyVaultAuditData(secretPage.NextPageLink, name, "Successfully retrieved the secret versions of next page from key vault.", OperationResult.Success);
                        }

                        foreach (SecretItem secretItem in secretPage)
                        {
                            DualLogger.Instance.Information(nameof(AzureKeyVaultClient), $"Cert: {secretItem?.Id}, Enabled = {secretItem?.Attributes?.Enabled}, Expires = {secretItem?.Attributes?.Expires}");
                            if (secretItem?.Attributes?.Enabled != true)
                            {
                                DualLogger.Instance.Information(nameof(AzureKeyVaultClient), "Not enabled.");
                                continue;
                            }

                            if (secretItem.Attributes.Expires != null && secretItem.Attributes.Expires < DateTimeOffset.UtcNow)
                            {
                                DualLogger.Instance.Information(nameof(AzureKeyVaultClient), "Already expired.");
                                continue;
                            }

                            X509Certificate2 cert = await GetMachineKeyCertificateFromSecretItemAsync(secretItem);
                            if (cert != null)
                            {
                                certs.Add(cert);
                            }
                        }
                    }
                    while (!string.IsNullOrWhiteSpace(secretPage.NextPageLink));

                    // Return things with the latest expiration at the front of the list.
                    return certs.OrderByDescending(x => x.NotAfter).ToList();
                });
            }
            catch (Exception ex)
            {
                this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, name, $"Failed to retrieve certs with versions from key vault. Exception is {ex.ToString()}", OperationResult.Failure);
                throw;
            }

            return certs;
        }

        private static async Task<T> WithBackoffAsync<T>(Func<Task<T>> callback)
        {
            int nextBackoffMs = 1000;
            int retries = 0;
            while (true)
            {
                try
                {
                    retries++;
                    return await callback();
                }
                catch (KeyVaultErrorException keyVaultException)
                {
                    if (retries >= 5)
                    {
                        DualLogger.Instance.Error(nameof(AzureKeyVaultClient), keyVaultException, $"Tried 5 times but still failed to get secret from Azure Key Vault");
                        throw;
                    }

                    double multiplier = 1.5 + RandomHelper.NextDouble();
                    nextBackoffMs = (int)(nextBackoffMs * multiplier);
                    if ((int)keyVaultException.Response.StatusCode == 429)
                    {
                        DualLogger.Instance.Information(nameof(AzureKeyVaultClient), $"Throttling error during getting Azure Key Vault secrets; sleeping for {nextBackoffMs}ms");
                        await Task.Delay(nextBackoffMs);
                    }
                    else
                    {
                        DualLogger.Instance.Error(nameof(AzureKeyVaultClient), keyVaultException, $"Failed to get secret from Azure Key Vault");
                        throw;
                    }
                }
            }
        }

        private void LogAccessToKeyVaultAuditData(string keyVaultBaseUrl, string name, string resultDescription, OperationResult operationResult)
        {
            var callerIdentities = new List<CallerIdentity>();

            // BUG: PCF Configuration doesn't read from this program and blocks the thread, preventing setup from succeeding. Temporarily remove this code to work around it.
            ////var subscriptionId = Config.Instance?.AzureManagement?.UAMISubscriptionId;

            ////if (!string.IsNullOrWhiteSpace(subscriptionId))
            ////{
            ////    callerIdentities.Add(new CallerIdentity(CallerIdentityType.SubscriptionID, subscriptionId));
            ////}

            if (!string.IsNullOrWhiteSpace(this.azureKeyVaultAccessToken?.AppId))
            {
                callerIdentities.Add(new CallerIdentity(CallerIdentityType.ApplicationID, this.azureKeyVaultAccessToken?.AppId));
            }
            else if (!string.IsNullOrWhiteSpace(this.azureKeyVaultAccessToken?.CertificateThumbprint))
            {
                callerIdentities.Add(new CallerIdentity(CallerIdentityType.Certificate, this.azureKeyVaultAccessToken?.CertificateThumbprint));
            }

            // Need at least one identity. This one isn't really useful, but it meets the requirement
            if (callerIdentities.Count == 0)
            {
                callerIdentities.Add(new CallerIdentity(CallerIdentityType.KeyName, name));
            }

            var auditData = AuditData.BuildAccessToKeyVaultOperationAuditData(keyVaultBaseUrl, name, resultDescription, CallerName, callerIdentities, operationResult);
            this.auditLogger.Log(auditData);
        }
    }
}
