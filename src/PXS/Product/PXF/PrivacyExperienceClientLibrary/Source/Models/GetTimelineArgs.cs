// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;

    /// <summary>
    ///     Arguments for getting timeline data
    /// </summary>
    public class GetTimelineArgs : PrivacyExperienceClientBaseArgs
    {
        /// <summary>
        ///     The card types to retrieve that should come from <see cref="TimelineCard.CardTypes" />
        /// </summary>
        public IList<string> CardTypes { get; }

        /// <summary>
        ///     The maximum number of results to return per page
        /// </summary>
        public int? Count { get; }

        /// <summary>
        ///     The device ids to filter to
        /// </summary>
        public IList<string> DeviceIds { get; }

        /// <summary>
        ///     The search string to apply to the results
        /// </summary>
        public string Search { get; }

        /// <summary>
        ///     The list of data sources interested in.
        /// </summary>
        public IList<string> Sources { get; }

        /// <summary>
        ///     The date and time from which to start returning results.
        /// </summary>
        public DateTimeOffset StartingAt { get; }

        /// <summary>
        ///     The timezone offset for the user
        /// </summary>
        public TimeSpan TimeZoneOffset { get; }

        /// <summary>
        ///     Constructs the arguments to fetch timeline data
        /// </summary>
        public GetTimelineArgs(
            string userProxyTicket,
            IList<string> cardTypes,
            int? count,
            IList<string> deviceIds,
            IList<string> sources,
            string search,
            TimeSpan timeZoneOffset,
            DateTimeOffset startingAt)
            : base(userProxyTicket)
        {
            if (cardTypes == null)
                throw new ArgumentNullException(nameof(cardTypes));
            if (cardTypes.Count == 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(cardTypes));

            this.CardTypes = cardTypes;
            this.Count = count;
            this.DeviceIds = deviceIds;
            this.Sources = sources;
            this.Search = search;
            this.TimeZoneOffset = timeZoneOffset;
            this.StartingAt = startingAt;
        }

        /// <summary>
        ///     Creates the appropriate query string collection
        /// </summary>
        public QueryStringCollection CreateQueryStringCollection()
        {
            var queryString = new QueryStringCollection();
            queryString.Add("cardTypes", string.Join(",", this.CardTypes));
            if (this.Count > 0)
                queryString.Add("count", this.Count?.ToString());
            if (this.DeviceIds?.Count > 0)
                queryString.Add("deviceIds", string.Join(",", this.DeviceIds));
            if (this.Sources?.Count > 0)
                queryString.Add("sources", string.Join(",", this.Sources));
            if (this.Search != null)
                queryString.Add("search", this.Search);
            queryString.Add("timeZoneOffset", this.TimeZoneOffset.ToString());
            queryString.Add("startingAt", this.StartingAt.ToString("o"));

            return queryString;
        }
    }
}
