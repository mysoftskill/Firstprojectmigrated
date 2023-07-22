// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using Newtonsoft.Json;

    /// <summary>
    ///     Card representing a voice utterance
    /// </summary>
    public class VoiceCard : TimelineCard
    {
        public static TimelineCard FromKeyComponents(IDictionary<string, string> components)
        {
            string voiceId;
            if (!components.TryGetValue(KeyConstants.Id, out voiceId))
                throw new ArgumentOutOfRangeException(nameof(components), $"Missing key component: {KeyConstants.Id}");

            string timestampStr;
            if (!components.TryGetValue(KeyConstants.Timestamp, out timestampStr))
                throw new ArgumentOutOfRangeException(nameof(components), $"Missing key component: {KeyConstants.Timestamp}");
            DateTimeOffset timestamp = DateTimeOffset.Parse(timestampStr, CultureInfo.InvariantCulture);

            return new VoiceCard(voiceId, null, null, null, null, timestamp, null, null);
        }

        /// <summary>
        ///     The application where the voice was recorded
        /// </summary>
        public string Application { get; }

        /// <summary>
        ///     The type of device the utterance was on
        /// </summary>
        public string DeviceType { get; }

        /// <summary>
        ///     The text representation of the utterance
        /// </summary>
        public string Text { get; }

        /// <summary>
        ///     The voice id
        /// </summary>
        public string VoiceId { get; }

        public VoiceCard(string voiceId, string text, string application, string deviceType, DateTimeOffset timestamp, IList<string> deviceIds, IList<string> sources)
            : this(voiceId, text, application, deviceType, null, timestamp, deviceIds, sources)
        {
        }

        [JsonConstructor]
        private VoiceCard(string voiceId, string text, string application, string deviceType, string id, DateTimeOffset timestamp, IList<string> deviceIds, IList<string> sources)
            : base(id, timestamp, deviceIds, sources)
        {
            this.Application = application;
            this.DeviceType = deviceType;
            this.Text = text;
            this.VoiceId = voiceId;
        }

        protected override IDictionary<string, string> GetKeyComponents()
        {
            return new Dictionary<string, string>
            {
                { KeyConstants.Id, this.VoiceId },
                { KeyConstants.Timestamp, this.Timestamp.ToString("o", CultureInfo.InvariantCulture) }
            };
        }
    }
}
