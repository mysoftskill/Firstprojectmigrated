// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Models
{
    using Microsoft.Membership.MemberServices.Common.Utilities;

    public class XboxAuthResponseWithClaims : XboxAuthResponseBase
    {
        /// <summary>
        ///     Contains display claims for the user.
        /// </summary>
        public XboxAuthDisplayClaims DisplayClaims { get; set; }

        public override string ToString()
        {
            return "DisplayClaims={0}".FormatInvariant(this.DisplayClaims);
        }
    }
}
