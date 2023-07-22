// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class XboxUserAccountException : XboxAuthenticationException
    {
        private const string UserAccountError =
            "There is an issue with the user account. The user should be advised to resolve any issue either on the "
            + "console or by signing in to https://xbox.com or by signing in to the Xbox app on PC.";

        /// <summary>
        ///     Error code
        /// </summary>
        public uint XErr { get; }

        public XboxUserAccountException()
            : base(UserAccountError)
        {
        }

        public XboxUserAccountException(uint XErr)
            : base(UserAccountError)
        {
            this.XErr = XErr;
        }

        public XboxUserAccountException(string message)
            : base(message)
        {
        }

        public XboxUserAccountException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected XboxUserAccountException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
