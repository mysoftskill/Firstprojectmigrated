// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Models
{
    using Microsoft.Membership.MemberServices.Common.Utilities;

    /// <summary>
    ///     Contract to retrieve Xbox service token from Xbox Authentication Service for Services (XASS).
    /// </summary>
    internal class XassRequest
    {
        /// <summary>
        ///     Additional properties to create a valid XASS request.
        /// </summary>
        public XassProperties Properties { get; set; }

        /// <summary>
        ///     An Xbox defined name for their authentication service, e.g. "https://auth.xboxlive.com".
        /// </summary>
        public string RelyingParty { get; set; }

        /// <summary>
        ///     The format the token should be returned in, e.g. "JWT".
        /// </summary>
        public string TokenType { get; set; }

        public override string ToString()
        {
            return "TokenType={0}, RelyingParty={1}, Properties={2}".FormatInvariant(
                this.TokenType,
                this.RelyingParty,
                this.Properties.ToString());
        }
    }
}
