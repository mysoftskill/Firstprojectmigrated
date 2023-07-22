// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Security;
    using Microsoft.Membership.MemberServices.Contracts.Exposed;

    /// <summary>
    /// Helper class to convert exceptions thrown by the WCF Service client into generic
    /// ErrorInfo type.
    /// </summary>
    public static class WcfServiceClientErrorHelper
    {
        /// <summary>
        /// Maps specific WCF client exceptions to an ErrorInfo object. Unrecognized exceptions will be 
        /// returned as ErrorCode.Unknown
        /// </summary>
        /// <param name="ex">The exception thrown by the client</param>
        /// <returns>The ErrorInfo object representing the error case</returns>
        public static ErrorInfo CreateErrorInfo(Exception ex)
        {
            ErrorInfo errorInfo = new ErrorInfo
            {
                ErrorCode = ErrorCode.Unknown,
                ErrorMessage = ex.Message
            };

            // Handle certain known communication errors
            if (ex is EndpointNotFoundException)
            {
                errorInfo.ErrorCode = ErrorCode.PartnerUnreachable;
            }
            else if (ex is AddressAccessDeniedException || ex is MessageSecurityException ||
                     ex is SecurityAccessDeniedException || ex is SecurityNegotiationException)
            {
                errorInfo.ErrorCode = ErrorCode.PartnerAuthorizationFailure;
            }
            else if (ex is CommunicationException)
            {
                // Any other communication exception
                errorInfo.ErrorCode = ErrorCode.PartnerError;
            }
            else if (ex is TimeoutException)
            {
                // Partner timeout case
                errorInfo.ErrorCode = ErrorCode.PartnerTimeout;
            }

            return errorInfo;
        }
    }
}
