// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.SecretStore
{
    using System;
    using System.Collections.Generic;
    using System.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using global::Azure.Core;
    using global::Azure.Identity;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Models;
    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.Membership.MemberServices.Common.Azure;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Rest.Azure;

    /// <summary>
    ///     AzureKeyVaultReader
    /// </summary>
    public class AzureKeyVaultReader : ISecretStoreReader
    {
        private readonly IClock clock;

        private readonly KeyVaultClientInstrumented keyVault;

        private readonly string keyVaultBaseUrl;

        private const string  CallerName = "ADGCS PXS " + nameof(AzureKeyVaultReader);

        private readonly IAuditLogger auditLogger;

        private readonly ILogger traceLogger;

        private readonly string appId;

        private readonly TokenCredential credential;

        /// <summary>
        ///     Creates a new instance of Azure KeyVault Reader
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="clock">The clock</param>
        /// <param name="logger">The trace logger</param>
        public AzureKeyVaultReader(IPrivacyConfigurationManager configurationManager, IClock clock, ILogger logger)
        {
            if (configurationManager == null)
            {
                throw new ArgumentNullException(nameof(configurationManager));
            }

            IAzureKeyVaultConfiguration keyVaultConfig = configurationManager.AzureKeyVaultConfiguration ??
                                                         throw new ArgumentNullException(nameof(configurationManager.AzureKeyVaultConfiguration));
            this.appId = keyVaultConfig.AadAppId ?? CallerName;

            if (configurationManager.EnvironmentConfiguration.EnvironmentType == EnvironmentType.OneBox)
            {
                // In a dev box, use AAD app (PXS-INT) to auth with AKV instead.
                const string certSubjectName = "aad-ame2.ppe.dpp.microsoft.com";
                var cert = CertificateFinder.FindCertificateByName(certSubjectName);
                if (cert == null)
                {
                    throw new InvalidOperationException($"Certificate {certSubjectName} is missing from local store.");
                }

                this.credential = new ConfidentialCredential(
                    clientId: "705363a0-5817-47fb-ba32-59f47ce80bb7",
                    certificate: cert,
                    authority: new Uri("https://login.windows.net/33e01921-4d64-4f8c-a055-5bdaffd5e33d"),
                    logger);
            }
            else
            {
                // In cloud env, MSI will be used
                this.credential = new DefaultAzureCredential();
            }

            async Task<string> authCallback(string authority, string resource, string scope)
            {
                var result = await this.credential.GetTokenAsync(new TokenRequestContext(new[] { keyVaultConfig.VaultResourceId }), CancellationToken.None);
                return result.Token;
            }

            IKeyVaultClient client = new KeyVaultClient(authCallback);

            this.keyVault = new KeyVaultClientInstrumented(client);
            this.keyVaultBaseUrl = keyVaultConfig.VaultBaseUrl;
            this.clock = clock;
            this.traceLogger = logger;
            this.auditLogger = AuditLoggerFactory.CreateAuditLogger(this.traceLogger);
        }

        public async Task<X509Certificate2> GetCertificateCurrentVersionAsync(
            string name,
            bool includePrivateKey = false,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            X509Certificate2 cert = null;

            try
            {
                if (includePrivateKey)
                {
                    SecretBundle secretBundle = await this.keyVault.GetSecretAsync(this.keyVaultBaseUrl, name, cancellationToken)
                        .ConfigureAwait(false);
                    this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, name, "Successfully retrieved secret from key vault.", OperationResult.Success);

                    return new X509Certificate2(
                        Convert.FromBase64String(secretBundle.Value),
                        (SecureString)null,
                        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
                }

                CertificateBundle certificateBundle = await this.keyVault.GetCertificateAsync(this.keyVaultBaseUrl, name, cancellationToken).ConfigureAwait(false);
                this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, name, "Successfully retrieved cert from key vault.", OperationResult.Success);
                cert = new X509Certificate2(certificateBundle.Cer);
            }
            catch (Exception ex)
            {
                this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, name, $"Failed to retrieve secret or cert from key vault. Exception is {ex.ToString()}", OperationResult.Failure);
                throw;
            }

            return cert;
        }

        /// <summary>
        ///     Get all of the certificates in the Key Vault, which includes all active/enabled versions that are not expired.
        /// </summary>
        /// <param name="exclusionNameList">List of cert identifier names to exclude from results</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of certificates.</returns>
        public async Task<IList<X509Certificate2>> GetCertificatesAsync(IList<string> exclusionNameList = null, CancellationToken cancellationToken = default(CancellationToken))
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
                        page = await this.keyVault.GetCertificatesAsync(this.keyVaultBaseUrl, cancellationToken: cancellationToken).ConfigureAwait(false);
                        this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, string.Empty, "Successfully retrieved certs of current page from key vault.", OperationResult.Success);
                    }
                    else
                    {
                        page = await this.keyVault.GetCertificatesNextAsync(page.NextPageLink, cancellationToken).ConfigureAwait(false);
                        this.LogAccessToKeyVaultAuditData(page.NextPageLink, string.Empty, "Successfully retrieved certs of next page from key vault.", OperationResult.Success);
                    }

                    certItems.AddRange(page);
                } while (!string.IsNullOrWhiteSpace(page.NextPageLink));
            }
            catch (Exception ex)
            {
                this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, string.Empty, $"Failed to retrieve certs from key vault. Exception is {ex.ToString()}", OperationResult.Failure);
                throw;
            }

            foreach (CertificateItem certItem in certItems)
            {
                if (exclusionNameList != null && exclusionNameList.Contains(certItem.Identifier.Name))
                {
                    this.traceLogger.Warning(nameof(AzureKeyVaultReader), $"Excluding certificate from results. ID: {certItem.Id}");
                    continue;
                }

                if (certItem?.Attributes?.Enabled == null || certItem.Attributes.Enabled.Value != true)
                {
                    this.traceLogger.Information(nameof(AzureKeyVaultReader), $"Ignoring disabled cert. Identifier name: {certItem.Identifier?.Name}, Identifier Version: {certItem.Identifier?.Version}");
                    continue;
                }

                X509Certificate2 cert = await this.GetCertificateCurrentVersionAsync(certItem.Identifier.Name, includePrivateKey: true, cancellationToken: cancellationToken).ConfigureAwait(false);

                this.traceLogger.Information(
                        nameof(AzureKeyVaultReader),
                        "Successfully retrieved certificate from Key Vault. " +
                        $"{cert.Thumbprint}, " +
                        $"{cert.SubjectName.Name}, " +
                        $"{cert.IssuerName.Name}, " +
                        $"HasPrivateKey: {cert.HasPrivateKey}");
                certificateResponse.Add(cert);
            }

            return certificateResponse;
        }

        /// <summary>
        ///     Gets a certificate from Key Vault for all enabled versions, not expired versions, with private keys, by name.
        /// </summary>
        /// <param name="name">The name of the certificate in Key Vault.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The certificate.</returns>
        public async Task<IList<X509Certificate2>> GetCertificateVersionsAsync(
            string name,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            IList<X509Certificate2> certs = new List<X509Certificate2>();

            try
            {
                async Task<X509Certificate2> GetMachineKeyCertificateFromSecretItemAsync(SecretItem secretItem)
                {
                    SecretBundle secretBundle = await this.keyVault.GetSecretAsync(this.keyVaultBaseUrl, name, secretItem.Identifier.Version, cancellationToken)
                        .ConfigureAwait(false);
                    this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, name, $"Retrieved secret with version {secretItem.Identifier.Version} from key vault", OperationResult.Success);

                    if (secretBundle == null)
                    {
                        this.traceLogger.Warning(nameof(AzureKeyVaultReader), "Ignoring null secret.");
                        return null;
                    }

                    return new X509Certificate2(
                        Convert.FromBase64String(secretBundle.Value),
                        (SecureString)null,
                        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
                }

                // In order to get the cert with private key, it must be read from GetSecretAsync
                // The method GetCertificateAsync method only returns the public cer (by Key Vault design)
                IPage<SecretItem> secretPage = null;
                do
                {
                    if (string.IsNullOrWhiteSpace(secretPage?.NextPageLink))
                    {
                        secretPage = await this.keyVault.GetSecretVersionsAsync(this.keyVaultBaseUrl, name, cancellationToken: cancellationToken).ConfigureAwait(false);
                        this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, name, "Successfully retrieved secret versions of current page from key vault.", OperationResult.Success);
                    }
                    else
                    {
                        secretPage = await this.keyVault.GetSecretVersionsNextAsync(secretPage.NextPageLink, cancellationToken: cancellationToken).ConfigureAwait(false);
                        this.LogAccessToKeyVaultAuditData(secretPage.NextPageLink, name, "Successfully retrieved the secret versions of next page from key vault.", OperationResult.Success);
                    }

                    foreach (SecretItem secretItem in secretPage)
                    {
                        if (secretItem == null)
                        {
                            this.traceLogger.Warning(nameof(AzureKeyVaultReader), "Ignoring null secret.");
                            continue;
                        }

                        if (secretItem.Attributes == null)
                        {
                            this.traceLogger.Warning(nameof(AzureKeyVaultReader), "Ignoring secret with null attributes.");
                            continue;
                        }

                        if (secretItem.Attributes.Enabled == null || secretItem.Attributes.Enabled.Value != true)
                        {
                            this.traceLogger.Information(nameof(AzureKeyVaultReader), $"Ignoring disabled secret. Name: {name}, Identifier Version: {secretItem.Identifier?.Version}");
                            continue;
                        }

                        if (secretItem.Attributes.Expires != null)
                        {
                            if (secretItem.Attributes.Expires > this.clock.UtcNow)
                            {
                                X509Certificate2 cert = await GetMachineKeyCertificateFromSecretItemAsync(secretItem).ConfigureAwait(false);
                                if (cert != null)
                                {
                                    certs.Add(cert);
                                }
                            }
                            else
                            {
                                this.traceLogger.Information(
                                    nameof(AzureKeyVaultReader),
                                    $"Ignoring expired secret. Name: {name}, Identifier Version: {secretItem.Identifier?.Version}, Expiration: {secretItem.Attributes.Expires}");
                            }
                        }
                        else
                        {
                            X509Certificate2 cert = await GetMachineKeyCertificateFromSecretItemAsync(secretItem).ConfigureAwait(false);
                            if (cert != null)
                            {
                                certs.Add(cert);
                            }
                        }
                    }
                } while (!string.IsNullOrWhiteSpace(secretPage.NextPageLink));
            }
            catch (Exception ex)
            {
                this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, name, $"Failed to retrieve certs with versions from key vault. Exception is {ex.ToString()}", OperationResult.Failure);
                throw;
            }

            return certs;
        }

        /// <inheritdoc />
        public async Task<string> ReadSecretByNameAsync(string secretName)
        {
            SecretBundle result = null;

            try
            {
                result = await this.keyVault.GetSecretAsync(this.keyVaultBaseUrl, secretName, CancellationToken.None).ConfigureAwait(false);
                this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, secretName, "Successfully retrieved secret.", OperationResult.Success);
            }
            catch (Exception ex)
            {
                this.LogAccessToKeyVaultAuditData(this.keyVaultBaseUrl, secretName, $"Failed to retrieve secret by name from key vault. Exception is {ex.ToString()}", OperationResult.Failure);
                throw;
            }

            return result?.Value;
        }
        
        private void LogAccessToKeyVaultAuditData(string keyVaultBaseUrl, string name, string resultDescription, OperationResult operationResult)
        {
            var callerIdentities = new List<CallerIdentity> { new CallerIdentity(CallerIdentityType.ApplicationID, this.appId) };
            var auditData = AuditData.BuildAccessToKeyVaultOperationAuditData(keyVaultBaseUrl, name, resultDescription, CallerName, callerIdentities, operationResult);
            this.auditLogger.Log(auditData);
        }
    }
}
