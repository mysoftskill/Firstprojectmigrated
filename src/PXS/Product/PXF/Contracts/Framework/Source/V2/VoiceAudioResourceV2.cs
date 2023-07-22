// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.DataContracts.V2
{
    using Newtonsoft.Json;

    /// <summary>
    ///     Voice audio resource record V2
    /// </summary>
    public class VoiceAudioResourceV2 : VoiceResourceV2
    {
        /// <summary>
        ///     Ordered array of byte arrays, each byte array one of the audio chunks.
        /// </summary>
        [JsonProperty("audioChunks")]
        public byte[][] AudioChunks { get; set; }

        /// <summary>
        ///     Audio Metadata
        /// </summary>
        [JsonProperty("averageByteRate")]
        public int AverageByteRate { get; set; }

        /// <summary>
        ///     Audio Metadata
        /// </summary>
        [JsonProperty("bitsPerSample")]
        public int BitsPerSample { get; set; }

        /// <summary>
        ///     Audio Metadata
        /// </summary>
        [JsonProperty("blockAlign")]
        public int BlockAlign { get; set; }

        /// <summary>
        ///     Audio Metadata
        /// </summary>
        [JsonProperty("channelCount")]
        public int ChannelCount { get; set; }

        /// <summary>
        ///     Audio Metadata
        /// </summary>
        [JsonProperty("encodingFormat")]
        public int EncodingFormat { get; set; }

        /// <summary>
        ///     Audio Metadata
        /// </summary>
        [JsonProperty("extraHeader")]
        public byte[] ExtraHeader { get; set; }

        /// <summary>
        ///     Audio Metadata
        /// </summary>
        [JsonProperty("sampleRate")]
        public int SampleRate { get; set; }
    }
}
