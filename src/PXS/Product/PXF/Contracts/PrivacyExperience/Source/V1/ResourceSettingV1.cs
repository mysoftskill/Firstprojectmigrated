// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1
{
    using Newtonsoft.Json;

    /// <summary>
    ///     Resource-Setting-V1
    /// </summary>
    public class ResourceSettingV1
    {
        /// <summary>
        ///     Gets or sets the value indicating to allow Microsoft to use the resource type for bing advertising recommendations and offerings
        /// </summary>
        /// <remarks>This is nullable because not all resource types may support advertising settings.</remarks>
        [JsonProperty("advertising", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Advertising { get; set; }

        /// <summary>
        ///     Gets or sets the e tag.
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        ///     Gets or sets the value enabling the sharing of location with other people such as parents in the users family.
        /// </summary>
        [JsonProperty("location_privacy", NullValueHandling = NullValueHandling.Ignore)]
        public bool? LocationPrivacy { get; set; }

        /// <summary>
        ///     Gets or sets the on-behalf-of privacy consent setting.
        /// </summary>
        [JsonProperty("on_behalf_of_privacy", NullValueHandling = NullValueHandling.Ignore)]
        public bool? OnBehalfOfPrivacy { get; set; }

        /// <summary>
        ///     Gets or sets the value indicating to allow Microsoft to use the resource type for driving targeted recommendations and offerings
        /// </summary>
        /// <remarks>This is nullable because not all resource types may support tailored_experiences_offers settings.</remarks>
        [JsonProperty("tailored_experiences_offers", NullValueHandling = NullValueHandling.Ignore)]
        public bool? TailoredExperiencesOffers { get; set; }

        /// <summary>
        ///     Gets or sets the value indicating to allow Microsoft share customer data to 3rd parties
        /// </summary>
        [JsonProperty("sharing_state", NullValueHandling = NullValueHandling.Ignore)]
        public bool? SharingState { get; set; }
    }
}
