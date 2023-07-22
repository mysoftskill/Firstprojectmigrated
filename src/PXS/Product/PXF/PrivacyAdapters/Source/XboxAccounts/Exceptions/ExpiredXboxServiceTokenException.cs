// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class ExpiredXboxServiceTokenException : XboxAuthenticationException
    {
        private const string ExpiredServiceTokenError = "An expired service token was passed in the request.";

        public ExpiredXboxServiceTokenException()
            : base(ExpiredServiceTokenError)
        {
        }

        public ExpiredXboxServiceTokenException(string message)
            : base(message)
        {
        }

        public ExpiredXboxServiceTokenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ExpiredXboxServiceTokenException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
