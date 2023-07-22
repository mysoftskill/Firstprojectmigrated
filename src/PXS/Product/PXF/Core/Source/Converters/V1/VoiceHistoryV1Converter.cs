// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Converters
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    /// <summary>
    /// VoiceHistoryV1 Converter
    /// </summary>
    public static class VoiceHistoryV1Converter
    {
        /// <summary>
        /// Converts a collection of <see cref="VoiceResource"/> to a collection of <see cref="VoiceHistoryV1"/>.
        /// </summary>
        /// <param name="voiceResources">The voice resources.</param>
        /// <returns>A collection of <see cref="VoiceHistoryV1"/></returns>
        internal static List<VoiceHistoryV1> ToVoiceHistoryV1(this IEnumerable<VoiceResource> voiceResources)
        {
            if (voiceResources == null)
            {
                return null;
            }

            var voiceHistoryV1 = new List<VoiceHistoryV1>();

            foreach (var browseResource in voiceResources)
            {
                voiceHistoryV1.Add(browseResource.ToVoiceHistoryV1());
            }

            return voiceHistoryV1;
        }

        /// <summary>
        /// Converts <see cref="VoiceResource"/> to <see cref="VoiceHistoryV1"/>.
        /// </summary>
        /// <param name="voiceResource">The browse resource.</param>
        /// <returns>VoiceHistoryV1</returns>
        public static VoiceHistoryV1 ToVoiceHistoryV1(this VoiceResource voiceResource)
        {
            // TODO: This is needed for using PDAPI over PXS V1 interface. This will go away.
            string id = $"{voiceResource.Id},{voiceResource.DateTime.ToUniversalTime().Ticks}";

            VoiceHistoryV1 voiceHistoryV1 = new VoiceHistoryV1();
            voiceHistoryV1.DateTime = voiceResource.DateTime;
            voiceHistoryV1.DeviceId = voiceResource.DeviceId;
            voiceHistoryV1.Id = id;
            var ids = new List<string> { id };
            voiceHistoryV1.Ids = ids;
            voiceHistoryV1.IsAggregate = ids.Count > 1;
            voiceHistoryV1.DisplayText = voiceResource.DisplayText;
            voiceHistoryV1.Application = voiceResource.Application;
            voiceHistoryV1.DeviceType = voiceResource.DeviceType;
            voiceHistoryV1.Source = voiceResource.Sources?.FirstOrDefault();
            voiceHistoryV1.PartnerId = voiceResource.PartnerId;

            return voiceHistoryV1;
        }

        /// <summary>
        /// Converts <see cref="VoiceAudioResource"/> to <see cref="VoiceHistoryAudioV1"/>.
        /// </summary>
        /// <param name="voiceAudioResource">The voice audio resource.</param>
        /// <returns>VoiceHistoryAudioV1</returns>
        internal static VoiceHistoryAudioV1 ToVoiceHistoryAudioV1(this VoiceAudioResource voiceAudioResource)
        {
            VoiceHistoryAudioV1 voiceHistoryAudioV1 = new VoiceHistoryAudioV1();
            voiceHistoryAudioV1.DateTime = voiceAudioResource.DateTime;
            voiceHistoryAudioV1.DeviceId = voiceAudioResource.DeviceId;
            voiceHistoryAudioV1.Id = voiceAudioResource.Id;
            var ids = new List<string> { voiceAudioResource.Id };
            voiceHistoryAudioV1.Ids = ids;
            voiceHistoryAudioV1.IsAggregate = ids.Count > 1;
            voiceHistoryAudioV1.Audio = voiceAudioResource.Audio;
            voiceHistoryAudioV1.Source = voiceAudioResource.Sources?.FirstOrDefault();
            voiceHistoryAudioV1.PartnerId = voiceAudioResource.PartnerId;

            return voiceHistoryAudioV1;
        }
    }
}