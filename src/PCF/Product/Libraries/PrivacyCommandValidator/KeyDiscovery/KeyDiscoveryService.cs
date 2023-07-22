namespace Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery.Keys;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;

    using Newtonsoft.Json;

    /// <inheritdoc />
    /// <summary>
    /// Implements the <see cref="T:Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery.IKeyDiscoveryService" />.
    /// </summary>
    public sealed class KeyDiscoveryService : IKeyDiscoveryService
    {
        private static readonly IHttpClient HttpClient = new DefaultHttpClient();

        private static readonly TimeSpan CachingInterval = TimeSpan.FromDays(1); // 1 day
        private static readonly TimeSpan PollingInterval = TimeSpan.FromHours(1); // 1 hour
        private static readonly TimeSpan NegativeResponseBackoff = TimeSpan.FromHours(1); // 1 hour

        private readonly Uri endpointUrl;
        private readonly Issuer issuer;
        private readonly bool isCertificateChainValidationEnabled;

        private readonly ICache cache;

        /// <inheritdoc />
        public KeyDiscoveryService(EnvironmentConfiguration configuration, ICache customCache)
        {
            this.cache = customCache ?? throw new ArgumentNullException(nameof(customCache));

            this.endpointUrl = configuration.KeyDiscoveryEndPoint;
            this.isCertificateChainValidationEnabled = configuration.IsCertificateChainValidationEnabled;
            this.issuer = configuration.Issuer;

            // Start background polling worker
            Task.Run(this.BackgroundPollForNewKeysAsync);
        }

        /// <inheritdoc />
        public async Task<X509Certificate2> GetCertificate(string keyId, LoggableInformation loggableInformation, CancellationToken cancellationToken)
        {
            JsonWebKey jwk = await this.GetKeyFromCacheAsync(keyId, loggableInformation, cancellationToken).ConfigureAwait(false);

            // Build x509cert chain. X5c param is guaranteed to be not null.
            if (!jwk.X509Chain.Any())
            {
                throw new KeyDiscoveryException($"x509chain parameter not present in key acquired from {this.issuer}.", loggableInformation);
            }

            // Convert all Base64 encoded DER certificates.
            IEnumerable<X509Certificate2> certificates = jwk.X509Chain.Select(c => new X509Certificate2(Convert.FromBase64String(c))).ToList();

            X509Certificate2 primaryCertificate = certificates.First();
            IEnumerable<X509Certificate2> chainCertificates = certificates.Skip(1);

            if (this.isCertificateChainValidationEnabled)
            {
                EnsureCertificateChainIsValid(primaryCertificate, chainCertificates, loggableInformation);
            }

            EnsureKeyIsValid(jwk, primaryCertificate, loggableInformation);

            return primaryCertificate;
        }

        /// <summary>
        /// Polls the JWK endpoint looking for new keys. This runs asynchronously in the background.
        /// </summary>
        private async Task BackgroundPollForNewKeysAsync()
        {
            await Task.Yield();

            await Task.Delay(PollingInterval).ConfigureAwait(false);

            try
            {
                IDictionary<string, CacheItem> cacheItems = await this.GetDocumentFromEndpointAsync(CancellationToken.None).ConfigureAwait(false);
                await this.cache.WriteAsync(cacheItems, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Service may be down, try again some other time.
            }
        }

        private async Task<CacheItem> GetCacheItemFromEndpointAsync(string keyId, LoggableInformation loggableInformation, CancellationToken cancellationToken)
        {
            // Check if previously locking thread has already written to the cache and avoid calling the endpoint again.
            CacheItem cacheItem = await this.cache.ReadAsync(keyId).ConfigureAwait(false);

            if (cacheItem != null)
            {
                return cacheItem;
            }

            // Fetch data from endpoint
            IDictionary<string, CacheItem> newCacheItems = await this.GetDocumentFromEndpointAsync(cancellationToken).ConfigureAwait(false);

            if (!newCacheItems.Any())
            {
                // Failed to retrieve items from jwk endpoint and the cache expired.
                throw new KeyDiscoveryException($"The JWK endpoint '{this.endpointUrl}' is down.", loggableInformation);
            }

            // Cache results for a day -  Enough time for the JWK service to recover.
            if (!newCacheItems.TryGetValue(keyId, out cacheItem))
            {
                // Cache negative response for 1hr to avoid DDoS attacks on the discovery endpoint.
                cacheItem = new CacheItem(null, false, DateTimeOffset.UtcNow + NegativeResponseBackoff);
                newCacheItems.Add(keyId, cacheItem);
            }

            await this.cache.WriteAsync(newCacheItems, cancellationToken).ConfigureAwait(false);

            return cacheItem;
        }

        private async Task<IDictionary<string, CacheItem>> GetDocumentFromEndpointAsync(CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, this.endpointUrl);
            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                JwkDocument document;
                using (var reader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(false)))
                using (var jsonTextReader = new JsonTextReader(reader))
                {
                    document = new JsonSerializer().Deserialize<JwkDocument>(jsonTextReader);
                }

                return document.Keys.Select(k => new CacheItem(k, true, DateTimeOffset.UtcNow + CachingInterval)).ToDictionary(ci => ci.Item.KeyId);
            }

            throw new KeyDiscoveryException($"The JDK discovery endpoint {this.endpointUrl} (issuer: {this.issuer}) answered this request with code: {response.StatusCode}");
        }

        private async Task<JsonWebKey> GetKeyFromCacheAsync(string keyId, LoggableInformation loggableInformation, CancellationToken cancellationToken)
        {
            CacheItem cacheItem = await this.cache.ReadAsync(keyId).ConfigureAwait(false);

            if (cacheItem == null)
            {
                // Key is not cached or we have a fresh start.
                cacheItem = await this.GetCacheItemFromEndpointAsync(keyId, loggableInformation, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // In cache
                if (cacheItem.Expiration < DateTime.UtcNow)
                {
                    // Stale data. Refresh cache.
                    cacheItem = await this.GetCacheItemFromEndpointAsync(keyId, loggableInformation, cancellationToken).ConfigureAwait(false);
                }
            }

            // Ensure that cached item is valid.
            if (cacheItem?.Item == null || !cacheItem.Found)
            {
                throw new InvalidPrivacyCommandException($"Key Id '{keyId}' not found in endpoint {this.endpointUrl}, issued by {this.issuer}.", loggableInformation);
            }

            return cacheItem.Item;
        }

        /// <summary>
        /// TODO: Validate this chain checks with a proper certificate chain before enabling this method.
        /// </summary>
        /// <param name="primaryCertificate">The primary certificate at the bottom of the trust chain</param>
        /// <param name="additionalCertificates">The additional certificates in the chain leading up to the root.</param>
        /// <param name="loggableInformation">Information about command and verifier that is included in the exception to help logging</param>
        private static void EnsureCertificateChainIsValid(X509Certificate2 primaryCertificate, IEnumerable<X509Certificate2> additionalCertificates, LoggableInformation loggableInformation)
        {
            var chain = new X509Chain();
            try
            {
                foreach (X509Certificate2 cert in additionalCertificates)
                {
                    chain.ChainPolicy.ExtraStore.Add(cert);
                }

                // You can alter how the chain is built/validated.
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Offline;
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

                // Do the preliminary validation.
                if (!chain.Build(primaryCertificate))
                {
                    throw new KeyDiscoveryException("Failed to build certificate chain.", loggableInformation);
                }

                // Make sure we have the same number of elements.
                if (chain.ChainElements.Count != chain.ChainPolicy.ExtraStore.Count + 1)
                {
                    throw new KeyDiscoveryException("There was a mismatch in certificate chain elements.", loggableInformation);
                }

                // Make sure all the thumbprints of the CAs match up.
                // The first one should be 'primaryCert', leading up to the root CA.
                for (int i = 1; i < chain.ChainElements.Count; i++)
                {
                    if (chain.ChainElements[i].Certificate.Thumbprint != chain.ChainPolicy.ExtraStore[i - 1].Thumbprint)
                    {
                        throw new KeyDiscoveryException("Certificate chain thumbprint mismatch.", loggableInformation);
                    }
                }
            }
            finally
            {
                var disposableChain = chain as IDisposable;
                disposableChain?.Dispose();
            }
        }

        private static void EnsureKeyIsValid(JsonWebKey key, X509Certificate2 primaryCertificate, LoggableInformation loggableInformation)
        {
            // Thumbprints match
            string decodedThumbprint = BitConverter.ToString(Base64Url.Decode(key.X509Thumbprint, loggableInformation)).Replace("-", string.Empty);
            if (decodedThumbprint != primaryCertificate.Thumbprint)
            {
                throw new KeyDiscoveryException(
                    $"Invalid Json web key id: '{key.KeyId}' the thumbprints do not match. Requested thumbprint: '{decodedThumbprint}' but the certificate is {primaryCertificate.Thumbprint}", loggableInformation);
            }

            if (key is RsaJsonWebKey rsaKey)
            {
                EnsureRsaKeyIsValid(rsaKey, primaryCertificate, loggableInformation);
            }
            else if (key is DsaJsonWebKey dsaKey)
            {
                EnsureDsaKeyIsValid(dsaKey, primaryCertificate, loggableInformation);
            }
            else
            {
                throw new KeyDiscoveryException($"Invalid Json web key type: '{key.GetType().Name}'", loggableInformation);
            }
        }

        private static void EnsureDsaKeyIsValid(DsaJsonWebKey dsaKey, X509Certificate2 primaryCertificate, LoggableInformation loggableInformation)
        {
            var dsaCryptoService = (DSA)primaryCertificate.PublicKey.Key;

            DSAParameters publicKey = dsaCryptoService.ExportParameters(false);

            if (Base64Url.Encode(publicKey.X) != dsaKey.XCoordinate || Base64Url.Encode(publicKey.Y) != dsaKey.YCoordinate)
            {
                throw new KeyDiscoveryException("x509chain DSA public key mismatch", loggableInformation);
            }
        }

        private static void EnsureRsaKeyIsValid(RsaJsonWebKey rsaKey, X509Certificate2 primaryCertificate, LoggableInformation loggableInformation)
        {
            var rsaCryptoService = (RSA)primaryCertificate.PublicKey.Key;

            RSAParameters publicKey = rsaCryptoService.ExportParameters(false);
            if (Base64Url.Encode(publicKey.Modulus) != rsaKey.Modulus || Base64Url.Encode(publicKey.Exponent) != rsaKey.Exponent)
            {
                throw new KeyDiscoveryException("x509chain RSA public key mismatch", loggableInformation);
            }
        }
    }
}
