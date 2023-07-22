// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class XboxAuthenticationException : Exception
    {
        public XboxAuthenticationException()
        {
        }

        public XboxAuthenticationException(string message)
            : base(message)
        {
        }

        public XboxAuthenticationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected XboxAuthenticationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
