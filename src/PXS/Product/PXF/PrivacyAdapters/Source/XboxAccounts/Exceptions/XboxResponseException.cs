// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class XboxResponseException : XboxAuthenticationException
    {
        private const string XboxCallError = "Xbox authentication service returned an invalid response.";

        public XboxResponseException()
            : base(XboxCallError)
        {
        }

        public XboxResponseException(string message)
            : base(message)
        {
        }

        public XboxResponseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected XboxResponseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
