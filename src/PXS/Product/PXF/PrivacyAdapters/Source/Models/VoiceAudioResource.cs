// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Models
{
    /// <summary>
    /// Voice Audio resource
    /// </summary>
    public sealed class VoiceAudioResource : Resource
    {
        /// <summary>
        /// Gets or sets the audio data bytes for the resource.
        /// </summary>
        public byte[] Audio { get; set; }
    }
}
