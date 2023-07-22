namespace Microsoft.PrivacyServices.CommandFeed.Client.Test.PrivacyCommandValidator
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery;
    using Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery.Keys;
    using Org.BouncyCastle.Asn1;
    using Org.BouncyCastle.Asn1.Pkcs;
    using Org.BouncyCastle.Asn1.X509;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Generators;
    using Org.BouncyCastle.Crypto.Operators;
    using Org.BouncyCastle.Crypto.Parameters;
    using Org.BouncyCastle.Crypto.Prng;
    using Org.BouncyCastle.Math;
    using Org.BouncyCastle.Pkcs;
    using Org.BouncyCastle.Security;
    using Org.BouncyCastle.Utilities;
    using Org.BouncyCastle.X509;
    using JsonWebKey = CommandFeed.Validator.KeyDiscovery.Keys.JsonWebKey;
    using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

    /// <summary>
    /// Mints test verifier strings.
    /// </summary>
    internal class VerifierTokenMinter
    {
        /// <summary>
        /// Creates and signs a new verifier string.
        /// </summary>
        /// <param name="claims">The claims in the JWT payload</param>
        /// <param name="issuer">The verifier Jwt token issuer</param>
        /// <param name="credentials">The signing credentials</param>
        /// <returns>A verifier string signed using test credentials</returns>
        internal static string MintVerifier(IEnumerable<Claim> claims, string issuer, SigningCredentials credentials)
        {
            var handler = new JwtSecurityTokenHandler();
            handler.InboundClaimTypeMap = new Dictionary<string, string>();


            JwtSecurityToken token = handler.CreateJwtSecurityToken(
                issuer,
                subject: new ClaimsIdentity(claims),
                signingCredentials: credentials);
            return handler.WriteToken(token);
        }

        /// <summary>
        /// Generates signing credentials capable of signing a Jwt.
        /// </summary>
        /// <param name="issuer">The Jwt issuer</param>
        /// <param name="key">The resulting Jwk</param>
        /// <returns>The signing credential</returns>
        internal static SigningCredentials GenerateSigningCredentials(string issuer, out JsonWebKey key)
        {
            X509Certificate2 cert = CreateSelfSignedCertificate(issuer);
            string keyId = Base64Url.Encode(StringToByteArray(cert.Thumbprint));

            var signingCredentials = new SigningCredentials(
                new X509SecurityKey(cert),
                SecurityAlgorithms.RsaSha256Signature,
                SecurityAlgorithms.Sha256Digest);

            var rsaCryptoService = (RSACryptoServiceProvider)cert.PublicKey.Key;
            RSAParameters publicKey = rsaCryptoService.ExportParameters(false);
            key = new RsaJsonWebKey
            {
                KeyId = signingCredentials.Kid,
                X509Thumbprint = keyId,
                X509Chain = new[]
                {
                    Convert.ToBase64String(cert.Export(X509ContentType.SerializedCert))
                },
                KeyType = JwkKeyType.RSA,
                PublicKeyUse = JwkKeyUse.Signature,
                Exponent = Base64Url.Encode(publicKey.Exponent),
                Modulus = Base64Url.Encode(publicKey.Modulus)
            };

            return signingCredentials;
        }

        private static X509Certificate2 CreateSelfSignedCertificate(string issuer)
        {
            const int keyStrength = 2048;

            // Generate Random Numbers
            var randomGenerator = new CryptoApiRandomGenerator(); // lgtm [cs/use-approved-crypto-library]
            var random = new SecureRandom(randomGenerator); // lgtm [cs/use-approved-crypto-library]

            // Key Usage Extensions
            var certificateGenerator = new X509V3CertificateGenerator(); // lgtm [cs/use-approved-crypto-library]
            var eku = new ExtendedKeyUsage( // lgtm [cs/use-approved-crypto-library]
                new[]
                {
                    new DerObjectIdentifier("1.3.6.1.5.5.7.3.1"),// lgtm [cs/use-approved-crypto-library]
                    new DerObjectIdentifier("1.3.6.1.5.5.7.3.2") // lgtm [cs/use-approved-crypto-library]
                });
            certificateGenerator.AddExtension(X509Extensions.ExtendedKeyUsage, true, eku);// lgtm [cs/use-approved-crypto-library]

            // Serial Number
            BigInteger serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);// lgtm [cs/use-approved-crypto-library]
            certificateGenerator.SetSerialNumber(serialNumber);// lgtm [cs/use-approved-crypto-library]

            // Issuer and Subject Name
            var name = new X509Name($"CN={issuer}");// lgtm [cs/use-approved-crypto-library]
            certificateGenerator.SetSubjectDN(name);// lgtm [cs/use-approved-crypto-library]
            certificateGenerator.SetIssuerDN(name);// lgtm [cs/use-approved-crypto-library]

            // Valid For
            DateTime notBefore = DateTime.UtcNow.Date;
            DateTime notAfter = notBefore.AddHours(1);
            certificateGenerator.SetNotBefore(notBefore);// lgtm [cs/use-approved-crypto-library]
            certificateGenerator.SetNotAfter(notAfter);// lgtm [cs/use-approved-crypto-library]

            // Generate Keys
            var keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);// lgtm [cs/use-approved-crypto-library]
            var keyPairGenerator = new RsaKeyPairGenerator();// lgtm [cs/use-approved-crypto-library]
            keyPairGenerator.Init(keyGenerationParameters);// lgtm [cs/use-approved-crypto-library]
            AsymmetricCipherKeyPair subjectKeyPair = keyPairGenerator.GenerateKeyPair();// lgtm [cs/use-approved-crypto-library]

            // Public Key
            certificateGenerator.SetPublicKey(subjectKeyPair.Public);// lgtm [cs/use-approved-crypto-library]

            // Generate cert factory
            ISignatureFactory signatureFactory = new Asn1SignatureFactory("SHA512WITHRSA", subjectKeyPair.Private, random);// lgtm [cs/use-approved-crypto-library]

            // Generate X509Certificate
            X509Certificate cert = certificateGenerator.Generate(signatureFactory);// lgtm [cs/use-approved-crypto-library]
            var x509 = new X509Certificate2(cert.GetEncoded());// lgtm [cs/use-approved-crypto-library]

            // Corresponding private key
            PrivateKeyInfo privateKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(subjectKeyPair.Private);// lgtm [cs/use-approved-crypto-library]

            var seq = (Asn1Sequence)Asn1Object.FromByteArray(privateKeyInfo.ParsePrivateKey().GetDerEncoded());// lgtm [cs/use-approved-crypto-library]
            if (seq.Count != 9)
            {
                // throw new PemException("malformed sequence in RSA private key");
            }

            RsaPrivateKeyStructure rsa = RsaPrivateKeyStructure.GetInstance(seq); // lgtm [cs/use-approved-crypto-library]
            var rsaparams = new RsaPrivateCrtKeyParameters( // lgtm [cs/use-approved-crypto-library]
                rsa.Modulus,
                rsa.PublicExponent,
                rsa.PrivateExponent,
                rsa.Prime1,
                rsa.Prime2,
                rsa.Exponent1,
                rsa.Exponent2,
                rsa.Coefficient);

            x509.PrivateKey = DotNetUtilities.ToRSA(rsaparams);// lgtm [cs/use-approved-crypto-library] // lgtm [cs/cryptography/default-rsa-key-construction]
            return x509;
        }

        private static byte[] StringToByteArray(string hex)
        {
            int numberChars = hex.Length;
            var bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }
    }
}
