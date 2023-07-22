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
    ///     The card representing a book consumption event
    /// </summary>
    public class BookConsumptionCard : ContentConsumptionCard
    {
        /// <summary>
        ///     Build a card from serialized key components
        /// </summary>
        public static BookConsumptionCard FromKeyComponents(IDictionary<string, string> components)
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

            return new BookConsumptionCard(mediaId, null, null, null, null, null, null, TimeSpan.Zero, null, timestamp, null, null);
        }

        /// <summary>
        ///     Name of the application content was consumed in.
        /// </summary>
        public string AppName { get; }

        /// <summary>
        ///     Book author
        /// </summary>
        public string Author { get; }

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
        ///     Name of the book series.
        /// </summary>
        public string Series { get; }

        /// <summary>
        ///     Name of the book.
        /// </summary>
        public string Title { get; }

        public BookConsumptionCard(
            string mediaId,
            string title,
            string author,
            string series,
            Uri iconUri,
            Uri contentUri,
            string appName,
            TimeSpan consumptionTime,
            DateTimeOffset timestamp,
            IList<string> deviceIds,
            IList<string> sources)
            : this(mediaId, title, author, series, iconUri, contentUri, appName, consumptionTime, null, timestamp, deviceIds, sources)
        {
        }

        [JsonConstructor]
        private BookConsumptionCard(
            string mediaId,
            string title,
            string author,
            string series,
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
            this.Author = author;
            this.Title = title;
            this.Series = series;
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
