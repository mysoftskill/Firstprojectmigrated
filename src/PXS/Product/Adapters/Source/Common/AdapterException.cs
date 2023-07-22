// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common
{
    using System;
    using Contracts.Exposed;
    using Oss.Membership.CommonCore.Extensions;

    public class AdapterException : Exception
    {
        private const string MessageFormat = "ErrorCode: {0}, ErrorMessage: {1}";

        public AdapterException(ErrorCode errorCode, string errorMessage) : 
            base(MessageFormat.FormatInvariant(errorCode, errorMessage))
        {
            this.ErrorCode = errorCode;
            this.ErrorMessage = errorMessage;
        }

        public ErrorCode ErrorCode { get; private set; }

        public string ErrorMessage { get; private set; }
    }
}