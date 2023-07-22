namespace Microsoft.PrivacyServices.CommandFeed.Client.Test.PrivacyCommandValidator.Mocks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery;
    using Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery.Keys;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;

    using Moq;

    /// <inheritdoc />
    public class MockKeyDiscoveryService : IMocked<IKeyDiscoveryService>
    {
        private readonly ICache mockCache;

        /// <inheritdoc />
        public Mock<IKeyDiscoveryService> Mock { get; }

        public MockKeyDiscoveryService(ICache cache)
        {
            this.mockCache = cache;
            this.Mock = new Mock<IKeyDiscoveryService>();
            this.Mock.Setup(provider => provider.GetCertificate(It.IsAny<string>(), It.IsAny<LoggableInformation>(), It.IsAny<CancellationToken>()))
                .Returns(
                    (string key, LoggableInformation loggableInformation, CancellationToken cancellationToken) => this.GetCertificate(key, loggableInformation, cancellationToken));
        }

        /// <inheritdoc />
        public async Task<X509Certificate2> GetCertificate(string keyId, LoggableInformation loggableInformation, CancellationToken cancellationToken)
        {
            CacheItem cacheItem = await this.mockCache.ReadAsync(keyId).ConfigureAwait(false);

            // Roslyn analyzer bug: https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2570
#pragma warning disable SA1119 // Statement must not use unnecessary parenthesis
            if (!(cacheItem.Item is RsaJsonWebKey key))
#pragma warning restore SA1119 // Statement must not use unnecessary parenthesis
            {
                return null;
            }

            // Build x509cert chain. X5c param is guaranteed to be not null.
            if (!key.X509Chain.Any())
            {
                throw new InvalidPrivacyCommandException("x509chain parameter not present in key.", loggableInformation);
            }

            // Convert all Base64 encoded DER certificates.
            IEnumerable<X509Certificate2> certificates = key.X509Chain.Select(c => new X509Certificate2(Convert.FromBase64String(c))).ToList();
            X509Certificate2 primaryCertificate = certificates.First();

            // TODO: expand support for DSA certificates
            var rsaCryptoService = (RSA)primaryCertificate.PublicKey.Key;

            RSAParameters publicKey = rsaCryptoService.ExportParameters(false);
            if (Base64Url.Encode(publicKey.Modulus) != key.Modulus || Base64Url.Encode(publicKey.Exponent) != key.Exponent)
            {
                throw new InvalidPrivacyCommandException("x509chain public key mismatch", loggableInformation);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            return primaryCertificate;
        }

        /// <inheritdoc />
        Mock IMocked.Mock => this.Mock;
    }
}
