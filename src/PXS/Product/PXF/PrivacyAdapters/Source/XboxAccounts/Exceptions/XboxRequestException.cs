// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class XboxRequestException : XboxAuthenticationException
    {
        private const string XboxCallError = "Xbox authentication service returned bad request exception.";

        public XboxRequestException()
            : base(XboxCallError)
        {
        }

        public XboxRequestException(string message)
            : base(message)
        {
        }

        public XboxRequestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected XboxRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
