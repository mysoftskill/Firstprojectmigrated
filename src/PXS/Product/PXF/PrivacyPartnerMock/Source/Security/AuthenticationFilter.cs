// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyMockService.Security
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Net.Http;
    using System.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http.Filters;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.Windows.Services.AuthN.Server;

    /// <summary>
    ///     Authenticates site and user before allowing the controller methods to be called
    /// </summary>
    public sealed class AuthenticationFilter : IAuthenticationFilter
    {
        public const string S2STokenHeader = "X-S2S-Access-Token";

        private const string DDSProxyTokenHeader = "X-S2S-Proxy-Token";

        private const string DDSS2STokenHeader = "X-S2S-Token";

        private const string S2SAppSiteName = "s2sapp.pxs.api.account.microsoft.com";

        private const string S2SUserSiteName = "s2suser.pxs.api.account.microsoft.com";

        private const string SocialAccessorToken = "X-User-Token";

        public readonly string[] FamilyTicketHeader =
        {
            // Header value for requests to PXS
            "X-Family-Json-Web-Token",

            // Header value for requests to CM
            "x-ms-jwt"
        };

        private readonly IRpsAuthServer authServer;

        public bool AllowMultiple
        {
            get { return false; }
        }

        public AuthenticationFilter(IRpsAuthServer authServer)
        {
            this.authServer = authServer;
        }

        /// <summary>
        ///     Authenticate the site and user
        /// </summary>
        /// <param name="context">Http auth context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        public Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            var identity = new MsaSelfIdentity();
            HttpRequestMessage request = context.Request;

            // skip authentication if it's for the AAD login mock
            if (request.RequestUri.LocalPath.StartsWith("/aadtoken/login"))
            {
                return Task.CompletedTask;
            }

            // mock authenticate of customer master
            if (request.RequestUri.LocalPath.StartsWith("/JarvisCM"))
            {
                this.AuthenticateMockCustomerMaster(context, identity);
                return Task.CompletedTask;
            }

            // Don't require auth for aqs mock
            if (request.RequestUri.LocalPath.StartsWith("/aqs"))
            {
                return Task.CompletedTask;
            }

            // skip authenticate of xbox accounts
            Regex regex = new Regex(@"users/puid\(\d+\)/lookup");
            var localPath = request.RequestUri.LocalPath.TrimEnd('/').TrimStart('/');
            var xboxRoutes = new List<string>
            {
                "service/authenticate",
                "user/authenticate",
                "xsts/authorize",
                "users/puid/lookup"
            };

            if (xboxRoutes.Any(r => localPath.StartsWith(r)) || regex.IsMatch(localPath))
            {
                this.AuthenticateMockXboxAccounts(context, identity);
                return Task.CompletedTask;
            }

            // mock authenticate DDS
            if (request.RequestUri.LocalPath.StartsWith("/DeviceStore/self/Users"))
            {
                this.AuthenticateMockDDS(context, identity);
                return Task.CompletedTask;
            }

            // Continue on as if the request is a normal PXF request.

            // Authenticate the site using the S2STokenHeader value
            string accessToken = null;
            long applicationId = 0;
            RpsPropertyBag propertyBag = new RpsPropertyBag();
            if (request.Headers.Contains(S2STokenHeader))
            {
                // Validate certificate is in the request.
                X509Certificate2 certificate = request.GetClientCertificate();
                if (certificate == null)
                {
                    throw new SecurityException("Client certificate is required.");
                }

                accessToken = request.Headers.GetValues(S2STokenHeader).FirstOrDefault();
                applicationId = this.ValidateAccessToken(accessToken, certificate, propertyBag);
            }

            // Authenticate the user proxy ticket or aad pop token
            if (request.Headers.Authorization != null)
            {
                try
                {
                    // The AadPopTokenProvider creates tokens in this format. See AaadTokenProvider.cs
                    if (request.Headers.Authorization.Scheme.Equals("MSAuth1.0", StringComparison.OrdinalIgnoreCase))
                    {
                        string authParam = request.Headers.Authorization.Parameter;

                        const string TokenPrefix = "popToken=\"";
                        var remainingValue = authParam.Remove(authParam.IndexOf(TokenPrefix, StringComparison.OrdinalIgnoreCase), TokenPrefix.Length);
                        var tokenValue = remainingValue.Remove(remainingValue.IndexOf("\"", StringComparison.OrdinalIgnoreCase));
                        var token = new JwtSecurityToken(tokenValue);

                        JwtSecurityToken aToken = new JwtSecurityToken(
                            token.Claims.First(o => o.Type.Equals("aat", StringComparison.OrdinalIgnoreCase)).Value);
                        string puidValue = aToken.Claims.First(o => o.Type.Equals("puid", StringComparison.OrdinalIgnoreCase)).Value;
                        identity.TargetPuid = long.Parse(puidValue, NumberStyles.AllowHexSpecifier);
                        context.Principal = new CallerPrincipal(identity);
                        return Task.CompletedTask;
                    }
                }
                catch (Exception e)
                {
                    throw new SecurityException($"Cannot authenticate request scheme 'MSAuth1.0'. Exception: {e}");
                }

                if (request.Headers.Authorization.Scheme != "msa")
                {
                    throw new SecurityException($"Authentication scheme must be 'msa'. Scheme is: {request.Headers?.Authorization?.Scheme ?? "NULL"}");
                }

                var proxyTicket = request.Headers.Authorization.Parameter;

                long puid;
                long cid;
                this.ValidateProxyTicket(proxyTicket, propertyBag, out puid, out cid);

                identity.SiteId = applicationId;
                identity.AuthorizingPuid = puid;
                identity.TargetPuid = puid;
                identity.Name = puid.ToString(CultureInfo.InvariantCulture);
                identity.IsAuthenticated = true;
            }

            string familyHeader = this.FamilyTicketHeader.FirstOrDefault(header => request.Headers.Contains(header));
            if (!string.IsNullOrEmpty(familyHeader))
            {
                identity.TargetPuid = null;
                string familyToken = request.Headers.GetValues(familyHeader).FirstOrDefault();
                if (!FamilyClaims.TryParse(familyToken, out FamilyClaims familyClaims))
                {
                    throw new SecurityException("Family token could not be parsed.");
                }

                if (!familyClaims.CheckIsValid())
                {
                    throw new SecurityException("Family token could not be parsed.");
                }

                if (!familyClaims.ParentChildRelationshipIsClaimed(identity.AuthorizingPuid.Value, familyClaims.TargetPuid.Value))
                {
                    throw new SecurityException("Not a claimed parent-child relationship.");
                }

                identity.TargetPuid = familyClaims.TargetPuid;
            }

            context.Principal = new CallerPrincipal(identity);
            return Task.CompletedTask;
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void AuthenticateMockCustomerMaster(HttpAuthenticationContext context, MsaSelfIdentity identity)
        {
            // Validate certificate is in the request.
            X509Certificate2 certificate = context.Request.GetClientCertificate();
            if (certificate == null)
            {
                throw new SecurityException("Client certificate is required.");
            }

            if (context.Request.Headers.Authorization != null)
            {
                if (context.Request.Headers.Authorization.Scheme != "MSAAuth1.0")
                {
                    throw new SecurityException("Authentication scheme must be 'MSAAuth1.0'");
                }

                string authHeader = context.Request.Headers.Authorization.Parameter;

                // Authenticate the site
                string accessToken = string.Empty;
                long applicationId = 0;
                accessToken = authHeader?.Split(',')[0]?.Split(new[] { '=' }, 2)[1].Trim('"');
                RpsPropertyBag propertyBag = new RpsPropertyBag();
                applicationId = this.ValidateAccessToken(accessToken, certificate, propertyBag);

                // Authenticate the user proxy ticket
                var proxyTicket = authHeader?.Split(',')[1]?.Split(new[] { '=' }, 2)[1].Trim('"');
                long puid;
                long cid;
                this.ValidateProxyTicket(proxyTicket, propertyBag, out puid, out cid);

                identity.SiteId = applicationId;
                identity.AuthorizingPuid = puid;
                identity.TargetPuid = puid;
                identity.Name = puid.ToString(CultureInfo.InvariantCulture);
                identity.IsAuthenticated = true;

                context.Principal = new CallerPrincipal(identity);
            }

            // Check for family claims
            HttpRequestMessage request = context.Request;
            string familyHeader = this.FamilyTicketHeader.FirstOrDefault(header => request.Headers.Contains(header));
            if (!string.IsNullOrEmpty(familyHeader))
            {
                identity.TargetPuid = null;
                string familyToken = request.Headers.GetValues(familyHeader).FirstOrDefault();
                if (!FamilyClaims.TryParse(familyToken, out FamilyClaims familyClaims))
                {
                    throw new SecurityException("Family token could not be parsed.");
                }

                if (!familyClaims.CheckIsValid())
                {
                    throw new SecurityException("Family token could not be parsed.");
                }

                if (!familyClaims.ParentChildRelationshipIsClaimed(identity.AuthorizingPuid.Value, familyClaims.TargetPuid.Value))
                {
                    throw new SecurityException("Not a claimed parent-child relationship.");
                }

                identity.TargetPuid = familyClaims.TargetPuid;
            }
        }

        private void AuthenticateMockDDS(HttpAuthenticationContext context, MsaSelfIdentity identity)
        {
            // Validate certificate is in the request.
            X509Certificate2 certificate = context.Request.GetClientCertificate();
            if (certificate == null)
            {
                throw new SecurityException("Client certificate is required.");
            }

            // Authenticate the site using the S2STokenHeader value
            string accessToken = null;
            long applicationId = 0;
            RpsPropertyBag propertyBag = new RpsPropertyBag();
            accessToken = context.Request.Headers.GetValues(DDSS2STokenHeader).FirstOrDefault();
            applicationId = this.ValidateAccessToken(accessToken, certificate, propertyBag);

            var proxyTicket = context.Request.Headers.GetValues(DDSProxyTokenHeader).FirstOrDefault();

            long puid;
            long cid;
            this.ValidateProxyTicket(proxyTicket, propertyBag, out puid, out cid);

            identity.SiteId = applicationId;
            identity.AuthorizingPuid = puid;
            identity.TargetPuid = puid;
            identity.Name = puid.ToString(CultureInfo.InvariantCulture);
            identity.IsAuthenticated = true;

            context.Principal = new CallerPrincipal(identity);
        }

        private void AuthenticateMockXboxAccounts(HttpAuthenticationContext context, MsaSelfIdentity identity)
        {
            // Validate certificate is in the request.
            X509Certificate2 certificate = context.Request.GetClientCertificate();
            if (certificate == null)
            {
                throw new SecurityException("Client certificate is required.");
            }
        }

        private long ValidateAccessToken(string accessToken, X509Certificate2 certificate, RpsPropertyBag propertyBag)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new SecurityException("Access token null or empty");
            }

            using (RpsAuthResult result = this.authServer.GetS2SSiteAuthResult(S2SAppSiteName, accessToken, certificate.RawData, propertyBag))
            {
                if (result == null)
                {
                    throw new SecurityException("RpsAuthResult was null in trying to open accessToken.");
                }

                if (result.AppId == null)
                {
                    throw new SecurityException("RpsAuthResult contained a null AppId.");
                }

                return result.AppId.Value;
            }
        }

        private void ValidateProxyTicket(string proxyTicket, RpsPropertyBag propertyBag, out long puid, out long cid)
        {
            if (string.IsNullOrWhiteSpace(proxyTicket))
            {
                throw new SecurityException("Proxy ticket null or empty");
            }

            using (var result = this.authServer.GetAuthResult(S2SUserSiteName, proxyTicket, RpsTicketType.Proxy, propertyBag))
            {
                if (!result.MemberId.HasValue)
                {
                    throw new SecurityException("MemberId not found in proxy ticket");
                }

                if (!result.Cid.HasValue)
                {
                    throw new SecurityException("Cid not found in proxy ticket");
                }

                puid = result.MemberId.Value;
                cid = result.Cid.Value;
            }
        }
    }
}
