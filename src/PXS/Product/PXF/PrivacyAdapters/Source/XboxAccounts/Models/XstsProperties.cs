// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Models
{
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Utilities;

    using Newtonsoft.Json;

    /// <summary>
    ///     Additional properties required to create a valid XSTS request.
    /// </summary>
    public class XstsProperties
    {
        /// <summary>
        ///     Xbox doc recommends value "RETAIL"
        /// </summary>
        public string SandboxId { get; set; }

        /// <summary>
        ///     The Xbox service token retrieved from XASS.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ServiceToken { get; set; }

        /// <summary>
        ///     The Xbox user token retrieved from XASU. Should contain only one element.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<string> UserTokens { get; set; }

        public override string ToString()
        {
            return "SandboxId={0}, ServiceToken={1}, UserTokens={2}".FormatInvariant(
                this.SandboxId,
                this.ServiceToken,
                EnumerableUtilities.ToString(this.UserTokens));
        }
    }
}
