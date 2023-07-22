//--------------------------------------------------------------------------------
// <copyright file="ErrorMessages.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Common.Validation
{
    /// <summary>
    /// Miscellaneous error messages (and error message formats).
    /// </summary>
    public static class ErrorMessages
    {
        /// <summary>
        /// The null-value error message.
        /// </summary>
        public const string NullValueErrorMessage = "Value cannot be null";

        /// <summary>
        /// The empty-value error message.
        /// </summary>
        public const string EmptyValueErrorMessage = "Value cannot be empty";

        /// <summary>
        /// The null-value error message format.
        /// </summary>
        public const string NullValueErrorMessageFormat = "{0} cannot be null.";

        /// <summary>
        /// The string-is-null-or-whitespace error message.
        /// </summary>
        public const string StringNullOrWhiteSpaceErrorMessage = "Value cannot be null or whitespace";

        /// <summary>
        /// The string-is-null-or-whitespace error message format.
        /// </summary>
        public const string StringNullOrWhiteSpaceErrorMessageFormat = "{0} cannot be null or whitespace.";

        /// <summary>
        /// The value-is-null error message format.
        /// </summary>
        public const string ValueNullOrInvalidMessageFormat = "{0} cannot be null or invalid";

        /// <summary>
        /// The value-is-not-defined-or-is-default error message.
        /// </summary>
        public const string ValueNotDefinedOrIsDefaultMessageFormat = "{0} is not defined or is the default";

        /// <summary>
        /// The value-is-out-of-range error message.
        /// </summary>
        public const string ValueOutOfRangeErrorMessage = "Value is out of range";

        /// <summary>
        /// The collection-is-empty-or-null error message format.
        /// </summary>
        public const string CollectionEmptyOrNullErrorMessageFormat = "{0} cannot be null or empty.";

        /// <summary>
        /// The dddd error message.
        /// </summary>
        public const string NegativeErrorMessageFormat = "{0} cannot be negative. Value={1}.";

        /// <summary>
        /// The less-than-zero error message format.
        /// </summary>
        public const string NonPositiveErrorMessageFormat = "{0} cannot be <=0. Value={1}.";

        /// <summary>
        /// The user-name-and-password-not-specified error message.
        /// </summary>
        public const string UserNameAndPasswordErrorMessage = "Both UserName and Password must be provided, or neither";

        /// <summary>
        /// The restricted-access error message.
        /// </summary>
        public const string PartnerBlocked = "Calls to partner service are temporarily restricted.";

        /// <summary>
        /// The not-authorized error message.
        /// </summary>
        public const string ResourceOwnership = "Resource id is not valid for the specified user.";
    }
}
