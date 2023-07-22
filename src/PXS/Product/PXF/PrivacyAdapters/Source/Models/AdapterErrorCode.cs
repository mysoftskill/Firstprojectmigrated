// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Models
{
    /// <summary>
    ///     Adapter-Error-Code
    /// </summary>
    public enum AdapterErrorCode
    {
        Unknown,

        PdpSearchHistoryBotDetection,

        JsonDeserializationFailure,

        EmptyResponse,

        InvalidInput,

        ConcurrencyConflict,

        Unauthorized,

        BadRequest,

        ResourceNotModified,

        ResourceAlreadyExists,

        MsaCallerNotAuthorized,

        MsaUserNotAuthorized,

        PartnerDisabled,

        NullVerifier,

        NullVerifierV3,

        UnexpectedVerifier,

        TimeWindowExpired,

        TooManyRequests,

        Forbidden,

        ResourceNotFound,

        MethodNotAllowed
    }
}
