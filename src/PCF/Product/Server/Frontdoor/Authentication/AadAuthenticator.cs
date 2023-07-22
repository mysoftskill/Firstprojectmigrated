namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Security.Claims;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.IdentityModel.Protocols.OpenIdConnect;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.Windows.Services.AuthN.Server;

    /// <summary>
    /// Authenticates the ticket with AAD
    /// </summary>
    public class AadAuthenticator : IAuthenticator
    {
        private readonly IAuthenticator innerAuthenticator;

        /// <summary>
        /// Initializes a new instance of the class <see cref="AadAuthenticator" />.
        /// </summary>
        /// <param name="authenticator">Inner authenticator to use.</param>
        public AadAuthenticator(IAuthenticator authenticator)
        {
            this.innerAuthenticator = authenticator;
        }

        /// <inheritdoc />
        public async Task<PcfAuthenticationContext> AuthenticateAsync(HttpRequestHeaders requestHeaders, X509Certificate2 clientCertificate)
        {
            PcfAuthenticationContext authContext;
            if (this.innerAuthenticator == null)
            {
                authContext = new PcfAuthenticationContext();
            }
            else
            {
                authContext = await this.innerAuthenticator.AuthenticateAsync(requestHeaders, clientCertificate);
            }

            if (ServiceAuthorizer.TryGetAadToken(requestHeaders, out string token))
            {
                authContext.AuthenticatedAadAppId = await AuthenticateMsaAuthToken(token);
            }

            return authContext;
        }

        private static async Task<Guid> AuthenticateMsaAuthToken(string token)
        {
            IncomingEvent.Current?.SetProperty("S2SAuthProvider", "AAD");

            JwtSecurityToken jwtSecurityToken;
            try
            {
                jwtSecurityToken = new JwtSecurityToken(token);
            }
            catch (SecurityTokenException e)
            {
                IncomingEvent.Current?.SetProperty("AuthNError", "SecurityTokenException while parsing the bearer token.");
                throw new AuthNException(AuthNErrorCode.AuthenticationFailed, "Invalid bearer token", e);
            }
            catch (ArgumentException e)
            {
                IncomingEvent.Current?.SetProperty("AuthNError", "Bearer token is of invalid format.");
                throw new AuthNException(AuthNErrorCode.AuthenticationFailed, "Invalid bearer token", e);
            }

            ClaimsPrincipal claimsPrincipal;
            IncomingEvent.Current?.SetProperty("AADSecurityTokenIssuer", jwtSecurityToken.Issuer);
            var authenticationConfig = AadAuthenticationConfiguration.GetAuthenticationConfiguration(jwtSecurityToken.Issuer, jwtSecurityToken.Audiences);

            claimsPrincipal = await GetClaimsPrincipalAsync(token, jwtSecurityToken, authenticationConfig);

            string appId = claimsPrincipal?.FindFirst("appid")?.Value;
            if (appId == null || !Guid.TryParse(appId, out Guid appIdGuid))
            {
                IncomingEvent.Current?.SetProperty("AuthNError", "MissingAppIdInTicket");
                throw new AuthNException(AuthNErrorCode.InvalidTicket, "Missing AppID in ticket");
            }

            IncomingEvent.Current?.SetProperty("AuthorizedId", appIdGuid.ToString());

            return appIdGuid;
        }

        private static async Task<ClaimsPrincipal> GetClaimsPrincipalAsync(
            string bearerToken,
            JwtSecurityToken jwtSecurityToken,
            AadAuthenticationConfiguration authenticationConfig)
        {
            string issuer = authenticationConfig.Issuer;

            try
            {
                var config = await GetOpenIdConfigurationWithRetryAsync(authenticationConfig).ConfigureAwait(false);
                issuer = config.Issuer;

                var validationParameters = new TokenValidationParameters
                {
                    ValidAudiences = authenticationConfig.Audiences,
                    ValidateAudience = true,
                    ValidIssuer = config.Issuer,
                    ValidateIssuer = true,
                    IssuerSigningKeys = config.SigningKeys
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                return tokenHandler.ValidateToken(bearerToken, validationParameters, out SecurityToken validatedToken);
            }
            catch (Exception e)
            {
                IncomingEvent.Current?.SetProperty("AuthNError", "Exception while validating bearer token");
                IncomingEvent.Current?.SetProperty("Issuer", issuer);
                IncomingEvent.Current?.SetProperty("Audiences", string.Join(",", jwtSecurityToken.Audiences));
                IncomingEvent.Current?.SetProperty("Valid Audiences", string.Join(",", authenticationConfig.Audiences));
                IncomingEvent.Current?.SetProperty("AuthNException", e.Message);

                throw new AuthNException(AuthNErrorCode.AuthenticationFailed, "Invalid bearer token", e);
            }
        }

        /// <summary>
        /// Fetches OpenIdConfiguration, retries with an exponential back off.
        /// </summary>
        /// <param name="authenticationConfig">Authentication config.</param>
        /// <returns>OpenIdConfiguration</returns>
        private static async Task<OpenIdConnectConfiguration> GetOpenIdConfigurationWithRetryAsync(AadAuthenticationConfiguration authenticationConfig)
        {
            int maxTries = 3;
            List<Exception> caughtExceptions = new List<Exception>();
            for (int currentAttempt = 1; currentAttempt <= maxTries; currentAttempt++)
            {
                try
                {
                    return await authenticationConfig.ConfigurationManager.GetConfigurationAsync(CancellationToken.None);
                }
                catch (Exception e)
                {
                    caughtExceptions.Add(e);
                    await Task.Delay( TimeSpan.FromSeconds((int)Math.Pow(2, currentAttempt)));
                }
            }

            throw new AggregateException(caughtExceptions);
        }
    }
}
