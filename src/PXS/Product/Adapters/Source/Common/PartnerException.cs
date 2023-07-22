// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common
{
    using System;

    /// <summary>
    /// Abstracts error information returned by a partner service.
    /// </summary>
    public class PartnerException : Exception
    {
        public PartnerException(int errorCode, string errorMessage, bool isRetryable)
            : base(errorMessage)
        {
            this.ErrorCode = errorCode;
            this.IsRetryable = isRetryable;
        }

        public PartnerException(int errorCode, string errorMessage, bool isRetryable, Exception innerException)
            : base(errorMessage, innerException)
        {
            this.ErrorCode = errorCode;
            this.IsRetryable = isRetryable;
        }

        /// <summary>
        /// Error code returned by the partner service.
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// Indicates if the error returned by the partner service is a transient error.
        /// </summary>
        public bool IsRetryable { get; private set; }
    }
}
