// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.Common.Azure;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    ///     Generates AAD PoP tokens
    /// </summary>
    public class AadTokenProvider : IAadTokenProvider
    {
        private const string MsaPtClaimKey = "msa_pt";
        private const string Sha256HashAlgorithm = "SHA256";

        // The header value doesn't change. 
        private static readonly Lazy<string> popHeader = new Lazy<string>(
            mode: LazyThreadSafetyMode.PublicationOnly,
            valueFactory: () => Base64UrlEncoder.Encode(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(
                        new Dictionary<string, string> { { "type", "JWT" }, { "alg", "RS256" }, { "kid", "1" } },
                        Formatting.None))));

        private readonly IAadTokenAuthConfiguration config;
        private readonly IClock clock;
        private readonly ILogger logger;
        private readonly IMemoryCache appAccessTokenCache;
        private readonly Lazy<IHttpClient> client;
        private readonly Lazy<X509Certificate2> signingCert;
        private readonly Lazy<RSA> csp;
        private readonly TimeSpan tokenTtl;
        private readonly TimeSpan maxCacheItemTtl;
        private readonly IAppConfiguration appConfiguration;

        /// <summary>
        ///     Initializes a new instance of the AadTokenProvider class
        /// </summary>
        /// <param name="pxsConfig">PXS configuration</param>
        /// <param name="certProvider">Certificate provider</param>
        /// <param name="httpClientFactory">HTTP client factory</param>
        /// <param name="counterFactory">Counter factory</param>
        /// <param name="logger">Geneva trace logger</param>
        /// <param name="clock">Time provider</param>
        /// <param name="memoryCache">Memory cache</param>
        /// <param name="appConfiguration">App configuration instance</param>
        public AadTokenProvider(
            IPrivacyConfigurationManager pxsConfig,
            ICertificateProvider certProvider,
            IHttpClientFactory httpClientFactory,
            ICounterFactory counterFactory,
            ILogger logger,
            IClock clock,
            IMemoryCache memoryCache,
            IAppConfiguration appConfiguration)
        {
            ArgumentCheck.ThrowIfNull(httpClientFactory, nameof(httpClientFactory));
            ArgumentCheck.ThrowIfNull(counterFactory, nameof(counterFactory));
            ArgumentCheck.ThrowIfNull(certProvider, nameof(certProvider));
            ArgumentCheck.ThrowIfNull(pxsConfig, nameof(pxsConfig));
            ArgumentCheck.ThrowIfNull(logger, nameof(logger));
            ArgumentCheck.ThrowIfNull(clock, nameof(clock));
            ArgumentCheck.ThrowIfNull(appConfiguration, nameof(appConfiguration));

            this.config = pxsConfig.AadTokenAuthGeneratorConfiguration;
            this.logger = logger;
            this.clock = clock;
            this.appAccessTokenCache = memoryCache;

            // Use Lazy<> for this to defer the client creation until it's actually used. The advantage here is that a failure to 
            // create an http client only affects folks who actually try to use them. This is particularly useful in the 
            // case of running locally as we'd only attempt to load the cert if the local instance happens to be configured
            // to use AAD auth
            this.client = new Lazy<IHttpClient>(
                () => httpClientFactory.CreateHttpClient(this.config, counterFactory),
                LazyThreadSafetyMode.PublicationOnly);

            this.signingCert = new Lazy<X509Certificate2>(
                () => certProvider.GetClientCertificate(this.config.RequestSigningCertificateConfiguration),
                LazyThreadSafetyMode.PublicationOnly);

            // Must create a new random RSA Key for PoP
            // https://aadwiki.windows-int.net/index.php?title=Server-to-server_authentication#Proof-of-possession_key
            this.csp = new Lazy<RSA>(
                () => RSA.Create(2048),
                LazyThreadSafetyMode.PublicationOnly);

            this.tokenTtl = TimeSpan.FromSeconds(this.config.AadPopTokenAuthConfig.AadAppTokenExpirySeconds);
            this.maxCacheItemTtl = TimeSpan.FromSeconds(this.config.AadPopTokenAuthConfig.MaxCacheAgeAadAppTokenExpirySeconds);

            this.appConfiguration = appConfiguration;
        }

        /// <summary>
        ///     Generates a PoP authenticator with an AAD token for accessing the specified resource
        /// </summary>
        /// <param name="request">Data to make PoP authenticator AAD request</param>
        /// <param name="cancelToken">Cancellation token to halt the request</param>
        /// <returns>Requested authenticator</returns>
        public async Task<string> GetPopTokenAsync(AadPopTokenRequest request, CancellationToken cancelToken)
        {
            ArgumentCheck.ThrowIfNull(request, nameof(request));
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(request.Resource, nameof(request.Resource));
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(request.Scope, nameof(request.Scope));
            ArgumentCheck.ThrowIfNull(request.RequestUri, nameof(request.RequestUri));

            IDictionary<string, object> popPayload = new Dictionary<string, object>();

            string appAccessToken = await this.GetOrCreateAppAccessTokenAsync(request, cancelToken);
            popPayload["at"] = appAccessToken;

            string httpMethod = request.HttpMethod.ToString().ToUpperInvariant();
            popPayload["m"] = httpMethod;

            string path = HttpUtility.UrlDecode(request.RequestUri.AbsolutePath, Encoding.UTF8);
            popPayload["p"] = path;

            string pathSha256 = AuthUtilities.GetStringFromHash(AuthUtilities.GetSha256Hash(path));
            popPayload["p#S256"] = pathSha256;

            long timestamp = Convert.ToInt64(new TimeSpan(this.clock.UtcNow.Ticks - EpochTime.UnixEpoch.Ticks).TotalSeconds);
            popPayload["ts"] = timestamp;

            if (request.Type == AadPopTokenRequestType.AppAssertedUserToken)
            {
                JwtSecurityToken appAccessSecurityToken = new JwtSecurityToken(appAccessToken);
                JwtSecurityToken appAssertedUserSecurityToken = GenerateAppAssertedUserToken(appAccessSecurityToken, request.Resource, request.Scope, request.Claims);
                string appAssertedUserToken = new JwtSecurityTokenHandler().WriteToken(appAssertedUserSecurityToken);
                popPayload["aat"] = appAssertedUserToken;
            }
            else if (request.Type == AadPopTokenRequestType.MsaProxyTicket)
            {
                string msaPt = request.Claims[MsaPtClaimKey];
                popPayload[MsaPtClaimKey] = msaPt;
            }
            else
            {
                throw new ArgumentException("Unknown AadPopTokenRequest type: " + request.Type);
            }

            return this.GeneratePopToken(popPayload);
        }

        private string GeneratePopToken(IDictionary<string, object> popPayload)
        {
            string header = popHeader.Value;
            string body =
                Base64UrlEncoder.Encode(
                    Encoding.UTF8.GetBytes(
                        JsonConvert.SerializeObject(popPayload, Formatting.None)));

            string message = $"{header}.{body}";
            string signature = Base64UrlEncoder.Encode(this.csp.Value.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            string token = $"{message}.{signature}";
            return token;
        }

        private JwtSecurityToken GenerateAppAssertedUserToken(
            JwtSecurityToken appAccessToken,
            string resource,
            string scope,
            IDictionary<string, string> claims)
        {
            string appTid = appAccessToken.Claims.Single(a => "tid".Equals(a.Type, StringComparison.OrdinalIgnoreCase)).Value;
            string appid = appAccessToken.Claims.Single(a => "appid".Equals(a.Type, StringComparison.OrdinalIgnoreCase)).Value;

            List<Claim> fullClaims = new List<Claim>
            {
                new Claim("ver", "app_asserted_user_v1"),
                new Claim("nbf", AuthUtilities.GetTimeStampClaim("nbf", appAccessToken.Claims, 0)),
                new Claim("exp", AuthUtilities.GetTimeStampClaim("exp", appAccessToken.Claims, 1)),
                new Claim("iss", appid + "@" + appTid),
                new Claim("tid", appTid),
                new Claim("aud", resource),
                new Claim("appidacr", "1"),
                new Claim("appid", appid),
                new Claim("scp", scope),

                // the equality check produces a bool, which we then convert to a string
                // TODO: Cargo cult programming! The documentation code has it, but the sample code I looked at doesn't.
                // TODO: Need to find someone to tell me if it's needed or not.
                new Claim("clientappid", Guid.Empty.ToString("D"))
            };

            if (claims != null)
            {
                fullClaims.AddRange(claims.Select(c => new Claim(c.Key, c.Value)));
            }

            return new JwtSecurityToken(claims: fullClaims);
        }

        private async Task<string> GetOrCreateAppAccessTokenAsync(AadPopTokenRequest request, CancellationToken cancelToken)
        {
            if (!this.config.AadPopTokenAuthConfig.CacheAppTokens)
            {
                this.logger.Information(nameof(AadTokenProvider), "Caching AAD Tokens is disabled. Fetching new aad token.");
                return await CreateAadAppAccessTokenAsync(request.Resource, cancelToken).ConfigureAwait(false);
            }

            string resource = request.Resource;
            var token = await this.appAccessTokenCache.GetOrCreateAsync(resource, async entry =>
            {
                this.logger.Information(nameof(AadTokenProvider), $"Cache item expired. Refreshing access token for {resource}");

                entry.AbsoluteExpirationRelativeToNow = this.maxCacheItemTtl;
                return await CreateAadAppAccessTokenAsync(request.Resource, cancelToken).ConfigureAwait(false);
            });

            return token;
        }

        /// <summary>
        ///     Fetches the AAD app access proof-of-possession protected token as defined in:
        ///     https://aadwiki.windows-int.net/index.php?title=Server-to-server_authentication
        ///     https://docs.microsoft.com/en-us/azure/active-directory/azuread-dev/azure-ad-endpoint-comparison
        /// </summary>
        private async Task<string> CreateAadAppAccessTokenAsync(string resource, CancellationToken cancelToken)
        {
            try
            {
                this.logger.Information(
                    nameof(AadTokenProvider),
                    $"Requesting new AAD token from {GetRegionalEstsEndpoint(this.config.BaseUrl)} for AppId {this.config.AadAppId} for resource {resource}");

                Uri aadTokenEndpoint = new Uri(GetRegionalEstsEndpoint(this.config.BaseUrl));
                OutgoingApiEventWrapper apiEvent = OutgoingApiEventWrapper.CreateHttpEventWithPuid(
                    partnerId: nameof(AadTokenProvider),
                    operationName: "GetAADToken",
                    operationVersion: string.Empty,
                    requestUri: aadTokenEndpoint,
                    requestMethod: HttpMethod.Post,
                    userPuid: null);

                JToken responseBody;
                using (HttpRequestMessage request = HttpExtensions.CreateHttpRequestMessage(aadTokenEndpoint, HttpMethod.Post, apiEvent, null))
                {
                    const string BodyMediaType = "application/x-www-form-urlencoded";
                    string body = this.GenerateAadTokenRequestHttpBody(this.GenerateRsaKey(), resource);
                    request.Content = new StringContent(body, Encoding.UTF8, BodyMediaType);

                    using (HttpResponseMessage response = await this.client.Value.SendAsync(request, cancelToken).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        responseBody = JToken.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                    }
                }

                return responseBody.SelectToken("access_token").Value<string>();
            }
            catch (HttpRequestException e)
            {
                const string ErrorMessage = "An error occurred while acquiring an AAD App token.";
                var errorEvent = new ErrorEvent
                {
                    ComponentName = nameof(AadTokenProvider),
                    ErrorMethod = nameof(this.GetPopTokenAsync),
                    ErrorCode = "GetPopTokenFailure",
                    ErrorMessage = ErrorMessage,
                    ErrorDetails = e.ToString()
                };
                errorEvent.LogError();
                this.logger.Error(nameof(AadTokenProvider), e, ErrorMessage);

                throw;
            }
        }

        private string GenerateRsaKey()
        {
            RSAParameters parameters = this.csp.Value.ExportParameters(false);
            Dictionary<string, string> rsaKey = new Dictionary<string, string>
            {
                { "kty", "RSA" },
                { "alg", "RS256" },
                { "kid", "1" },
                { "n", Base64UrlEncoder.Encode(parameters.Modulus) },
                { "e", Base64UrlEncoder.Encode(parameters.Exponent) }
            };

            return JsonConvert.SerializeObject(rsaKey, Formatting.None);
        }

        private string GenerateAadTokenRequestHttpBody(string rsaKey, string resource)
        {
            var jwtHeader = new JwtHeader(new SigningCredentials(new X509SecurityKey(this.signingCert.Value), "RS256"))
            {
                // The signingCert used here is registered in firstparty portal using SNI. This requires we include the cert (x5c) in the header
                { "x5c", Convert.ToBase64String(this.signingCert.Value.Export(X509ContentType.Cert)) },
                // This is required after upgrade to System.IdentityModel.Tokens.Jwt 5.5.0
                { "x5t", Base64UrlEncoder.Encode(this.signingCert.Value.GetCertHash()) }
            };

            JwtPayload jwtPayload = new JwtPayload(
                claims: new List<Claim>
                {
                    new Claim("iss", this.config.AadAppId),
                    new Claim("aud", this.config.BaseUrl),
                    new Claim("iat", this.clock.UtcNow.AddMinutes(-1).ToUnixTimeSeconds().ToString()),
                    new Claim("exp", this.clock.UtcNow.Add(this.tokenTtl).ToUnixTimeSeconds().ToString()),
                    new Claim("pop_jwk", rsaKey)
                });

            var token = new JwtSecurityToken(jwtHeader, jwtPayload);

            return "grant_type=client_credentials&client_id={0}&resource={1}&request={2}".FormatInvariant(
                this.config.AadAppId,
                HttpUtility.UrlEncode(resource),
                new JwtSecurityTokenHandler().WriteToken(token));
        }

        /// <summary>
        /// Get the regional ESTS endpoint
        /// Ideally, this should be done via MSAL's ConfidentialClientApplicationBuilder.WithAzureRegion function, but this class has not been migrated to MSAL yet, 
        /// and we are not sure if the MSAL GetPopToken function (https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Proof-Of-Possession-%28PoP%29-tokens) 
        /// really works with our partners. So, to make a low risk fix, borrowed some code from MSAL library
        /// (https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/blob/ed990cefe7c19dbc84726840bc55d46be6ed6ae1/src/client/Microsoft.Identity.Client/Instance/Discovery/RegionDiscoveryProvider.cs#L56)
        /// In the future, this code should be replaced by MSAL call.
        /// </summary>
        /// <param name="baseUrl">The base aad endpoint Url</param>
        /// <returns>The regional aad enpoint or original url if azure region is not set or the feature flag is turned off</returns>
        private string GetRegionalEstsEndpoint(string baseUrl)
        {
            // Note: this is the setting for Public cloud. Sovereign clouds have different settings.
            const string PublicEnvForRegional = "r.login.microsoftonline.com";

            // The expectation is this should give us the short azure region name (https://aka.ms/region-map)
            var region = Environment.GetEnvironmentVariable("MONITORING_DATACENTER");
            if (appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PXS.EnableEstsrForPopToken).GetAwaiter().GetResult() && 
                !string.IsNullOrEmpty(region))
            {
                var newUri = new UriBuilder(baseUrl);
                newUri.Host = $"{region}.{PublicEnvForRegional}";
                return newUri.ToString();
            }
            else
            {
                return baseUrl;
            }
        }
    }
}
