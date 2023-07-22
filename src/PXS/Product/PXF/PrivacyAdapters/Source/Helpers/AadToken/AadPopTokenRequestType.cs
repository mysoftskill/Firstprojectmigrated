// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers
{
    /// <summary>
    ///     Enumeration of PFT token request types.
    /// </summary>
    public enum AadPopTokenRequestType
    {
        /// <summary>
        ///     Unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     AppAssertedUserToken.
        /// </summary>
        AppAssertedUserToken = 1,

        /// <summary>
        ///     MsaProxyTicket.
        ///     RPS ticket type 10 “proxy compact ticket” is a id token forwarded (like a PFT)
        /// </summary>
        MsaProxyTicket = 2
    }
}
