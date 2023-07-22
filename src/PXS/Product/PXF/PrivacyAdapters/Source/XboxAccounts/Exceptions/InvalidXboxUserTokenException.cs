// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class InvalidXboxUserTokenException : XboxAuthenticationException
    {
        private const string InvalidUserTokenError = "An invalid user token was passed in the request.";

        public InvalidXboxUserTokenException()
            : base(InvalidUserTokenError)
        {
        }

        public InvalidXboxUserTokenException(string message)
            : base(message)
        {
        }

        public InvalidXboxUserTokenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidXboxUserTokenException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
