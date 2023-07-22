// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Models
{
    using System.Collections.Generic;

    /// <summary>
    ///     Public class for XASU request
    /// </summary>
    public class XasuRequest
    {
        /// <summary>
        ///     Gets or sets properties
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }

        /// <summary>
        ///     Gets or sets relying party
        /// </summary>
        public string RelyingParty { get; set; }

        /// <summary>
        ///     Gets or sets token type
        /// </summary>
        public string TokenType { get; set; }
    }
}
