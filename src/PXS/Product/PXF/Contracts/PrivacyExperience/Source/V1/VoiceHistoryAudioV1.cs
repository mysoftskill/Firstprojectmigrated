// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1
{
    using Newtonsoft.Json;

    /// <summary>
    /// Voice History Audio V1
    /// </summary>
    public class VoiceHistoryAudioV1 : ResourceV1
    {
        /// <summary>
        /// Gets or sets the audio
        /// </summary>
        [JsonProperty("audio")]
        public byte[] Audio { get; set; }
    }
}