// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PXS.Command.Contracts.V1
{
    using System.ComponentModel;

    using Newtonsoft.Json;

    /// <summary>
    ///     Account close request
    /// </summary>
    public class AccountCloseRequest : PrivacyRequest
    {
        /// <summary>
        ///     Gets or sets the reason the account was closed.
        /// </summary>
        [JsonProperty("accountCloseReason", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(AccountCloseReason.None)]
        public AccountCloseReason AccountCloseReason { get; set; }
    }

    /// <summary>
    ///     Why the account was closed
    /// </summary>
    /// <remarks>
    ///     https://microsoft.sharepoint.com/teams/MSApartner/SitePages/LearningSeries/Learning%20Series%20When%20accounts%20are%20closed.aspx
    /// </remarks>
    public enum AccountCloseReason
    {
        /// <summary>
        ///     The <see cref="AccountCloseReason" /> was not assigned
        /// </summary>
        None = 0,

        /// <summary>
        ///     The user account had a creation failure
        /// </summary>
        UserAccountCreationFailure = 1,

        /// <summary>
        ///     [Default] The user closed their account
        /// </summary>
        UserAccountClosed = 2,

        /// <summary>
        ///     The user account aged out
        /// </summary>
        UserAccountAgedOut = 3,

        /// <summary>
        ///     Testing account close
        /// </summary>
        Test = 4
    }
}
