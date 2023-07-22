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
    ///     Card representing App Usage
    /// </summary>
    public class AppUsageCard : TimelineCard
    {
        /// <summary>
        ///     Build an AppUsage card from key components.
        /// </summary>
        public static AppUsageCard FromKeyComponents(IDictionary<string, string> components)
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));

            if (!components.TryGetValue(KeyConstants.Id, out string appId))
                throw new ArgumentOutOfRangeException(nameof(components), $"Missing key component: {KeyConstants.Id}");

            if (!components.TryGetValue(KeyConstants.Timestamp, out string timestampStr))
                throw new ArgumentOutOfRangeException(nameof(components), $"Missing key component: {KeyConstants.Timestamp}");
            DateTimeOffset timestamp = DateTimeOffset.Parse(timestampStr, CultureInfo.InvariantCulture);

            if (!components.TryGetValue(KeyConstants.EndTimestamp, out timestampStr))
                throw new ArgumentOutOfRangeException(nameof(components), $"Missing key component: {KeyConstants.EndTimestamp}");
            DateTimeOffset endTimestamp = DateTimeOffset.Parse(timestampStr, CultureInfo.InvariantCulture);

            if (!components.TryGetValue(KeyConstants.Aggregation, out string aggregation))
                throw new ArgumentOutOfRangeException(nameof(components), $"Missing key component: {KeyConstants.Aggregation}");

            IDictionary<string, IList<string>> bag = DeserializePropertyBagFromIdKeyComponents(components);

            return new AppUsageCard(appId, aggregation, null, null, null, null, timestamp, endTimestamp, null, null, bag);
        }

        /// <summary>
        ///     The aggregation kind, for example daily or monthly.
        /// </summary>
        public string Aggregation { get; }

        /// <summary>
        ///     The icon background color, in #ffffff format.
        /// </summary>
        public string AppIconBackground { get; }

        /// <summary>
        ///     The uri to the application icon.
        /// </summary>
        public Uri AppIconUri { get; }

        /// <summary>
        ///     The application id.
        /// </summary>
        public string AppId { get; }

        /// <summary>
        ///     The name of the application.
        /// </summary>
        public string AppName { get; }

        /// <summary>
        ///     The application publisher.
        /// </summary>
        public string AppPublisher { get; }

        /// <summary>
        ///     The end timestamp
        /// </summary>
        public DateTimeOffset EndTimestamp { get; }

        public AppUsageCard(
            string appId,
            string aggregation,
            string appIconBackground,
            Uri appIconUri,
            string appName,
            string appPublisher,
            DateTimeOffset timestamp,
            DateTimeOffset endTimestamp,
            IList<string> deviceIds,
            IList<string> sources,
            IDictionary<string, IList<string>> propertyBag)
            : base(null, timestamp, deviceIds, sources, propertyBag)
        {
            this.AppId = appId;
            this.AppIconBackground = appIconBackground;
            this.AppIconUri = appIconUri;
            this.AppName = appName;
            this.AppPublisher = appPublisher;
            this.Aggregation = aggregation;
            this.EndTimestamp = endTimestamp;
        }

        [JsonConstructor]
        private AppUsageCard(
            string appId,
            string aggregation,
            string appIconBackground,
            Uri appIconUri,
            string appName,
            string appPublisher,
            string id,
            DateTimeOffset timestamp,
            DateTimeOffset endTimestamp,
            IList<string> deviceIds,
            IList<string> sources)
            : base(id, timestamp, deviceIds, sources)
        {
            this.AppId = appId;
            this.AppIconBackground = appIconBackground;
            this.AppIconUri = appIconUri;
            this.AppName = appName;
            this.AppPublisher = appPublisher;
            this.Aggregation = aggregation;
            this.EndTimestamp = endTimestamp;
        }

        protected override IDictionary<string, string> GetKeyComponents()
        {
            return new Dictionary<string, string>
            {
                { KeyConstants.Id, this.AppId },
                { KeyConstants.Timestamp, this.Timestamp.ToString("o", CultureInfo.InvariantCulture) },
                { KeyConstants.EndTimestamp, this.EndTimestamp.ToString("o", CultureInfo.InvariantCulture) },
                { KeyConstants.Aggregation, this.Aggregation },
                { KeyConstants.PropertyBag, this.GetSerializedPropertyBag() }
            };
        }
    }
}
