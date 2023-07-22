// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication
{
    using Microsoft.IdentityModel.S2S;
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Security.Claims;

    /// <summary>
    ///     AadS2SAuthResult
    /// </summary>
    public class AadS2SAuthResult : IAadS2SAuthResult
    {
        /// <summary>
        ///     The claim type.
        /// </summary>
        private const string AuthorizationTokenClaimTypeWellKnownIds = "wids";

        /// <summary>
        ///     The expectid wid claim value for privacy operation authorized users.
        /// </summary>
        private static Guid AuthorizationTokenValueWellKnownIdTenantAdministrator = Guid.Parse("62e90394-69f5-4237-9190-012177145e10");

        /// <inheritdoc />
        public string AccessToken { get; }

        /// <inheritdoc />
        public string AppDisplayName { get; }

        /// <inheritdoc />
        public IReadOnlyList<string> DiagnosticLogs { get; }

        /// <inheritdoc />
        public Exception Exception { get; }

        /// <summary>
        ///     The Inbound App Id
        /// </summary>
        public string InboundAppId { get; }

        /// <inheritdoc />
        public Guid ObjectId { get; }

        /// <inheritdoc />
        public string SubjectTicket { get; }

        /// <inheritdoc />
        public bool Succeeded { get; }

        /// <inheritdoc />
        public Guid TenantId { get; }

        /// <inheritdoc />
        public string UserPrincipalName { get; }

        /// <summary>
        ///     Creates a new instance of the <see cref="AadS2SAuthResult" />
        /// </summary>
        /// <param name="s2sResult">the s2s result</param>
        public AadS2SAuthResult(S2SAuthenticationResult s2sResult)
            : this(s2sResult, null)
        {
        }

        /// <summary>
        ///     Creates a new instance of the <see cref="AadS2SAuthResult" />
        /// </summary>
        /// <param name="s2sResult">the s2s result</param>
        /// <param name="accessTokenString">the access token</param>
        public AadS2SAuthResult(S2SAuthenticationResult s2sResult, string accessTokenString)
        {
            if (s2sResult != null)
            {
                JwtSecurityToken accessToken = null;
                bool canReadAccessToken = new JwtSecurityTokenHandler().CanReadToken(accessTokenString);
                if (canReadAccessToken)
                {
                    // Note: there is an accessTokenValidation result in s2sResult.Token, but 
                    // that comes up null. So, we get the claims directly from the access token string
                    // instead.
                    accessToken = new JwtSecurityToken(accessTokenString);
                }

                this.Succeeded = s2sResult.Succeeded;
                this.Exception = s2sResult.Exception;
                this.DiagnosticLogs = new List<string> {s2sResult.AuthenticationFailureDescription};
                this.InboundAppId = s2sResult?.Ticket?.ApplicationIdentity?.Claims?.FirstOrDefault(c => c != null && c.Type == "appid")?.Value;
                this.AccessToken = accessTokenString;

                if (s2sResult.Ticket?.SubjectToken != null)
                {
                    this.SubjectTicket = (s2sResult.Ticket.SubjectToken as JwtSecurityToken)?.RawData;
                }

                // The tenant id in the access token is the tenant of the user who initiated the request
                var tenantId = accessToken?.Claims?.FirstOrDefault(c => c != null && c.Type == "tid")?.Value;

                if (!string.IsNullOrWhiteSpace(tenantId) && Guid.TryParse(tenantId, out Guid tentantIdValue))
                {
                    this.TenantId = tentantIdValue;
                }

                // The object id in the access token is from the user who initiated the request
                // In our case, this would be the tenant admin.
                var objectid = accessToken?.Claims?.FirstOrDefault(c => c != null && c.Type == "oid")?.Value;

                if (!string.IsNullOrWhiteSpace(objectid) && Guid.TryParse(objectid, out Guid objectidValue))
                {
                    this.ObjectId = objectidValue;
                }

                // Example: Graph explorer
                this.AppDisplayName = accessToken?.Claims?.FirstOrDefault(c => c != null && c.Type == "app_displayname")?.Value;

                // Example: username@microsoft.com
                this.UserPrincipalName = accessToken?.Claims?.FirstOrDefault(c => c != null && c.Type == "upn")?.Value;
            }
            else
            {
                this.Exception = new ArgumentNullException(nameof(s2sResult), "S2S Result was null.");
            }
        }

        /// <summary>
        /// Used for Mise token validation
        /// </summary>
        /// <param name="s2sResult"></param>
        /// <param name="claims"></param>
        /// <param name="accessTokenString"></param>
        public AadS2SAuthResult(S2SAuthenticationResult s2sResult, ClaimsPrincipal claims, string accessTokenString)
        {
            if (s2sResult != null)
            {
                this.Succeeded = s2sResult.Succeeded;
                this.Exception = s2sResult.Exception;
                this.DiagnosticLogs = new List<string> { s2sResult.AuthenticationFailureDescription };
                this.InboundAppId = s2sResult?.Ticket?.ApplicationIdentity?.Claims?.FirstOrDefault(c => c != null && c.Type == "appid")?.Value;
                this.AccessToken = accessTokenString;

                if (s2sResult.Ticket?.SubjectToken != null)
                {
                    this.SubjectTicket = s2sResult.Ticket.SubjectToken?.ToString();
                }

                // The tenant id in the access token is the tenant of the user who initiated the request
                var tenantId = claims?.FindFirst("tid")?.Value;//accessToken?.Claims?.FirstOrDefault(c => c != null && c.Type == "tid")?.Value;

                if (!string.IsNullOrWhiteSpace(tenantId) && Guid.TryParse(tenantId, out Guid tentantIdValue))
                {
                    this.TenantId = tentantIdValue;
                }

                // The object id in the access token is from the user who initiated the request
                // In our case, this would be the tenant admin.
                var objectid = claims?.FindFirst("oid")?.Value;

                if (!string.IsNullOrWhiteSpace(objectid) && Guid.TryParse(objectid, out Guid objectidValue))
                {
                    this.ObjectId = objectidValue;
                }

                // Example: Graph explorer
                this.AppDisplayName = claims?.FindFirst("app_displayname")?.Value;

                // Example: username@microsoft.com
                this.UserPrincipalName = claims?.FindFirst("upn")?.Value;
            }
            else
            {
                this.Exception = new ArgumentNullException(nameof(s2sResult), "S2S Result was null.");
            }
        }

        /// <summary>
        ///     Creates a new instance of the <see cref="AadS2SAuthResult" />
        /// </summary>
        /// <param name="exception">The exception.</param>
        public AadS2SAuthResult(Exception exception)
        {
            this.Exception = exception;
            this.Succeeded = false;
        }

        /// <summary>
        ///     Creates a new instance of the <see cref="AadS2SAuthResult" />
        /// </summary>
        /// <param name="objectId">The objectId.</param>
        /// <param name="tenantId">The tenantId.</param>
        /// <param name="appId">The appId.</param>
        public AadS2SAuthResult(Guid objectId, Guid tenantId, string appId)
        {
            this.ObjectId = objectId;
            this.TenantId = tenantId;
            this.InboundAppId = appId;
            this.Succeeded = true;
        }
    }
}
