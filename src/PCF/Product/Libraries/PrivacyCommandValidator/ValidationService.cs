namespace Microsoft.PrivacyServices.CommandFeed.Validator
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using TokenValidationParameters = IdentityModel.Tokens.TokenValidationParameters;
    using X509SecurityKey = IdentityModel.Tokens.X509SecurityKey;

    /// <summary>
    /// Validates the PrivacyCommand.
    /// </summary>
    public sealed class ValidationService : IValidationService
    {
        private IKeyDiscoveryServiceFactory keyDiscoveryServiceFactory;
        private JwtSecurityTokenHandler jwtSecurityTokenHandler;
        private ITokenValidator tokenValidator;

        private readonly ICache cache;
        private readonly PcvEnvironment environment;

        /// <summary>
        /// Initializes a new <see cref="ValidationService"/>.
        /// </summary>
        /// <param name="environment">The environment in which the package is running</param>
        /// <param name="cache">Custom Implementation of the ICache</param>
        public ValidationService(PcvEnvironment environment = PcvEnvironment.Production, ICache cache = null)
        {
            this.cache = cache;
            this.environment = environment;
        }

        /// <summary>
        /// IKeyDiscoveryServiceFactory used for key discovery
        /// </summary>
        /// <remarks>Removed from test coverage due to it only being used by unit tests.</remarks>
        [ExcludeFromCodeCoverage]
        internal IKeyDiscoveryServiceFactory KeyDiscoveryServiceFactory
        {
            get => this.keyDiscoveryServiceFactory ?? (this.keyDiscoveryServiceFactory = new KeyDiscoveryServiceFactory());

            set => this.keyDiscoveryServiceFactory = value;
        }

        /// <summary>
        /// ITokenValidator used for validating the token and claims
        /// </summary>
        /// <remarks>Removed from test coverage due to it only being used by unit tests.</remarks>
        [ExcludeFromCodeCoverage]
        internal ITokenValidator TokenValidator
        {
            get => this.tokenValidator ?? (this.tokenValidator = new TokenValidator());

            set => this.tokenValidator = value;
        }

        /// <summary>
        /// JwtSecurityHandler to validate the JWTToken
        /// </summary>
        /// <remarks>Removed from test coverage due to it only being used by unit tests.</remarks>
        [ExcludeFromCodeCoverage]
        public JwtSecurityTokenHandler JwtSecurityTokenHandler
        {
            get
            {
                if (this.jwtSecurityTokenHandler != null)
                {
                    return this.jwtSecurityTokenHandler;
                }
                else
                {
                    this.jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

                    // Do not use claim type mappings.
                    // Mappings set in System.IdentityModel.Tokens.Jwt map “tid” and “oid” special TenantId and ObjectId types.
                    // The side effect of this special mapping is that the types become difficult to read and becoming buried deep in the claim object.
                    // By resetting the inbound claim type map, we can avoid this unnecessary type conversion and thus get the claim values more easily.
                    this.jwtSecurityTokenHandler.InboundClaimTypeMap = new Dictionary<string, string>();

                    return this.jwtSecurityTokenHandler;
                }
            }

            set => this.jwtSecurityTokenHandler = value;
        }

        /// <inheritdoc />
        public List<KeyDiscoveryConfiguration> SovereignCloudConfigurations { get; set; } = new List<KeyDiscoveryConfiguration>();

        /// <inheritdoc />
        public async Task EnsureValidAsync(string verifier, CommandClaims commandClaims, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(verifier) && this.environment == PcvEnvironment.Preproduction)
            {
                return;
            }

            if (commandClaims?.Subject == null)
            {
                throw new ArgumentException("validatableClaims should not be null and should contain IPrivacySubject");
            }

            if (string.IsNullOrWhiteSpace(verifier))
            {
                if (commandClaims.Subject.GetType() == typeof(DemographicSubject) || commandClaims.Subject.GetType() == typeof(EdgeBrowserSubject) || commandClaims.Subject.GetType() == typeof(MicrosoftEmployee))
                {
                    return;
                }

                throw new ArgumentException("verifier cannot be null");
            }

            var loggableInformation = new LoggableInformation(commandClaims.CommandId, commandClaims.Subject.GetType().ToString());

            var environmentConfiguration = EnvironmentConfiguration.GetEnvironmentConfiguration(
                    commandClaims.Subject,
                    commandClaims.CloudInstance,
                    this.SovereignCloudConfigurations,
                    this.environment,
                    loggableInformation);

            var keyDiscoveryService = this.KeyDiscoveryServiceFactory.GetKeyDiscoveryService(
                commandClaims.Subject,
                environmentConfiguration,
                commandClaims.CloudInstance,
                loggableInformation,
                this.cache);

            // Parse the verifier string
            JwtSecurityToken jwtSecurityToken;
            try
            {
                jwtSecurityToken = new JwtSecurityToken(verifier);
            }
            catch (SecurityTokenException e)
            {
                throw new InvalidPrivacyCommandException("Verifier is invalid", e, loggableInformation);
            }
            catch (ArgumentException e)
            {
                throw new InvalidPrivacyCommandException("Verifier is invalid", e, loggableInformation);
            }

            loggableInformation.JwtId = jwtSecurityToken.Id;
            
            this.TokenValidator.RunPrechecksOnToken(jwtSecurityToken, commandClaims.Subject, loggableInformation, environmentConfiguration);

            X509Certificate2 certificate = await keyDiscoveryService.GetCertificate(
                jwtSecurityToken.GetKeyId(),
                loggableInformation,
                cancellationToken).ConfigureAwait(false);

            var parameters = new ExtendedTokenValidationParameters
            {
                ValidIssuer = jwtSecurityToken.Issuer,
                IssuerSigningKeyResolver = IssuerSigningKeyResolver,
                //using below query identifier for suppressing CodeQL warning for ValidateAudience set to false.
                //ValidateAudience cannot be set to true, as there is no aud claim in the Verifier and is so by design, hence we cannot have valid audiences in request
                ValidateAudience = false, // lgtm[cs/jwtvalidationparameters]
                Certificate = certificate
            };

            ClaimsPrincipal claimsPrincipal;
            try
            {
                claimsPrincipal = this.JwtSecurityTokenHandler.ValidateToken(verifier, parameters, out var _);
            }
            catch (Exception e)
            {
                throw new InvalidPrivacyCommandException("Verifier is invalid", e, loggableInformation);
            }

            this.TokenValidator.ValidateCommand(commandClaims, loggableInformation, claimsPrincipal.Claims);
        }

        private static IEnumerable<SecurityKey> IssuerSigningKeyResolver(string token, SecurityToken securityToken, string kid, TokenValidationParameters validationParameters)
        {
            var parameters = (ExtendedTokenValidationParameters)validationParameters;
            return new[] { new X509SecurityKey(parameters.Certificate) };
        }

        private class ExtendedTokenValidationParameters : TokenValidationParameters
        {
            public X509Certificate2 Certificate { get; set; }
        }
    }
}
