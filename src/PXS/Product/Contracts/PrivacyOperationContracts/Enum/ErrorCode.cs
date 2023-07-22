// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.PrivacyOperation.Contracts
{
    /// <summary>
    /// Error Code
    /// </summary>
    public enum ErrorCode
    {
        /// <summary>
        /// Unknown error (Default)
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Invalid input (Bad Request)
        /// </summary>
        InvalidInput = 1,

        /// <summary>
        /// Partner service returned an invalid response or unknown error.
        /// </summary>
        PartnerError = 2,

        /// <summary>
        /// Client request did not contain the required authorization credentials (e.g. certificate, MSA token(s) etc.)
        /// </summary>
        MissingClientCredentials = 3,

        /// <summary>
        /// Credentials provided in the client request could not be validated.
        /// </summary>
        InvalidClientCredentials = 4,

        /// <summary>
        /// Precondition failed
        /// </summary>
        PreconditionFailed = 5,

        /// <summary>
        /// Client is unauthorized by statutory age rules
        /// </summary>
        UnauthorizedStatutoryAge = 6,

        /// <summary>
        /// Timeout making request to partner service.
        /// </summary>
        PartnerTimeout = 7,

        /// <summary>
        /// return error code 429 too-many-requests.
        /// </summary>
        TooManyRequests = 8,

        /// <summary>
        /// Client is unauthorized by majority age rules
        /// </summary>
        UnauthorizedMajorityAge = 9,

        /// <summary>
        /// Generic unauthorized error code
        /// </summary>
        Unauthorized = 10,

        /// <summary>
        /// Update concurrency conflict
        /// </summary>
        UpdateConflict = 11,

        /// <summary>
        /// The resource not modified
        /// </summary>
        ResourceNotModified = 12,

        /// <summary>
        /// Create conflict
        /// </summary>
        CreateConflict = 13,

        /// <summary>
        /// Resource not found.
        /// </summary>
        ResourceNotFound = 14,

        /// <summary>
        /// Time Window Expired.
        /// </summary>
        TimeWindowExpired = 15
    }
}