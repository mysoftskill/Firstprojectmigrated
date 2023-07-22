// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core
{
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    /// <summary>
    ///     AadRvsErrorToServiceResponseExtension
    /// </summary>
    public static class AadRvsErrorToServiceResponseExtension
    {
        public static ErrorCode ToServiceErrorCode(this AdapterErrorCode adapterErrorCode)
        {
            switch (adapterErrorCode)
            {
                case AdapterErrorCode.InvalidInput:
                    return ErrorCode.InvalidInput;
                case AdapterErrorCode.Unauthorized:
                    return ErrorCode.Unauthorized;
                case AdapterErrorCode.Forbidden:
                    return ErrorCode.Forbidden;
                case AdapterErrorCode.ResourceNotFound:
                    return ErrorCode.ResourceNotFound;
                case AdapterErrorCode.MethodNotAllowed:
                    return ErrorCode.MethodNotAllowed;
                case AdapterErrorCode.ConcurrencyConflict:
                    return ErrorCode.ConcurrencyConflict;
                case AdapterErrorCode.TooManyRequests:
                    return ErrorCode.TooManyRequests;
                case AdapterErrorCode.NullVerifier:
                default:
                    return ErrorCode.PartnerError;
            }
        }
    }
}
