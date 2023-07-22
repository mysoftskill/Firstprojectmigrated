// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2
{
    /// <summary>
    ///     This represents the audio stream for a voice card
    /// </summary>
    public class VoiceCardAudio
    {
        /// <summary>
        ///     The audio stream for playback
        /// </summary>
        public byte[] Audio { get; }

        public VoiceCardAudio(byte[] audio)
        {
            this.Audio = audio;
        }
    }
}
