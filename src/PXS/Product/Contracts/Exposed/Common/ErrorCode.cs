//--------------------------------------------------------------------------------
// <copyright file="ErrorCode.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Contracts.Exposed
{
    /// <summary>
    /// Error codes specific to subscription cancelling
    /// </summary>
    public enum CancelSubscriptionErrorCode
    {
        /// <summary>
        /// Unknown/Unspecific error
        /// </summary>
        None = 0,

        /// <summary>
        /// Unknown/Unspecific error
        /// </summary>
        Unknown = 1,

        /// <summary>
        /// Refund mismatch error
        /// </summary>
        AmountRefundMismatch = 2,

        /// <summary>
        /// Subscription is already canceled
        /// </summary>
        SubscriptionAlreadyCanceled = 3,
    }

    /// <summary>
    /// Error codes (used in <seealso cref="ErrorInfo"/>.)
    /// </summary>
    public enum ErrorCode
    {
        /// <summary>
        /// Unknown error.
        /// </summary>
        None = 0,

        /// <summary>
        /// Unknown error.
        /// </summary>
        Unknown = 1,

        /// <summary>
        /// One (or more) of the input parameters was invalid.
        /// </summary>
        InvalidInput = 2,

        /// <summary>
        /// Partner service did not recognize the user PUID.
        /// </summary>
        UnknownUser = 3,

        /// <summary>
        /// Partner service returned an invalid response or unknown error.
        /// </summary>
        PartnerError = 4,

        /// <summary>
        /// Partner service timed out.
        /// </summary>
        PartnerTimeout = 5,

        /// <summary>
        /// Either partner service returned 401 Unauthorized or failed to authorize to service.
        /// </summary>
        PartnerAuthorizationFailure = 6,

        /// <summary>
        /// Partner service could not be contacted.
        /// </summary>
        PartnerUnreachable = 7,

        /// <summary>
        /// Partner server certificate could not be validated.
        /// </summary>
        PartnerCertificateInvalid = 8,

        /// <summary>
        /// Partner service returned unauthorized due to supplied MSA token being invalid.
        /// </summary>
        PartnerAuthorizationFailureMsaToken = 9,

        /// <summary>
        /// Calls to partner service are temporarily restricted.
        /// </summary>
        PartnerTemporarilyUnavailable = 10,

        /// <summary>
        /// Partner service's TOS (Terms of Service) have been updated. User needs to accept the new TOS. 
        /// </summary>
        UserNeedsToAcceptUpdatedTermsOfService = 11,

        /// <summary>
        /// Client request did not contain the required authorization credentials (e.g. certificate, MSA token(s) etc.)
        /// </summary>
        MissingClientCredentials = 12,

        /// <summary>
        /// Credentials provided in the client request could not be validated.
        /// </summary>
        InvalidClientCredentials = 13,

        /// <summary>
        /// A response from the service was not valid.
        /// </summary>
        ServiceError = 14,

        /// <summary>
        /// Per the current state of the target resource, operation is not allowed.
        /// </summary>
        InvalidOperation = 15,
        
        /// <summary>
        /// Partner service returned 403 Forbidden.
        /// </summary>
        PartnerForbiddenFailure = 16,

        /// <summary>
        /// Partner service returned an error that we recognize is attributable to an error on their service.
        /// </summary>
        PartnerErrorExternal = 17,

        /// <summary>
        /// The Partner service indicated problem with the request we issued (we probably formed the request badly)
        /// </summary>
        PartnerErrorInternal = 18,

        /// <summary>
        /// The Partner service indicated the resource does not exist
        /// </summary>
        PartnerErrorNotFound = 19,

        /// <summary>
        /// The requested resource was not found.
        /// </summary>
        ResourceNotFound = 20
    }
}
