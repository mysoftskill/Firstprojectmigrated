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
    ///     The card representing a video consumption event
    /// </summary>
    public class VideoConsumptionCard : ContentConsumptionCard
    {
        /// <summary>
        ///     Build a card from serialized key components
        /// </summary>
        public static VideoConsumptionCard FromKeyComponents(IDictionary<string, string> components)
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));

            string mediaId;
            if (!components.TryGetValue(KeyConstants.Id, out mediaId))
                throw new ArgumentOutOfRangeException(nameof(components), $"Missing key component: {KeyConstants.Id}");

            string timestampStr;
            if (!components.TryGetValue(KeyConstants.Timestamp, out timestampStr))
                throw new ArgumentOutOfRangeException(nameof(components), $"Missing key component: {KeyConstants.Timestamp}");
            DateTimeOffset timestamp = DateTimeOffset.Parse(timestampStr, CultureInfo.InvariantCulture);

            return new VideoConsumptionCard(mediaId, null, null, null, null, null, null, TimeSpan.Zero, null, timestamp, null, null);
        }

        /// <summary>
        ///     Name of the application content was consumed in.
        /// </summary>
        public string AppName { get; }

        /// <summary>
        ///     Duration the content was consumed
        /// </summary>
        public TimeSpan ConsumptionTime { get; }

        /// <summary>
        ///     Uri for the content.
        /// </summary>
        public Uri ContentUri { get; }

        /// <summary>
        ///     Icon for the content.
        /// </summary>
        public Uri IconUri { get; }

        /// <summary>
        ///     The name of the show
        /// </summary>
        public string Show { get; }

        /// <summary>
        ///     The name of the studio
        /// </summary>
        public string Studio { get; }

        /// <summary>
        ///     The title of the show
        /// </summary>
        public string Title { get; }

        public VideoConsumptionCard(
            string mediaId,
            string title,
            string show,
            string studio,
            Uri iconUri,
            Uri contentUri,
            string appName,
            TimeSpan consumptionTime,
            DateTimeOffset timestamp,
            IList<string> deviceIds,
            IList<string> sources)
            : this(mediaId, title, show, studio, iconUri, contentUri, appName, consumptionTime, null, timestamp, deviceIds, sources)
        {
        }

        [JsonConstructor]
        private VideoConsumptionCard(
            string mediaId,
            string title,
            string show,
            string studio,
            Uri iconUri,
            Uri contentUri,
            string appName,
            TimeSpan consumptionTime,
            string id,
            DateTimeOffset timestamp,
            IList<string> deviceIds,
            IList<string> sources)
            : base(mediaId, id, timestamp, deviceIds, sources)
        {
            this.Studio = studio;
            this.Title = title;
            this.Show = show;
            this.IconUri = iconUri;
            this.ContentUri = contentUri;
            this.AppName = appName;
            this.ConsumptionTime = consumptionTime;
        }

        protected override IDictionary<string, string> GetKeyComponents()
        {
            return new Dictionary<string, string>
            {
                { KeyConstants.Id, this.MediaId },
                { KeyConstants.Timestamp, this.Timestamp.ToString("o", CultureInfo.InvariantCulture) }
            };
        }
    }
}
