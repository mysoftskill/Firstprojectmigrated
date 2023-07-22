// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Tracing;
    using System.IdentityModel.Tokens.Jwt;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Security.Claims;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Http.Filters;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Extensions.Primitives;
    using Microsoft.Identity.Client;
    using Microsoft.Identity.ServiceEssentials;
    using Microsoft.IdentityModel.Logging;
    using Microsoft.IdentityModel.Protocols.OpenIdConnect;
    using Microsoft.IdentityModel.S2S;
    using Microsoft.IdentityModel.S2S.Logging;
    using Microsoft.IdentityModel.S2S.Tokens;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.PrivacyServices.Common.Azure;
    using System.Net.Http;

    /// <summary>
    ///     AadAuthManager
    /// </summary>
    public class AadAuthManager : IAadAuthManager
    {
        private const string PftBearer = "Bearer";

        private const char PftSeparator = ',';

        private readonly string aadLoginEndpoint;

        private readonly string aadAuthority;

        private readonly X509Certificate2 certificate;

        private readonly IPrivacyConfigurationManager configManager;

        private readonly IAadTokenAuthConfiguration aadTokenAuthConfig;

        private readonly JwtAuthenticationHandler jwtHandler;

        private readonly ILogger logger;

        private readonly IDictionary<OutboundPolicyName, IJwtOutboundPolicyConfig> outboundPolicies;

        private readonly string stsAuthorityEndpoint;

        private readonly IAadJwtSecurityTokenHandler tokenHandler;

        private readonly ITokenManager tokenManager;

        private readonly IList<string> validIncomingAppIds;

        private readonly IAppConfiguration appConfiguration;

        private readonly IMiseTokenValidationUtility miseTokenHandler;

        /// <inheritdoc />
        public string AadLoginEndpoint
        {
            get { return this.aadLoginEndpoint; }
        }

        /// <inheritdoc />
        public string StsAuthorityEndpoint
        {
            get { return this.stsAuthorityEndpoint; }
        }

        /// <summary>
        ///     Creates a new instance of <see cref="AadAuthManager" />
        /// </summary>
        /// <param name="config">The aad config</param>
        /// <param name="certProvider">The cert provider</param>
        /// <param name="logger">logger</param>
        /// <param name="tokenManager">The token manager</param>
        /// <param name="miseTokenHandler"></param>
        /// <param name="tokenHandler">The token handler</param>
        /// <param name="appConfiguration"></param>
        public AadAuthManager(
            IPrivacyConfigurationManager config,
            ICertificateProvider certProvider,
            ILogger logger,
            ITokenManager tokenManager,
            IAadJwtSecurityTokenHandler tokenHandler,
            IMiseTokenValidationUtility miseTokenHandler,
            IAppConfiguration appConfiguration)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (certProvider == null)
                throw new ArgumentNullException(nameof(certProvider));

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
            this.tokenHandler = tokenHandler ?? throw new ArgumentNullException(nameof(tokenHandler));
            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));

            //Set up Mise for token validation
            this.miseTokenHandler = miseTokenHandler ?? throw new ArgumentNullException(nameof(miseTokenHandler));

            if (config.AadTokenAuthGeneratorConfiguration == null)
            {
                throw new NullReferenceException($"{nameof(config.AadTokenAuthGeneratorConfiguration)} was null.");
            }
            
            this.configManager = config;
            this.aadTokenAuthConfig = config.AadTokenAuthGeneratorConfiguration;
            this.certificate = certProvider.GetClientCertificate(this.aadTokenAuthConfig.RequestSigningCertificateConfiguration.Subject);

            this.jwtHandler = new JwtAuthenticationHandler();
            this.validIncomingAppIds = this.aadTokenAuthConfig.JwtInboundPolicyConfig.ValidIncomingAppIds;
            JwtInboundPolicy jwtInboundPolicy = CreateInboundPolicy(this.aadTokenAuthConfig);
            this.jwtHandler.InboundPolicies.Add(jwtInboundPolicy);

            this.outboundPolicies = new Dictionary<OutboundPolicyName, IJwtOutboundPolicyConfig>();

            IDictionary<string, IJwtOutboundPolicyConfig> outboundPolicyConfiguration =
                config.AadTokenAuthGeneratorConfiguration.JwtOutboundPolicyConfig ??
                throw new ArgumentNullException(nameof(config.AadTokenAuthGeneratorConfiguration.JwtOutboundPolicyConfig));

            foreach (KeyValuePair<string, IJwtOutboundPolicyConfig> outboundPolicy in outboundPolicyConfiguration)
            {
                if (Enum.TryParse(outboundPolicy.Key, out OutboundPolicyName key))
                {
                    this.outboundPolicies.Add(key, outboundPolicy.Value);
                }
                else
                {
                    throw new KeyNotFoundException($"Invalid enumeration specified: {outboundPolicy.Key}");
                }
            }

            this.aadLoginEndpoint = this.aadTokenAuthConfig.AadLoginEndpoint?.TrimEnd('/');
            string aadLoginAuthorityTenantId = this.aadTokenAuthConfig.AuthorityTenantId;
            this.stsAuthorityEndpoint = this.aadTokenAuthConfig.StsAuthorityEndpoint;
            this.aadAuthority = $"https://{this.aadLoginEndpoint}/{aadLoginAuthorityTenantId}";

            // Allows for more useful diagnostic logs
            if (this.aadTokenAuthConfig.EnableShowPIIDiagnosticLogs)
            {
                IdentityModelEventSource.ShowPII = true;
                S2SEventSource.ShowPII = true;
                S2SEventSource.Instance.EventLevel = EventLevel.Verbose;
            }

            if (appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.AuthenticationLogging).ConfigureAwait(false).GetAwaiter().GetResult())
            {
                this.logger.Information(nameof(AadAuthManager), $"aadTokenAuthConfig: cert: {this.certificate.Subject}, aadAuthority: {this.aadAuthority}, stsAuthorityEnpoint: {this.stsAuthorityEndpoint}");
                foreach (var policy in this.jwtHandler.InboundPolicies)
                {
                    this.logger.Information(nameof(AadAuthManager), $"inboundPolicy: {FormatJwtInboundPolicy(policy)}");
                }
                foreach (var policy in this.outboundPolicies)
                {
                    this.logger.Information(nameof(AadAuthManager), $"outboundPolicy: name: {policy.Key}, {FormatJwtOutboundPolicyConfig(policy.Value)}");
                }
            }
        }

        /// <inheritdoc />
        public async Task<string> GetAccessTokenAsync(string resourceId)
        {
            try
            {
                return await this.tokenManager.GetAppTokenAsync(
                    this.aadAuthority,
                    this.aadTokenAuthConfig.AadAppId,
                    resourceId,
                    this.certificate,
                    cacheable: true,
                    this.logger).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                string errorMessage =
                    $"An unknown exception occurred getting the aad s2s token for {nameof(resourceId)}: {resourceId}. " +
                    $"AppId: {this.aadTokenAuthConfig.AadAppId}. " +
                    $"Certificate Thumbprint: {this.certificate?.Thumbprint}";
                this.logger.Error(nameof(AadAuthManager), e, errorMessage);
                ErrorEvent errorEvent = new ErrorEvent
                {
                    ComponentName = nameof(AadAuthManager),
                    ErrorMethod = nameof(this.GetAccessTokenAsync),
                    ErrorName = "FailedToAcquireAccessToken",
                    ErrorCode = "Authorization",
                    ErrorMessage = errorMessage,
                    ErrorDetails = e.ToString()
                };
                errorEvent.LogError();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task SetAuthorizationHeaderAppTokenAsync(HttpRequestHeaders httpRequestHeaders, IJwtOutboundPolicy jwtOutboundPolicy, bool hasPreverifier = false, bool useAadRvsAppId = false)
        {
            string appToken = string.Empty;
            try
            {
                appToken = await this.GetAppTokenAsync(jwtOutboundPolicy, useAadRvsAppId).ConfigureAwait(false);
            }
            catch (MsalUiRequiredException e) when (e.ErrorCode.Equals("invalid_grant"))
            {
                if (hasPreverifier)
                {
                    this.logger.Information(nameof(AadAuthManager), $"Didn't get app token successfully for tenant {jwtOutboundPolicy.TargetTenantId} due to \"invalid_grant\". Will try to reprocess with default tenant {configManager.AadTokenAuthGeneratorConfiguration.AuthorityTenantId}");
                    appToken = await this.GetAppTokenAsync(OutboundPolicyName.AadRvsConstructAccountClose, useAadRvsAppId).ConfigureAwait(false);
                }
                else
                {
                    this.logger.Error(nameof(AadAuthManager), $"Didn't get app token successfully for tenant {jwtOutboundPolicy.TargetTenantId} due to \"invalid_grant\".");
                    throw;
                }
            }
            httpRequestHeaders.TryAddWithoutValidation(HttpRequestHeader.Authorization.ToString(), $"{PftBearer} {appToken}");
        }

        /// <inheritdoc />
        public async Task SetAuthorizationHeaderAppTokenAsync(HttpRequestHeaders httpRequestHeaders, OutboundPolicyName outboundPolicyName, bool hasPreverifier = false, bool useAadRvsAppId = false)
        {
            var appToken = await this.GetAppTokenAsync(outboundPolicyName, useAadRvsAppId).ConfigureAwait(false);
            httpRequestHeaders.TryAddWithoutValidation(HttpRequestHeader.Authorization.ToString(), $"{PftBearer} {appToken}");
        }

        /// <inheritdoc />
        public async Task SetAuthorizationHeaderProtectedForwardedTokenAsync(HttpRequestHeaders httpRequestHeaders, string accessToken, OutboundPolicyName outboundPolicyName, bool useAadRvsAppId = false)
        {
            this.logger.Information(nameof(AadAuthManager), $"SetAuthorizationHeaderProtectedForwardedTokenAsync: outboundPolicyName: {outboundPolicyName}");

            string appToken = await this.GetAppTokenAsync(outboundPolicyName, useAadRvsAppId).ConfigureAwait(false);
            string forwardedAuthorizationHeaderValue = TokenCreator.CreateMSAuth1_0PFATHeader(accessToken, appToken);

            if (string.IsNullOrEmpty(forwardedAuthorizationHeaderValue))
            {
                this.logger.Error(nameof(AadAuthManager), "Failed to generate PFT");
            }

            httpRequestHeaders.TryAddWithoutValidation(HttpRequestHeader.Authorization.ToString(), forwardedAuthorizationHeaderValue);
        }

        /// <inheritdoc />
        public async Task SetAuthorizationHeaderProtectedForwardedTokenAsync(HttpRequestHeaders httpRequestHeaders, string accessToken, IJwtOutboundPolicy jwtOutboundPolicy, bool useAadRvsAppId = false)
        {
            this.logger.Information(nameof(AadAuthManager), $"SetAuthorizationHeaderProtectedForwardedTokenAsync: outboundPolicy: {FormatJwtOutboundPolicy(jwtOutboundPolicy)}");

            string appToken = await this.GetAppTokenAsync(jwtOutboundPolicy, useAadRvsAppId).ConfigureAwait(false);
            string forwardedAuthorizationHeaderValue = TokenCreator.CreateMSAuth1_0PFATHeader(accessToken, appToken);

            if (string.IsNullOrEmpty(forwardedAuthorizationHeaderValue))
            {
                this.logger.Error(nameof(AadAuthManager), "Failed to generate PFT");
            }

            httpRequestHeaders.TryAddWithoutValidation(HttpRequestHeader.Authorization.ToString(), forwardedAuthorizationHeaderValue);
        }

        /// <inheritdoc />
        public async Task<IAadS2SAuthResult> ValidateInboundJwtAsync(string headerValue)
        {
            if (!TryParseAuthorizationHeader(headerValue, "Bearer", out string parsedJwt))
            {
                var result = new S2SAuthenticationResult
                {
                    Succeeded = false,
                    Exception = new Exception("Invalid incoming access token.")
                };
                return new AadS2SAuthResult(result);
            }

            // JwtSecurityToken will need to be removed after confirming that MISE is working
            JwtSecurityToken token;
            IConnectConfigurationWrapper openIdConnectConfiguration;
            try
            {
                token = new JwtSecurityToken(parsedJwt);
                openIdConnectConfiguration = await this.tokenHandler.GetConnectConfigurationAsync(token, new OpenIdConnectConfigurationRetriever()).ConfigureAwait(false);
            }
            catch (SecurityTokenException e)
            {
                return new AadS2SAuthResult(e);
            }
            catch (ArgumentNullException e)
            {
                return new AadS2SAuthResult(e);
            }
            catch (ArgumentException e)
            {
                return new AadS2SAuthResult(e);
            }

            string receivingAadAppId = this.tokenHandler.MapTokenToAppId(token);

            try
            {
                ClaimsPrincipal claimsPrincipal = null;
                claimsPrincipal = await miseTokenHandler.AuthenticateAsync(headerValue).ConfigureAwait(false);
                
                if (claimsPrincipal == null || !claimsPrincipal.Identity.IsAuthenticated)
                {
                    return new AadS2SAuthResult(new UnauthorizedAccessException("Cannot validate the input JWT"));
                }

                if (!this.TryParseClaimsPrincipal(claimsPrincipal, token, out Guid oid, out Guid tid, out string appId))
                {
                    return new AadS2SAuthResult(new UnauthorizedAccessException("Cannot parse identity information from the input JWT"));
                }

                return new AadS2SAuthResult(oid, tid, appId);
            }
            catch (SecurityTokenException e)
            {
                return new AadS2SAuthResult(e);
            }
        }

        public async Task<bool> IsTenantIdValidAsync(string tenantId)
        {
            return await this.tokenHandler.IsTenantIdValidAsync(tenantId, new OpenIdConnectConfigurationRetriever());
        }

        /// <inheritdoc />
        public async Task<IAadS2SAuthResult> ValidateInboundPftAsync(AuthenticationHeaderValue authHeaderValue, Guid activityId)
        {
            this.logger.Information(nameof(MiseTokenValidationUtility), "Entering ValidateInboundPftAsync...");
            HttpRequestBase httpRequestBase = CreateHttpRequestBaseWithAuthHeaderValue(authHeaderValue);
            S2SAuthenticationResult authResult;

            var s2Scontext = new S2SContext
            {
                ActivityId = activityId,
                CaptureLogs = true,
            };
            if (!TryParseAccessToken(authHeaderValue.ToString(), out string accessToken))
            {
                var result = new S2SAuthenticationResult
                {
                    Succeeded = false,
                    Exception = new Exception("Invalid incoming PFT")
                };
                this.logger.Information(nameof(AadAuthManager), "Invalid incoming PFT");
                return new AadS2SAuthResult(result);
            }

            try
            {
                authResult = await this.jwtHandler.TryValidateAsync(
                    authorizationHeader: authHeaderValue.ToString(), 
                    uri: httpRequestBase.Url, 
                    httpMethod: httpRequestBase.HttpMethod, 
                    context: s2Scontext
                    ).ConfigureAwait(false);
                var claimsPrincipal = await miseTokenHandler.AuthenticateAsync(authHeaderValue.ToString());

                if (claimsPrincipal == null || !claimsPrincipal.Identity.IsAuthenticated)
                {
                    return new AadS2SAuthResult(new UnauthorizedAccessException("Cannot validate the incoming PFT"));
                }

                //Validate the auth result, if invalid, we will mark succeeded as false
                await ValidatePftAuthResultAsync(authResult, authHeaderValue).ConfigureAwait(false);
                return new AadS2SAuthResult(
                    s2sResult: authResult,
                    claims: claimsPrincipal,
                    accessTokenString: accessToken
                    );
            }
            catch (Exception e)
            {
                // It's bad to catch a general exception here, but we don't know why kind of exception the SAL library would throw
                this.logger.Error(nameof(AadAuthManager), $"Exception thrown by SAL when validating PFT: {e}");
                return new AadS2SAuthResult(e);
            }
        }

        private async Task ValidatePftAuthResultAsync(S2SAuthenticationResult authResult, AuthenticationHeaderValue authHeaderValue)
        {
            if (authResult.Succeeded)
            {
                // 'Microsoft.IdentityModel.S2S' validates appid's from access token in versions >= 2.1.1,
                // so we do not leverage it for that because we do not wish it to validate the access token's app id.
                // Instead, we validate it ourselves without the library.
                // Additional token details - there are two tokens in the auth header.
                // 1. access token is the 3rd party that is calling into Graph. We do not maintain an AllowedList for the app id's from that token, because we cannot presume to know all of those apps - they are 3rd party.
                // 2. actor token is the app calling directly into us, which we validate ourselves, see below.
                var appIdClaimValue = authResult?.Ticket?.ApplicationIdentity?.Claims?.FirstOrDefault(c => c.Type == "appid")?.Value;
                if (!this.validIncomingAppIds.Contains(appIdClaimValue))
                {
                    authResult.Succeeded = false;

                    var errorLog = new ErrorEvent
                    {
                        ComponentName = nameof(AadAuthManager),
                        ErrorCode = "Unauthorized",
                        ErrorName = "UnauthorizedActorTokenAppId",
                        ErrorMessage = $"The actortoken appid is not authorized for access: {appIdClaimValue}",
                        ErrorType = "Authorization",
                        ErrorMethod = nameof(this.ValidateInboundPftAsync)
                    };
                    errorLog.LogError();
                }
            }
            else
            {
                if (await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.AuthenticationLogging).ConfigureAwait(false))
                {
                    this.logger.Error(nameof(AadAuthManager), $"Error validating PFT: {authHeaderValue}");
                }
            }
        }

        private Task<string> GetAppTokenAsync(OutboundPolicyName targetOutboundPolicyName, bool useRVSAppId = false)
        {
            return this.outboundPolicies.TryGetValue(targetOutboundPolicyName, out IJwtOutboundPolicyConfig outboundPolicyConfig)
                ? this.tokenManager.GetAppTokenAsync(
                    authority: outboundPolicyConfig.Authority,
                    clientId: useRVSAppId ? this.aadTokenAuthConfig.AadRvsAppId : this.aadTokenAuthConfig.AadAppId, // PXS App ID
                    resource: outboundPolicyConfig.Resource,
                    certificate: this.certificate,
                    cacheable: true,
                    this.logger)
                : throw new ArgumentOutOfRangeException(
                    nameof(targetOutboundPolicyName),
                    targetOutboundPolicyName,
                    $"Target outbound poilcy not supported: {targetOutboundPolicyName}");
        }

        private Task<string> GetAppTokenAsync(IJwtOutboundPolicy targetJwtOutboundPolicy, bool useRVSAppId = false) 
        {
            return this.tokenManager.GetAppTokenAsync(
                authority: targetJwtOutboundPolicy.Authority,
                clientId: useRVSAppId ? this.aadTokenAuthConfig.AadRvsAppId: this.aadTokenAuthConfig.AadAppId, // PXS App ID
                resource: targetJwtOutboundPolicy.Resource,
                certificate: this.certificate,
                cacheable: true,
                this.logger);
        }

        private bool TryParseClaimsPrincipal(ClaimsPrincipal claimsPrincipal, JwtSecurityToken token, out Guid oId, out Guid tId, out string appId)
        {
            oId = default(Guid);
            tId = default(Guid);

            appId = claimsPrincipal.FindFirst("appid")?.Value;
            if (string.IsNullOrEmpty(appId))
            {
                this.logger.Error(nameof(AadAuthManager), $"Claim: 'appid' was not found in the {nameof(ClaimsPrincipal)}.");
                return false;
            }

            // oid isn't required all the time (some clouds don't populate it when the token is just an app token), so if it's not there it's a Warning only.
            string oidString = token.Claims?.FirstOrDefault(claim => claim.Type.Equals("oid", StringComparison.OrdinalIgnoreCase))?.Value;
            if (string.IsNullOrEmpty(oidString))
            {
                this.logger.Warning(nameof(AadAuthManager), $"Claim: 'oid' was not found in the {nameof(JwtSecurityToken)}");
            }
            else if (!Guid.TryParse(oidString, out oId))
            {
                this.logger.Warning(nameof(AadAuthManager), $"Claim: 'oid' could not be parsed to a Guid from the {nameof(JwtSecurityToken)}. Actual value: {oidString}");
            }

            string tidString = token.Claims?.FirstOrDefault(claim => claim.Type.Equals("tid", StringComparison.OrdinalIgnoreCase))?.Value;
            if (string.IsNullOrEmpty(tidString))
            {
                this.logger.Error(nameof(AadAuthManager), $"Claim: 'tid' was not found in the {nameof(JwtSecurityToken)}");
                return false;
            }

            if (!Guid.TryParse(tidString, out tId))
            {
                this.logger.Error(nameof(AadAuthManager), $"Claim: 'tid' could not be parsed to a Guid from the {nameof(JwtSecurityToken)}. Actual value: {tidString}");
                return false;
            }

            return true;
        }

        private string FormatJwtInboundPolicy(JwtInboundPolicy policy)
        {
            StringBuilder sb = new StringBuilder("JwtInboundPolicy: ");
            sb.Append($"Authority: {policy.Authority}, ClientId: {policy.ClientId}, ");
            sb.Append("Audiences: ");
            foreach (var audience in policy.ValidAudiences)
            {
                sb.Append($"{audience} ");
            }
            sb.Append("ValidAppIds: ");
            foreach (var appId in policy.ValidApplicationIds)
            {
                sb.Append($"{appId} ");
            }
            sb.Append("ValidIssuerPrefixes: ");
            sb.Append($"{policy.CommonIssuerPrefix} ");
            return sb.ToString();
        }

        private string FormatJwtOutboundPolicyConfig(IJwtOutboundPolicyConfig config)
        {
            return $"Authority: {config.Authority}, AppId: {config.AppId}, Resource: {config.Resource}";
        }

        private string FormatJwtOutboundPolicy(IJwtOutboundPolicy policy)
        {
            return $"Authority: {policy.Authority}, AppId: {policy.AppId}, Resource: {policy.Resource} TokenEndpoint: {policy.TokenEndpoint}";
        }

        private static HttpRequestBase CreateHttpRequestBaseWithAuthHeaderValue(AuthenticationHeaderValue authenticationHeaderValue)
        {
            // This is a workaround to set the authorization header on the S2SContext because we do not have HttpContext.Current in non-IIS hosted applications.
            // And, there is no way to convert between HttpRequestMessage to HttpRequest.
            // So to workaround this, this creates an instance of HttpRequest and sets the authorization header on it directly.
            // But since the header collection is read-only, this changes it so it is not read only in order to set the auth header in the collection.

            // We don't need anything on the HttpRequest, other than it being a container for the auth header.
            var httpRequest = new HttpRequest(string.Empty, "https://www.microsoft.com", string.Empty);
            Type headerType = httpRequest.Headers.GetType();
            PropertyInfo isReadOnlyProperty = headerType.GetProperty("IsReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo setHeader = headerType.GetMethod("SynchronizeHeader", BindingFlags.NonPublic | BindingFlags.Instance);
            bool readOnlyOriginal = isReadOnlyProperty != null && (bool)isReadOnlyProperty.GetValue(httpRequest.Headers);

            // Change the property so it can be written to
            if (isReadOnlyProperty != null)
            {
                isReadOnlyProperty.SetValue(httpRequest.Headers, false);
            }

            // Add the auth header to the HttpRequest
            setHeader?.Invoke(httpRequest.Headers, new object[] { HttpRequestHeader.Authorization.ToString(), authenticationHeaderValue.ToString() });

            if (isReadOnlyProperty != null)
            {
                isReadOnlyProperty.SetValue(httpRequest.Headers, readOnlyOriginal);
            }

            HttpRequestBase httpRequestBase = new HttpContextWrapper(new HttpContext(httpRequest, new System.Web.HttpResponse(new StringWriter()))).Request;
            return httpRequestBase;
        }

        private static JwtInboundPolicy CreateInboundPolicy(IAadTokenAuthConfiguration config)
        {
            var jwtInboundPolicy = new JwtInboundPolicy(config.JwtInboundPolicyConfig.Authority, config.AadAppId)
            {
                ApplyPolicyForAllTenants = config.JwtInboundPolicyConfig.ApplyPolicyForAllTenants,
                Authority = config.JwtInboundPolicyConfig.Authority,
                ClientId = config.AadAppId
            };

            foreach (string audience in config.JwtInboundPolicyConfig.Audiences)
            {
                jwtInboundPolicy.ValidAudiences.Add(audience);
            }

            jwtInboundPolicy.TokenValidationParameters.ValidIssuers = config.JwtInboundPolicyConfig.IssuerPrefixes.ToArray();


            return jwtInboundPolicy;
        }

        internal static bool TryParseAccessToken(string authorizationHeaderValue, out string accessToken)
        {
            accessToken = null;
            string[] tokens = authorizationHeaderValue.Split(PftSeparator);
            if (tokens.Length != 3)
                return false;

            string accessTokenPiece = tokens[1];
            int indexOfBearer = accessTokenPiece.IndexOf(PftBearer, StringComparison.OrdinalIgnoreCase);
            if (indexOfBearer < 0)
                return false;

            try
            {
                string accessTokenWithQuotes = accessTokenPiece.Substring(indexOfBearer + 1);
                accessToken = accessTokenWithQuotes.Substring(PftBearer.Length, accessTokenWithQuotes.Length - PftBearer.Length - 1);
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }

            return true;
        }

        internal static bool TryParseAuthorizationHeader(string authorizationHeaderValue, string scheme, out string jwt)
        {
            jwt = null;
            int indexOfScheme = authorizationHeaderValue.IndexOf(scheme, StringComparison.OrdinalIgnoreCase);

            if (indexOfScheme < 0)
                return false;

            jwt = authorizationHeaderValue.Substring(indexOfScheme + scheme.Length + 1);
            return true;
        }
    }
}
