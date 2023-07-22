// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts
{
    /// <summary>
    ///     Error Messages
    /// </summary>
    public static class ErrorMessages
    {
        /// <summary>
        ///     Duplicate resource type conflict from request input.
        /// </summary>
        public const string DuplicateResourceTypeConflict = "Duplicate ResourceTypes provided in the request input.";

        /// <summary>
        ///     The user is not allowed to bypass throttling
        /// </summary>
        public const string NotAllowedToBypassThrottling = "User is not allowed to bypass throttling.";

        /// <summary>
        ///     The partner response was null
        /// </summary>
        public const string PartnerNullResponse = "Null response from partner.";

        /// <summary>
        ///     A request is already in progress
        /// </summary>
        public const string RequestAlreadyInProgress = "A request is already in progress";

        /// <summary>
        ///     too many requests
        /// </summary>
        public const string TooManyRequests = "Too many requests have been submitted, please come back later";

        /// <summary>
        ///     The unknown error
        /// </summary>
        public const string UnknownError = "An unknown error occurred.";
    }
}
