// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService.Models
{
    using Newtonsoft.Json;

    /// <summary>
    ///     AAD RVS response model.
    ///     https://microsoft.sharepoint.com/:w:/t/DataScienceEngineering/ETThJvxBjyhPkpZjxbH8JuUBRoWCSekpQaB9GT-J6Tj7Pg?e=Gt9KlT
    /// </summary>
    public class AadRvsResponse
    {
        /// <summary>
        ///     Gets or sets the outcome.
        /// </summary>
        [JsonProperty("outcome")]
        public string Outcome { get; set; }

        /// <summary>
        ///     Gets or sets the message.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
