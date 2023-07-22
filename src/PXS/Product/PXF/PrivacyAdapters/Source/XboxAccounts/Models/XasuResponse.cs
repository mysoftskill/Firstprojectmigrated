// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Models
{
    /// <summary>
    ///     Class for XASU response
    /// </summary>
    public class XasuResponse : XboxAuthResponseWithClaims
    {
        /// <summary>
        ///     Converts XASU response to XASU token object.
        /// </summary>
        /// <param name="response">XASU Response to convert.</param>
        /// <returns>XASU token.</returns>
        public static XasuToken ToToken(XasuResponse response)
        {
            return new XasuToken
            {
                IssueInstant = response.IssueInstant,
                NotAfter = response.NotAfter,
                Token = response.Token,
                DisplayClaims = response.DisplayClaims
            };
        }
    }
}
