// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.ProfileIdentityService;

    /// <summary>
    ///     Interface for MsaIdentityService Adapter. This is aka IDSAPI.
    ///     IDSAPI (or SAPI for short) is the SOAP interface to MSA's identity data.
    /// </summary>
    /// <remarks>
    ///     Partner API documentation available @
    ///     <c>https://microsoft.sharepoint.com/teams/liveid/docs/idsapi/webframe.html</c> and
    ///     <c>https://identitydocs.azurewebsites.net/static/msa/idsapi.html</c>
    /// </remarks>
    public interface IMsaIdentityServiceAdapter
    {
        /// <summary>
        ///     Gets a GDPR verifier token for account close
        /// </summary>
        /// <param name="commandId">The command identifier. Unique per request, and generated by PXS.</param>
        /// <param name="puid">The target puid</param>
        /// <param name="preVerifierToken">The preverifier token, this must originate from MSA AQS</param>
        /// <param name="xuid">(Optional)The xbox user id (xuid) of the user</param>
        /// <returns>A GDPR verifier token</returns>
        Task<AdapterResponse<string>> GetGdprAccountCloseVerifierAsync(Guid commandId, long puid, string preVerifierToken, string xuid = "");

        /// <summary>
        ///     Gets a GDPR verifier token for device delete. In this API, a user is not authenticated so no user context is sent to MSA.
        /// </summary>
        /// <param name="commandId">The command identifier. Unique per request, and generated by PXS.</param>
        /// <param name="globalDeviceId">The target global device id</param>
        /// <param name="predicate">(Optional)The predicate</param>
        /// <returns>A GDPR verifier token</returns>
        Task<AdapterResponse<string>> GetGdprDeviceDeleteVerifierAsync(Guid commandId, long globalDeviceId, string predicate = "");

        /// <summary>
        ///     Gets a GDPR verifier token for export
        /// </summary>
        /// <param name="commandId">The command identifier. Unique per request, and generated by PXS.</param>
        /// <param name="requestContext">The user request context</param>
        /// <param name="storageDestination">The destination to write to.</param>
        /// <param name="xuid">The xbox user id (xuid) of the user</param>
        /// <returns>A GDPR verifier token</returns>
        Task<AdapterResponse<string>> GetGdprExportVerifierAsync(Guid commandId, IPxfRequestContext requestContext, Uri storageDestination, string xuid);

        /// <summary>
        ///     Gets a GDPR verifier token for user delete
        /// </summary>
        /// <param name="commandIds">The list of command identifiers (generated by PXS). This allows for a verifier token to contain multiple command id's for the same user subject.</param>
        /// <param name="requestContext">The user request context</param>
        /// <param name="xuid">(Optional)The xbox user id (xuid) of the user</param>
        /// <param name="predicate">(Optional)The predicate</param>
        /// <param name="dataType">(Optional)The datatype</param>
        /// <returns>A GDPR verifier token</returns>
        Task<AdapterResponse<string>> GetGdprUserDeleteVerifierAsync(
            IList<Guid> commandIds,
            IPxfRequestContext requestContext,
            string xuid = "",
            string predicate = "",
            string dataType = "");

        /// <summary>
        ///     Gets a GDPR verifier token for a user delete command with preverifier.
        /// </summary>
        /// <param name="commandId">The command identifier generated by PXS</param>
        /// <param name="requestContext">The user request context</param>
        /// <param name="preVerifier">PreVerifier token</param>
        /// <param name="xuid">(Optional)The xbox user id (xuid) of the user</param>
        /// <param name="predicate">(Optional)The predicate</param>
        /// <param name="dataType">(Optional)The datatype</param>
        /// <returns>A GDPR verifier token</returns>
        Task<Models.AdapterResponse<string>> GetGdprUserDeleteVerifierWithPreverifierAsync(
            Guid commandId,
            IPxfRequestContext requestContext,
            string preVerifier,
            string xuid = "",
            string predicate = "",
            string dataType = "");

        /// <summary>
        ///     Gets a GDPR verifier token for user delete with refresh claim.
        /// </summary>
        /// <param name="requestContext">The user request context</param>
        /// <returns>A GDPR verifier token</returns>
        Task<AdapterResponse<string>> GetGdprUserDeleteVerifierWithRefreshClaimAsync(IPxfRequestContext requestContext);
        
        /// <summary>
        ///     Renews GDPR verifier token for user delete using existing preverifier obtained from MSA RVS before.
        /// </summary>
        /// <param name="requestContext">The user request context</param>
        /// <param name="preVerifier">preverifier</param>
        /// <returns>A GDPR verifier token</returns>
        Task<AdapterResponse<string>> RenewGdprUserDeleteVerifierUsingPreverifierAsync(IPxfRequestContext requestContext, string preVerifier);

        /// <summary>
        ///     Gets the profile attributes for the given request context
        /// </summary>
        /// <param name="requestContext">The user request context, used for determining OBO or self</param>
        /// <param name="attributes">The attributes to retrieve</param>
        /// <returns>A dictionary containing the attributes for the user</returns>
        Task<AdapterResponse<IProfileAttributesUserData>> GetProfileAttributesAsync(IPxfRequestContext requestContext, params ProfileAttribute[] attributes);

        /// <summary>
        ///     Gets the user's sign in name information.
        /// </summary>
        /// <param name="puid">The puids.</param>
        /// <returns>The user's sign in name information</returns>
        Task<AdapterResponse<ISigninNameInformation>> GetSigninNameInformationAsync(long puid);

        /// <summary>
        ///     Gets the users' sign in name information.
        /// </summary>
        /// <param name="puids">The puids.</param>
        /// <returns>The users' sign in name information</returns>
        Task<AdapterResponse<IEnumerable<ISigninNameInformation>>> GetSigninNameInformationsAsync(IEnumerable<long> puids);
    }
}
