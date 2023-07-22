// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Newtonsoft.Json;

    /// <summary>
    ///     The card representing a browse event
    /// </summary>
    public class BrowseCard : TimelineCard
    {
        /// <summary>
        ///     A navigation of the browse domain
        /// </summary>
        public class Navigation
        {
            /// <summary>
            ///     Timestamp of the navigation.
            /// </summary>
            public IList<DateTimeOffset> Timestamps { get; }

            /// <summary>
            ///     Title of the page navigated to
            /// </summary>
            public string Title { get; }

            /// <summary>
            ///     Url of the page navigated to
            /// </summary>
            public Uri Uri { get; }

            /// <summary>
            ///     The hash of the Url
            /// </summary>
            public string UriHash { get; }

            public Navigation(string uriHash, IList<DateTimeOffset> timestamps, string title, Uri uri)
            {
                this.UriHash = uriHash;
                this.Title = title;
                this.Uri = uri;
                this.Timestamps = timestamps;
            }
        }

        /// <summary>
        ///     Max aggregation within a single card.
        /// </summary>
        private const int MaxAggregationCount = 60;

        public static bool Aggregate(TimeSpan timeZoneOffset, BrowseCard a, BrowseCard b)
        {
            if ((a.Navigations?.Sum(n => n.Timestamps?.Count ?? 0) ?? 0) >= MaxAggregationCount)
                return false;

            if (!string.Equals(a.Domain, b.Domain, StringComparison.OrdinalIgnoreCase) ||
                a.Timestamp.ToOffset(timeZoneOffset).Date != b.Timestamp.ToOffset(timeZoneOffset).Date)
                return false;

            foreach (Navigation bNavigation in b.Navigations)
            {
                Navigation matchingNav = a.Navigations.FirstOrDefault(n => n.Uri == bNavigation.Uri);
                if (matchingNav == null)
                    a.Navigations.Add(bNavigation);
                else
                {
                    foreach (DateTimeOffset bTimestamp in bNavigation.Timestamps)
                        matchingNav.Timestamps.Add(bTimestamp);
                }
            }

            return true;
        }

        public static TimelineCard FromKeyComponents(IDictionary<string, string> components)
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));

            string idStr;
            if (!components.TryGetValue(KeyConstants.Id, out idStr))
                throw new ArgumentOutOfRangeException(nameof(components), $"Missing key component: {KeyConstants.Id}");
            string[] uriHashes = idStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(i => i == "null" ? null : i).ToArray();

            string timestampsStr;
            if (!components.TryGetValue(KeyConstants.Timestamp, out timestampsStr))
                throw new ArgumentOutOfRangeException(nameof(components), $"Missing key component: {KeyConstants.Timestamp}");
            List<DateTimeOffset>[] timestamps = timestampsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Split(new[] { '~' }, StringSplitOptions.RemoveEmptyEntries).Select(s => DateTimeOffset.Parse(s, CultureInfo.InvariantCulture)).ToList())
                .ToArray();

            return new BrowseCard(null, uriHashes.Zip(timestamps, (i, t) => new Navigation(i, t, null, null)).ToList(), timestamps[0][0], null, null);
        }

        /// <summary>
        ///     Browse Domain
        /// </summary>
        public string Domain { get; }

        /// <summary>
        ///     Navigations in this domain.
        /// </summary>
        public IList<Navigation> Navigations { get; }

        public BrowseCard(string domain, IList<Navigation> navigations, DateTimeOffset timestamp, IList<string> deviceIds, IList<string> sources)
            : this(domain, navigations, null, timestamp, deviceIds, sources)
        {
        }

        [JsonConstructor]
        private BrowseCard(string domain, IList<Navigation> navigations, string id, DateTimeOffset timestamp, IList<string> deviceIds, IList<string> sources)
            : base(id, timestamp, deviceIds, sources)
        {
            this.Domain = domain;
            this.Navigations = navigations;
        }

        protected override IDictionary<string, string> GetKeyComponents()
        {
            return new Dictionary<string, string>
            {
                { KeyConstants.Id, string.Join(",", this.Navigations.Select(n => string.IsNullOrEmpty(n.UriHash) ? "null" : n.UriHash)) },
                {
                    KeyConstants.Timestamp,
                    string.Join(",", this.Navigations.Select(n => string.Join("~", n.Timestamps.Select(t => t.ToString("o", CultureInfo.InvariantCulture)))))
                }
            };
        }
    }
}
