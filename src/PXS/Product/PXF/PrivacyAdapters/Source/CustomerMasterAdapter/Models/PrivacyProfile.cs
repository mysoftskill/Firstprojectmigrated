// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter
{
    using Newtonsoft.Json;

    /// <summary>
    ///     Customer Master Privacy-Profile
    /// </summary>
    /// <remarks>
    ///     The CM team maintains the JSON property names at:
    ///     https://microsoft.visualstudio.com/Universal%20Store/_git/MCKP.Jarvis.CM.TransactionService?path=%2FProduct%2FAccountService%2FDynamicProfileDefinitions%2FMSAPrivacyProfile.JSON
    /// </remarks>
    public class PrivacyProfile : Profile
    {
        /// <summary>
        ///     Gets or sets advertising property in MSAPrivacy resource type in Customer Master.
        /// </summary>
        [JsonProperty("advertising")]
        public bool? Advertising { get; set; }

        /// <summary>
        ///     Gets or sets the value enabling the sharing of location with other people such as parents in the users family.
        /// </summary>
        [JsonProperty("OBOPrivacyLocation")]
        public bool? LocationPrivacy { get; set; }

        /// <summary>
        ///     Gets or sets the on-behalf-of privacy consent setting,
        /// </summary>
        [JsonProperty("OBOPrivacy")]
        public bool? OnBehalfOfPrivacy { get; set; }

        /// <summary>
        ///     Gets or sets tailored_experiences_offers property in MSAPrivacy resource type in Customer Master.
        /// </summary>
        [JsonProperty("tailored_experiences_offers")]
        public bool? TailoredExperiencesOffers { get; set; }

        /// <summary>
        ///     Gets or sets sharing state property in MSAPrivacy resource type in Customer Master.
        /// </summary>
        [JsonProperty("sharing_state")]
        public bool? SharingState { get; set; }

    }
}
