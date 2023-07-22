namespace Microsoft.PrivacyServices.AzureFunctions.Common.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Manages the caching of certificates from the environment variable and provides methods to access stored certificates.
    /// </summary>
    public class CertificateManager
    {
        private static readonly Lazy<CertificateManager> Lazy =
            new Lazy<CertificateManager>(() => new CertificateManager());

        private readonly Dictionary<string, X509Certificate2> certificateCache = new Dictionary<string, X509Certificate2>();

        private CertificateManager()
        {
        }

        /// <summary>
        /// Returns the instance of CertificateManager.
        /// </summary>
        public static CertificateManager Instance
        {
            get { return Lazy.Value; }
        }

        /// <summary>
        /// Returns the certificate for the specified key, either from the cache or by loading it from environment variable.
        /// Adds the certificate to the cache if it's not already cached.
        /// </summary>
        /// <param name="key">The key for the certificate in the environment variable.</param>
        /// <returns>The certificate for the specified key.</returns>
        public X509Certificate2 GetCertificate(string key)
        {
            if (this.certificateCache.ContainsKey(key))
            {
                return this.certificateCache[key];
            }
            else
            {
                string secretValue = GetStringValue(key);
                byte[] certificateBytes = Convert.FromBase64String(secretValue);
                X509Certificate2 certificate = new X509Certificate2(certificateBytes, string.Empty, X509KeyStorageFlags.EphemeralKeySet);
                this.certificateCache[key] = certificate;
                return certificate;
            }
        }

        private static string GetStringValue(string key)
        {
            return Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process);
        }
    }
}
