namespace Microsoft.PrivacyServices.CommandFeed.Client.Test.PrivacyCommandValidator
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery;

    using JsonWebKey = CommandFeed.Validator.KeyDiscovery.Keys.JsonWebKey;

    /// <summary>
    /// Base class for validator tests
    /// </summary>
    public class ValidatorTestBase
    {
        protected const string MsaIssuerUrl = "https://gdpr.login.live-int.com/4925308c-f164-4d2d-bc7e-0631132e9375";

        protected const string AadIssuerUrl = "https://aadrvs-ppe.msidentity.com/";

        protected const string SigningAlgo = "RS256";

        /// <summary>
        /// Constructor. Creates in memory cache.
        /// </summary>
        public ValidatorTestBase()
        {
            this.MsaTestSigningCredentials = VerifierTokenMinter.GenerateSigningCredentials(MsaIssuerUrl, out JsonWebKey msaJwk);
            this.MsaTestKeyId = msaJwk.KeyId;

            this.AadTestSigningCredentials = VerifierTokenMinter.GenerateSigningCredentials(AadIssuerUrl, out JsonWebKey aadJwk);
            this.AadTestKeyId = aadJwk.KeyId;

            var cache = new InMemoryCache();
            cache.WriteAsync(
                new Dictionary<string, CacheItem>
                {
                    { msaJwk.KeyId, new CacheItem(msaJwk, true, DateTimeOffset.UtcNow.AddHours(1)) },
                    { aadJwk.KeyId, new CacheItem(aadJwk, true, DateTimeOffset.UtcNow.AddHours(1)) }
                },
                CancellationToken.None).Wait();

            this.TestValidationService = new ValidationService(PcvEnvironment.Preproduction, cache);

            this.ProductionValidationService = new ValidationService();
        }

        /// <summary>
        /// Test validator service
        /// </summary>
        public IValidationService TestValidationService { get; }

        /// <summary>
        /// Production validator service
        /// </summary>
        public IValidationService ProductionValidationService { get; }

        /// <summary>
        /// Aad test signing Credentials
        /// </summary>
        public SigningCredentials AadTestSigningCredentials { get; }

        /// <summary>
        /// Msa test signing Credentials
        /// </summary>
        public SigningCredentials MsaTestSigningCredentials { get; }

        /// <summary>
        /// Msa test key Id
        /// </summary>
        public string MsaTestKeyId { get; }

        /// <summary>
        /// Aad test key id
        /// </summary>
        public string AadTestKeyId { get; }

        /// <summary>
        /// Aad tenant id
        /// </summary>
        protected Guid AadTid { get; } = new Guid("3b6b9028-92c8-48ce-9050-03c39183bae7");

        /// <summary>
        /// Run validation e2e
        /// </summary>
        public async Task EnsureValidAsyncEndToEnd(Type subjectType, CommandClaims commandClaims, IList<Claim> verifierClaims = null)
        {
            string verifier;
            if (subjectType == typeof(AadSubject2))
            {
                IList<Claim> aadclaims = verifierClaims ?? commandClaims.GenerateClaims(this.AadTestKeyId, "RS256");
                verifier = VerifierTokenMinter.MintVerifier(aadclaims, AadIssuerUrl, this.AadTestSigningCredentials);
            }
            else if (subjectType == typeof(AadSubject))
            {
                IList<Claim> aadclaims = verifierClaims ?? commandClaims.GenerateClaims(this.AadTestKeyId, "RS256");
                verifier = VerifierTokenMinter.MintVerifier(aadclaims, AadIssuerUrl, this.AadTestSigningCredentials);
            }
            else if (subjectType == typeof(MsaSubject))
            {
                IList<Claim> msaClaims = verifierClaims ?? commandClaims.GenerateClaims(MsaTestKeyId, "RS256");
                verifier = VerifierTokenMinter.MintVerifier(msaClaims, MsaIssuerUrl, MsaTestSigningCredentials);
            }
            else
            {
                throw new NotImplementedException();
            }

            await this.TestValidationService.EnsureValidAsync(verifier, commandClaims, CancellationToken.None);
        }
    }
}
