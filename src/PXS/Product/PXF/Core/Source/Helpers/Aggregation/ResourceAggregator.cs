// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;

    /// <summary>
    /// ResourceAggregator is responsible for aggregating resources
    /// </summary>
    public static class ResourceAggregator
    {
        public static bool Aggregate(BrowseHistoryV1 aggregatedResult, BrowseHistoryV1 additionalResult, TimeSpan timeZoneOffset)
        {
            if (aggregatedResult.NavigatedToUrl != additionalResult.NavigatedToUrl ||
                aggregatedResult.DateTime.ToOffset(timeZoneOffset).Date != additionalResult.DateTime.ToOffset(timeZoneOffset).Date)
            {
                return false;
            }

            foreach (var newId in additionalResult.Ids)
                aggregatedResult.Ids.Add(newId);

            if (aggregatedResult.Ids.Count > 1)
                aggregatedResult.IsAggregate = true;

            bool preAggregated = false;
            foreach (KeyValuePair<string, int> pair in additionalResult.AggregateCountByPartner)
            {
                preAggregated = true;
                if (!aggregatedResult.AggregateCountByPartner.ContainsKey(pair.Key))
                    aggregatedResult.AggregateCountByPartner[pair.Key] = 0;
                aggregatedResult.AggregateCountByPartner[pair.Key] += pair.Value;
            }

            if (!preAggregated)
            {
                if (!aggregatedResult.AggregateCountByPartner.ContainsKey(additionalResult.PartnerId))
                    aggregatedResult.AggregateCountByPartner[additionalResult.PartnerId] = 0;
                aggregatedResult.AggregateCountByPartner[additionalResult.PartnerId] += additionalResult.Ids.Count;
            }

            aggregatedResult.AggregateCount = aggregatedResult.AggregateCountByPartner.Values.Max();

            return true;
        }

        public static bool Aggregate(SearchHistoryV1 aggregatedResult, SearchHistoryV1 additionalResult, TimeSpan timeZoneOffset)
        {
            if (aggregatedResult == null)
                throw new ArgumentNullException(nameof(aggregatedResult));
            if (additionalResult == null)
                throw new ArgumentNullException(nameof(additionalResult));

            if (aggregatedResult.SearchTerms != additionalResult.SearchTerms ||
                aggregatedResult.DateTime.ToOffset(timeZoneOffset).Date != additionalResult.DateTime.ToOffset(timeZoneOffset).Date)
            {
                return false;
            }

            foreach (var newId in additionalResult.Ids)
                aggregatedResult.Ids.Add(newId);

            if (aggregatedResult.Ids.Count > 1)
                aggregatedResult.IsAggregate = true;

            if (additionalResult.NavigatedToUrls != null)
            {
                if (aggregatedResult.NavigatedToUrls == null)
                    aggregatedResult.NavigatedToUrls = new List<NavigatedToUrlV1>();

                foreach (var url in additionalResult.NavigatedToUrls)
                {
                    bool inserted = false;
                    for (int i = 0; i < aggregatedResult.NavigatedToUrls.Count; i++)
                    {
                        if (url.DateTime > aggregatedResult.NavigatedToUrls[i].DateTime)
                        {
                            inserted = true;
                            aggregatedResult.NavigatedToUrls.Insert(i, url);
                            break;
                        }
                    }
                    if (!inserted)
                        aggregatedResult.NavigatedToUrls.Add(url);
                }
            }

            return true;
        }

        /// <summary>
        /// Aggregates the specified <see cref="List{LocationHistoryV1}" />.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="matchDistance">The match distance.</param>
        public static void Aggregate(ref IList<LocationHistoryV1> items, int matchDistance)
        {
            if (items == null)
            {
                return;
            }

            if (matchDistance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(matchDistance), "Match distance must be positive.");
            }

            // Order the list by distance from center.
            var orderedList = items
                .Select(item => new Node(item))
                .OrderBy(i => i.DistanceFromCenter).ToList();

            var clusterer = new SlidingCircleClusterGenerator(matchDistance);

            foreach (Node node in orderedList)
            {
                ClusterNode<LocationHistoryV1> clusterNode = new ClusterNode<LocationHistoryV1>(node.Latitude, node.Longitude, node.DistanceFromCenter, node.Properties, node.PartnerId, node.DeviceId);
                clusterer.AddNode(clusterNode);
            }

            clusterer.Complete();
            items = clusterer.GetCompletedClusteredNodes();
        }

        /// <summary>
        /// Adjusts the invalid minimum time value to offset the offset.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="timeZoneOffset">The time zone offset.</param>
        /// <remarks>
        /// This is done because partners may send us invalid DateTime data (and they have for browse history).
        /// We apply the timezone offset of the users' time zone prior to aggregation.
        /// So this is a workaround to prevent us from throwing exceptions when we do aggregation on users data where aggregation happens according to time.
        /// </remarks>
        public static void AdjustInvalidMinTimeValue<T>(T resource, TimeSpan timeZoneOffset) where T : ResourceV1
        {
            if (resource != null && resource.DateTime.Equals(DateTimeOffset.MinValue) && timeZoneOffset < TimeSpan.Zero)
            {
                TimeSpan offset = new TimeSpan(Math.Abs(timeZoneOffset.Hours), timeZoneOffset.Minutes, timeZoneOffset.Seconds);
                resource.DateTime = resource.DateTime.Add(offset);
            }
        }
    }
}