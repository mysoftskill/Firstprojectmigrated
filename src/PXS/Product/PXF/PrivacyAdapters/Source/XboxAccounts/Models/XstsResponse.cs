// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Models
{
    public class XstsResponse : XboxAuthResponseWithClaims
    {
        /// <summary>
        ///     Converts XSTS response to XSTS token object.
        /// </summary>
        /// <param name="response">XSTS Response to convert.</param>
        /// <returns>XSTS token.</returns>
        public static XstsToken ToToken(XstsResponse response)
        {
            return new XstsToken
            {
                Token = response.Token,
                NotAfter = response.NotAfter,
                IssueInstant = response.IssueInstant,
                DisplayClaims = response.DisplayClaims
            };
        }
    }
}
