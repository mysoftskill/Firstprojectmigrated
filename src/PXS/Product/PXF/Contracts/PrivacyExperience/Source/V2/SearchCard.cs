// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;

    /// <summary>
    ///     A timeline card for representing searches
    /// </summary>
    public class SearchCard : TimelineCard
    {
        /// <summary>
        ///     A navigation off the search results
        /// </summary>
        public class Navigation
        {
            /// <summary>
            ///     Timestamp of the navigation.
            /// </summary>
            public DateTimeOffset Timestamp { get; }

            /// <summary>
            ///     Title of the page navigated to
            /// </summary>
            public string Title { get; }

            /// <summary>
            ///     Url of the page navigated to
            /// </summary>
            public Uri Uri { get; }

            public Navigation(string title, Uri uri, DateTimeOffset timestamp)
            {
                this.Title = title;
                this.Uri = uri;
                this.Timestamp = timestamp;
            }
        }

        /// <summary>
        ///     Max aggregation within a single card.
        /// </summary>
        private const int MaxAggregationCount = 1000;

        public static bool Aggregate(TimeSpan timeZoneOffset, SearchCard a, SearchCard b)
        {
            if ((a.Navigations?.Count ?? 0) >= MaxAggregationCount)
                return false;

            if (a.Search != b.Search || a.Timestamp.ToOffset(timeZoneOffset).Date != b.Timestamp.ToOffset(timeZoneOffset).Date)
                return false;

            a.Navigations = a.Navigations.Concat(b.Navigations).ToList();
            a.ImpressionIds = a.ImpressionIds.Concat(b.ImpressionIds).ToList();
            return true;
        }

        public static TimelineCard FromKeyComponents(IDictionary<string, string> components)
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));

            string impressionIdList;
            if (!components.TryGetValue(KeyConstants.Id, out impressionIdList))
                throw new ArgumentOutOfRangeException(nameof(components), $"Missing key component: {KeyConstants.Id}");

            string[] impressionIds = impressionIdList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            return new SearchCard(null, null, impressionIds, DateTimeOffset.MinValue, null, null);
        }

        /// <summary>
        ///     The list of impression ids.
        /// </summary>
        public IList<string> ImpressionIds { get; set; }

        /// <summary>
        ///     A list of navigation entries off the search this card represents
        /// </summary>
        public IList<Navigation> Navigations { get; set; }

        /// <summary>
        ///     The search term
        /// </summary>
        public string Search { get; }

        public SearchCard(string search, IList<Navigation> navigations, IList<string> impressionIds, DateTimeOffset timestamp, IList<string> deviceIds, IList<string> sources)
            : this(search, navigations, impressionIds, null, timestamp, deviceIds, sources)
        {
        }

        [JsonConstructor]
        private SearchCard(
            string search,
            IList<Navigation> navigations,
            IList<string> impressionIds,
            string id,
            DateTimeOffset timestamp,
            IList<string> deviceIds,
            IList<string> sources)
            : base(id, timestamp, deviceIds, sources)
        {
            this.Search = search;
            this.Navigations = navigations;
            this.ImpressionIds = impressionIds;
        }

        protected override IDictionary<string, string> GetKeyComponents()
        {
            return new Dictionary<string, string>
            {
                { KeyConstants.Id, string.Join(",", this.ImpressionIds) }
            };
        }
    }
}
