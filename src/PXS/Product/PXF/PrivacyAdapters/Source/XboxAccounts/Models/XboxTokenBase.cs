// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Models
{
    using System;

    using Microsoft.Membership.MemberServices.Common.Utilities;

    /// <summary>
    ///     The base representation of an Xbox token.
    /// </summary>
    public class XboxTokenBase
    {
        /// <summary>
        ///     Contains display claims for the user.
        /// </summary>
        public XboxAuthDisplayClaims DisplayClaims { get; set; }

        /// <summary>
        ///     The timestamp in UTC at which the token was issued.
        /// </summary>
        public DateTime IssueInstant { get; set; }

        /// <summary>
        ///     The expiration time in UTC for the token. Token may expire sooner.
        /// </summary>
        public DateTime NotAfter { get; set; }

        /// <summary>
        ///     The Xbox service token.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        ///     Returns a string that represents the current Xbox token.
        /// </summary>
        /// <returns>A string that represents the current Xbox token.</returns>
        public override string ToString()
        {
            return "IssueInstant={0}, NotAfter={1}, Token={2}, DisplayClaims={3}".FormatInvariant(
                this.IssueInstant,
                this.NotAfter,
                this.Token,
                this.DisplayClaims);
        }
    }
}
