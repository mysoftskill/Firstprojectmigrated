// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication
{
    using System;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Configuration;

    /// <summary>
    ///     A public interface to validate AAD Protected-Forwarded-Tokens (PFTs) are valid
    /// </summary>
    public interface IAadAuthManager
    {
        /// <summary>
        ///     The aad login endpoint
        /// </summary>
        string AadLoginEndpoint { get; }

        /// <summary>
        ///     The STS Authority endpoint
        /// </summary>
        string StsAuthorityEndpoint { get; }

        /// <summary>
        ///     Get access token.
        /// </summary>
        /// <param name="resourceId">The AAD application ID of the resource.</param>
        /// <returns>An access token.</returns>
        Task<string> GetAccessTokenAsync(string resourceId);

        /// <summary>
        ///     Sets the Authorization header with the AAD App token.
        ///     The added header format is: Authorization: Bearer [B]
        ///     where B is the AAD App token
        /// </summary>
        /// <remarks>Auth header format is: Bearer {appToken}</remarks>
        /// <param name="httpRequestHeaders">Http request headers to modify</param>
        /// <param name="jwtOutboundPolicy">The outbound policy for the app token</param>
        /// <param name="hasPreVerifier">Boolean that determines if the AAD RVS request has a pre-verifier</param>
        /// <param name="useAadRvsAppId">Temporarily set useAadRvsAppId to true for RVS </param>
        Task SetAuthorizationHeaderAppTokenAsync(HttpRequestHeaders httpRequestHeaders, IJwtOutboundPolicy jwtOutboundPolicy, bool hasPreVerifier = false, bool useAadRvsAppId = false);

        /// <summary>
        ///     Sets the Authorization header with the AAD App token based on a predefined outbound policy.
        ///     The added header format is: Authorization: Bearer [B]
        ///     where B is the AAD App token.
        /// </summary>
        /// <remarks>Auth header format is: Bearer {appToken}</remarks>
        /// <param name="httpRequestHeaders">Http request headers to modify</param>
        /// <param name="outboundPolicyName">The outbound policy for the app token</param>
        /// <param name="hasPreVerifier">Boolean that determines if the AAD RVS request has a pre-verifier</param>
        /// <param name="useAadRvsAppId">Temporarily set useAadRvsAppId to true for RVS </param>
        Task SetAuthorizationHeaderAppTokenAsync(HttpRequestHeaders httpRequestHeaders, OutboundPolicyName outboundPolicyName, bool hasPreVerifier = false, bool useAadRvsAppId = false);

        /// <summary>
        ///     Sets the authorization header with the specified access token.
        ///     The added header format is: Authorization: MSAuth1.0 actortoken="Bearer [B]", accesstoken="Bearer [A’]", type="PFAT"
        ///     where A' is the provided access token and B is the AAD App token:
        /// </summary>
        /// <param name="httpRequestHeaders">Http request headers to modify</param>
        /// <param name="accessToken">The access token (aka PFT) containing user credentials.</param>
        /// <param name="outboundPolicyName">The outbound policy name for the app token</param>
        /// <param name="useAadRvsAppId">Temporarily set useAadRvsAppId to true for RVS </param>
        Task SetAuthorizationHeaderProtectedForwardedTokenAsync(HttpRequestHeaders httpRequestHeaders, string accessToken, OutboundPolicyName outboundPolicyName, bool useAadRvsAppId = false);

        /// <summary>
        ///     Sets the authorization header with the specified access token.
        ///     The added header format is: Authorization: MSAuth1.0 actortoken="Bearer [B]", accesstoken="Bearer [A’]", type="PFAT"
        ///     where A' is the provided access token and B is the AAD App token:
        /// </summary>
        /// <param name="httpRequestHeaders">Http request headers to modify</param>
        /// <param name="accessToken">The access token (aka PFT) containing user credentials.</param>
        /// <param name="jwtOutboundPolicy">The outbound policy for the app token</param>
        /// <param name="useAadRvsAppId">Temporarily set useAadRvsAppId to true for RVS </param>
        Task SetAuthorizationHeaderProtectedForwardedTokenAsync(HttpRequestHeaders httpRequestHeaders, string accessToken, IJwtOutboundPolicy jwtOutboundPolicy, bool useAadRvsAppId = false);

        /// <summary>
        ///     Validate the inbound JWT async
        /// </summary>
        /// <param name="inboundJwt">The inbound JWT async</param>
        /// <returns>A <see cref="AadS2SAuthResult" /> contains information about the result of the validation.</returns>
        Task<IAadS2SAuthResult> ValidateInboundJwtAsync(string inboundJwt);

        /// <summary>
        ///     Validate the inbound PFT async
        /// </summary>
        /// <param name="authenticationHeaderValue">The auth header value</param>
        /// <param name="activityId">The activity id</param>
        /// <returns>A <see cref="AadS2SAuthResult" /> contains information about the result of the validation.</returns>
        /// 
        Task<IAadS2SAuthResult> ValidateInboundPftAsync(AuthenticationHeaderValue authenticationHeaderValue, Guid activityId);

        /// <summary>
        /// Checks to see if tenant id is valid.
        /// </summary>
        /// <param name="tenantId">The tenant ID of the resource.</param>
        /// <returns>An access token.</returns>
        Task<bool> IsTenantIdValidAsync(string tenantId);

    }
}
