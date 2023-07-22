// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Models
{
    public class XassResponse : XboxAuthResponseWithClaims
    {
        /// <summary>
        ///     Converts XASS response to XASS token object.
        /// </summary>
        /// <param name="response">XASS Response to convert.</param>
        /// <returns>XASS token.</returns>
        public static XassToken ToToken(XassResponse response)
        {
            return new XassToken
            {
                IssueInstant = response.IssueInstant,
                NotAfter = response.NotAfter,
                Token = response.Token,
                DisplayClaims = response.DisplayClaims
            };
        }
    }
}
