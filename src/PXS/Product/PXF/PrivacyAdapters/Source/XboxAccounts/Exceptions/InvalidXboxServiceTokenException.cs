// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class InvalidXboxServiceTokenException : XboxAuthenticationException
    {
        private const string InvalidServiceTokenError = "An invalid service token was passed in the request.";

        public InvalidXboxServiceTokenException()
            : base(InvalidServiceTokenError)
        {
        }

        public InvalidXboxServiceTokenException(string message)
            : base(message)
        {
        }

        public InvalidXboxServiceTokenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidXboxServiceTokenException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
