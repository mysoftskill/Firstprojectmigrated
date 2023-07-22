// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Models
{
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Utilities;

    /// <summary>
    ///     Additional properties required to create a valid XASS request.
    /// </summary>
    internal class XassProperties
    {
        /// <summary>
        ///     An Xbox defined name for their authentication service associated to the S2S application token, e.g.
        ///     "s2sapp.user.auth.dnet.xboxlive.com".
        /// </summary>
        public string AppSiteName { get; set; }

        /// <summary>
        ///     The MSA S2S application token in the format "a={{appToken}}", e.g. "a=EwCYA...".
        /// </summary>
        public string AppTicket { get; set; }

        /// <summary>
        ///     The authentication method used to retrieve the Xbox application token, e.g. "RPS".
        /// </summary>
        public string AuthMethod { get; set; }

        /// <summary>
        ///     The public key of the private/public key pair generated to sign the request, represented in JWK format.
        /// </summary>
        public Dictionary<string, string> ProofKey { get; set; }

        public override string ToString()
        {
            return "AppTicket={0}, AuthMethod={1}, AppSiteName={2}, ProofKey={3}".FormatInvariant(
                this.AppTicket,
                this.AuthMethod,
                this.AppSiteName,
                EnumerableUtilities.ToString(this.ProofKey));
        }
    }
}
