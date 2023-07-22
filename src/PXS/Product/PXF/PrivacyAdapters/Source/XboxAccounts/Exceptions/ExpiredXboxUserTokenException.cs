// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class ExpiredXboxUserTokenException : XboxAuthenticationException
    {
        private const string ExpiredUserTokenError = "An expired user token was passed in the request.";

        public ExpiredXboxUserTokenException()
            : base(ExpiredUserTokenError)
        {
        }

        public ExpiredXboxUserTokenException(string message)
            : base(message)
        {
        }

        public ExpiredXboxUserTokenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ExpiredXboxUserTokenException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
