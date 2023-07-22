// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Models
{
    using System.Runtime.Serialization;

    using Microsoft.Membership.MemberServices.Common.Utilities;

    /// <summary>
    ///     Xbox user identity (XUI) claims.
    /// </summary>
    [DataContract]
    public class XuiClaims
    {
        /// <summary>
        ///     This can be Child, Teen, or Adult.
        /// </summary>
        [DataMember(Name = "agg")]
        public string AgeGroup { get; set; }

        /// <summary>
        ///     The Xbox gamertag of the user.
        /// </summary>
        [DataMember(Name = "gtg")]
        public string Gamertag { get; set; }

        /// <summary>
        ///     The set of privileges the user has.
        /// </summary>
        [DataMember(Name = "prv")]
        public string Privileges { get; set; }

        /// <summary>
        ///     The user's unique identifier to be used when constructing Authorization headers.
        /// </summary>
        [DataMember(Name = "uhs")]
        public string UserHash { get; set; }

        /// <summary>
        ///     The user's unique identifier for Xbox Live services.
        /// </summary>
        [DataMember(Name = "xid")]
        public string Xuid { get; set; }

        /// <summary>
        ///     Returns a string that represents the current XUI claims.
        /// </summary>
        /// <returns>A string that represents the current XUI configuration.</returns>
        public override string ToString()
        {
            return "AgeGroup={0}, Gamertag={1}, Privileges={2}, Xuid={3}, UserHash={4}".FormatInvariant(this.AgeGroup, this.Gamertag, this.Privileges, this.Xuid, this.UserHash);
        }
    }
}
