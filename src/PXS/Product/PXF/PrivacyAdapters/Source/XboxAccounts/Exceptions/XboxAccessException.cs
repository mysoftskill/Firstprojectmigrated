// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class XboxAccessException : XboxAuthenticationException
    {
        private const string AccessError =
            "Access to the sandbox specified in the request was denied. You should verify that the correct sandbox "
            + "was specified in the request, and/or that the appropriate access policies were created through your DAM.";

        public XboxAccessException()
            : base(AccessError)
        {
        }

        public XboxAccessException(string message)
            : base(message)
        {
        }

        public XboxAccessException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected XboxAccessException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
