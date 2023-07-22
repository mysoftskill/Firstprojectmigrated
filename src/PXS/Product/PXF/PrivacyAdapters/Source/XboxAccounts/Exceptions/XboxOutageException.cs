// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class XboxOutageException : XboxAuthenticationException
    {
        private const string XboxServiceOutageError = "Xbox Live authentication infrastructure is currently experiencing an outage.";

        public XboxOutageException()
            : base(XboxServiceOutageError)
        {
        }

        public XboxOutageException(string message)
            : base(message)
        {
        }

        public XboxOutageException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected XboxOutageException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
