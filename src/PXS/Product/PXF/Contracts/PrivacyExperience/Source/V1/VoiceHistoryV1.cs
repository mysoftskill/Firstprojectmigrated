// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1
{
    using Newtonsoft.Json;

    /// <summary>
    /// Voice History V1
    /// </summary>
    public class VoiceHistoryV1 : ResourceV1
    {
        /// <summary>
        /// Gets or sets the translated utterance.
        /// </summary>
        [JsonProperty("displayText")]
        public string DisplayText { get; set; }

        /// <summary>
        /// Gets or sets the application for the event.
        /// </summary>
        [JsonProperty("application")]
        public string Application { get; set; }

        /// <summary>
        /// Gets or sets the device type for the event.
        /// </summary>
        [JsonProperty("deviceType")]
        public string DeviceType { get; set; }
    }
}