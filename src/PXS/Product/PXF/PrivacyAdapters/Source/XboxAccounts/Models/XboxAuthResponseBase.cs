// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Models
{
    using System;

    using Microsoft.Membership.MemberServices.Common.Utilities;

    public class XboxAuthResponseBase
    {
        /// <summary>
        ///     The timestamp in UTC at which the token was issued.
        /// </summary>
        public DateTime IssueInstant { get; set; }

        /// <summary>
        ///     The indication of Auth response validity.
        /// </summary>
        public bool IsValid
        {
            get { return !string.IsNullOrEmpty(this.Token) && DateTime.UtcNow < this.NotAfter; }
        }

        /// <summary>
        ///     The expiration time in UTC for the token. Token may expire sooner.
        /// </summary>
        public DateTime NotAfter { get; set; }

        /// <summary>
        ///     The Xbox service token.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        ///     Converts the value of this instance to a <see cref="System.String" />.
        /// </summary>
        /// <returns>A string whose value is the same as this instance.</returns>
        public override string ToString()
        {
            return "IssueInstant={0}, NotAfter={1}, Token={2}".FormatInvariant(this.IssueInstant, this.NotAfter, this.Token);
        }
    }
}
