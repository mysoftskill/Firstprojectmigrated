// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Models
{
    using System.Runtime.Serialization;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Utilities;

    /// <summary>
    ///     Claims contain a set of information about the user.
    /// </summary>
    [DataContract]
    public class XboxAuthDisplayClaims
    {
        /// <summary>
        ///     The user's Xbox user identity (XUI) claims
        /// </summary>
        [DataMember(Name = "xui")]
        public XuiClaims[] XuiClaims { get; set; }

        /// <summary>
        ///     Returns a string that represents the current Xbox display claims.
        /// </summary>
        /// <returns>A string that represents the current Xbox display claims.</returns>
        public override string ToString()
        {
            return "XuiClaims={0}".FormatInvariant(EnumerableUtilities.ToString(this.XuiClaims));
        }
    }
}
