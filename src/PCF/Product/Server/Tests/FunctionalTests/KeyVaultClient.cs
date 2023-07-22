namespace PCF.FunctionalTests
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Models;
    using Microsoft.Identity.Client;

    public abstract class TestSecretClient
    {
        private readonly object syncRoot = new object();

        private readonly string vaultBaseUri;
        private readonly Dictionary<string, Task<X509Certificate2>> certificateTaskCache = new Dictionary<string, Task<X509Certificate2>>();
        private readonly Dictionary<string, Task<string>> secretTaskCache = new Dictionary<string, Task<string>>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#")]
        protected TestSecretClient(string vaultBaseUri)
        {
            this.vaultBaseUri = vaultBaseUri;
        }

        public Task<X509Certificate2> GetPrivateCertificateAsync(string certificateName)
        {
            if (!this.certificateTaskCache.ContainsKey(certificateName))
            {
                lock (this.syncRoot)
                {
                    if (!this.certificateTaskCache.ContainsKey(certificateName))
                    {
                        this.certificateTaskCache[certificateName] = Task.Run(async () =>
                        {
                            string secretText = await this.GetSecretAsync(certificateName);

                            var result = new X509Certificate2(Convert.FromBase64String(secretText), (SecureString)null);
                            return result;
                        });
                    }
                }
            }

            return this.certificateTaskCache[certificateName];
        }

        public Task<string> GetSecretAsync(string key)
        {
            if (!this.secretTaskCache.ContainsKey(key))
            {
                lock (this.syncRoot)
                {
                    if (!this.secretTaskCache.ContainsKey(key))
                    {
                        this.secretTaskCache[key] = Task.Run(async () =>
                        {
                            using (var client = this.CreateKeyVaultClient())
                            {
                                SecretBundle bundle = await client.GetSecretAsync(this.vaultBaseUri, key);
                                string secret = bundle.Value;
                                return secret;
                            }
                        });
                    }
                }
            }

            return this.secretTaskCache[key];
        }

        protected abstract KeyVaultClient CreateKeyVaultClient();
    }

    /// <summary>
    /// Key vault client based on password.
    /// </summary>
    public class ApplicationSecretClient : TestSecretClient
    {
        private readonly string applicationId;
        private readonly string applicationPassword;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#")]
        public ApplicationSecretClient(
            string vaultBaseUri, 
            string applicationId,
            string password) : base(vaultBaseUri)
        {
            this.applicationId = applicationId;
            this.applicationPassword = password;
        }

        protected override KeyVaultClient CreateKeyVaultClient()
        {
            return new KeyVaultClient(this.GetAccessTokenAsync);
        }
        
        private async Task<string> GetAccessTokenAsync(
            string authority,
            string resource,
            string scope)
        {
            var scopes = new[] { $"{resource}/.default" };

            var result = await ConfidentialCredential.GetTokenAsync(this.applicationId, this.applicationPassword, new Uri(authority), scopes);

            return result.AccessToken;
        }
    }

    /// <summary>
    /// Key vault client based on user credentials.
    /// </summary>
    public class CertificateSecretClient : TestSecretClient
    {
        private readonly string applicationId;
        private readonly X509Certificate2 cert;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "2#")]
        public CertificateSecretClient(string applicationId, X509Certificate2 cert, string vaultBaseUri) : base(vaultBaseUri)
        {
            this.applicationId = applicationId;
            this.cert = cert;
        }

        protected override KeyVaultClient CreateKeyVaultClient()
        {
            return new KeyVaultClient(this.GetAccessTokenAsync, new HttpClient()); // lgtm [cs/httpclient-checkcertrevlist-disabled]
        }

        private async Task<string> GetAccessTokenAsync(
            string authority,
            string resource,
            string scope)
        {
            var scopes = new[] { $"{resource}/.default" };

            var result = await ConfidentialCredential.GetTokenAsync(this.applicationId, this.cert, new Uri(authority), scopes);

            return result.AccessToken;
        }
    }
}
