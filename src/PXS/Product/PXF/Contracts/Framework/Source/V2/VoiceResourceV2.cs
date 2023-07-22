// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.DataContracts.V2
{
    using Newtonsoft.Json;

    /// <summary>
    ///     Voice resource record V1
    /// </summary>
    public class VoiceResourceV2 : PrivacyResourceV2
    {
        /// <summary>
        ///     Gets or sets the application for the event.
        /// </summary>
        [JsonProperty("application")]
        public string Application { get; set; }

        /// <summary>
        ///     Gets or sets the device type for the event.
        /// </summary>
        [JsonProperty("deviceType")]
        public string DeviceType { get; set; }

        /// <summary>
        ///     Gets or sets the translated utterance.
        /// </summary>
        [JsonProperty("displayText")]
        public string DisplayText { get; set; }

        /// <summary>
        ///     The id of the audio.
        /// </summary>
        [JsonProperty("id")]
        [JsonRequired]
        public string Id { get; set; }
    }
}
