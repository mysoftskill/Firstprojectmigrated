// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary
{
    using System;
    using System.Collections.Specialized;
    using System.Globalization;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Extensions;

    /// <summary>
    /// A collection of query parameters
    /// </summary>
    public sealed class QueryStringCollection : NameValueCollection
    {
        private const string FilterParam = "filter";
        private const string SkipParam = "skip";
        private const string CountParam = "count";
        private const string OrderByParam = "orderby";
        private const string DistanceParam = "distance";
        private const string TimeZoneOffsetParam = "timeZoneOffset";
        private const string DisableThrottlingParam = "disableThrottling";
        
        /// <summary>
        /// This is equivelent to the sortable format 's' described at: https://msdn.microsoft.com/en-us/library/az4se3k1(v=vs.110).aspx
        /// But with the addition of '.'fff to the end. This is to get millisecond precision when formatting DateTime into OData style
        /// datetime strings described here: https://www.odata.org/documentation/odata-version-2-0/overview/
        /// </summary>
        private const string SortableWithFractionalFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff";

        /// <summary>
        /// Adds a date filter
        /// </summary>
        /// <param name="dateOption">Date Option. Start/End date parameters may be required. (optional)</param>
        /// <param name="startDate">State date/time (optional)</param>
        /// <param name="endDate">End date/time (optional)</param>
        public void AddDateFilter(DateOption? dateOption, DateTime? startDate, DateTime? endDate)
        {
            const string SingleDateFilterTemplate = "date eq datetime'{0}'";
            const string BeforeDateTimeFilterTemplate = "datetime le datetime'{0}'";
            const string AfterDateTimeFilterTemplate = "datetime ge datetime'{0}'";
            const string BetweenDateFilterTemplate = "date ge datetime'{0}' and date le datetime'{1}'";

            if (dateOption != null)
            {
                startDate.ThrowOnNull("startDate");

                if (dateOption.Value == DateOption.SingleDay)
                {
                    var dateFilter = string.Format(
                        CultureInfo.InvariantCulture,
                        SingleDateFilterTemplate,
                        startDate.Value.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    this.Add(FilterParam, dateFilter);
                }
                else if (dateOption.Value == DateOption.Before)
                {
                    var dateFilter = string.Format(
                        CultureInfo.InvariantCulture,
                        BeforeDateTimeFilterTemplate,
                        startDate.Value.ToUniversalTime().ToString(SortableWithFractionalFormat, CultureInfo.InvariantCulture));
                    this.Add(FilterParam, dateFilter);
                }
                else if (dateOption.Value == DateOption.After)
                {
                    var dateFilter = string.Format(
                        CultureInfo.InvariantCulture,
                        AfterDateTimeFilterTemplate,
                        startDate.Value.ToUniversalTime().ToString(SortableWithFractionalFormat, CultureInfo.InvariantCulture));
                    this.Add(FilterParam, dateFilter);
                }
                else if (dateOption.Value == DateOption.Between)
                {
                    endDate.ThrowOnNull("endDate");
                    var dateFilter = string.Format(
                        CultureInfo.InvariantCulture,
                        BetweenDateFilterTemplate,
                        startDate.Value.ToUniversalTime().ToString(SortableWithFractionalFormat, CultureInfo.InvariantCulture),
                        endDate.Value.ToUniversalTime().ToString(SortableWithFractionalFormat, CultureInfo.InvariantCulture));
                    this.Add(FilterParam, dateFilter);
                }
            }
        }

        /// <summary>
        /// Adds a skip parameter
        /// </summary>
        /// <param name="skip">Skip count</param>
        public void AddSkip(int? skip)
        {
            if (skip.HasValue)
            {
                this.Add(SkipParam, skip.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Adds a page count
        /// </summary>
        /// <param name="count">Page count</param>
        public void AddCount(int count)
        {
            if (count > 0)
            {
                this.Add(CountParam, count.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Adds an order by parameter
        /// </summary>
        /// <param name="orderByType">Order by type</param>
        public void AddOrderBy(OrderByType? orderByType)
        {
            if (orderByType.HasValue)
            {
                this.Add(OrderByParam, orderByType.Value.ToString());
            }
        }

        /// <summary>
        /// Adds the aggregation distance.
        /// </summary>
        /// <param name="aggregationDistance">The aggregation distance.</param>
        public void AddAggregationDistance(int aggregationDistance)
        {
            this.Add(DistanceParam, aggregationDistance.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Adds the time zone offset.
        /// </summary>
        /// <param name="timeZoneOffset">The time zone offset.</param>
        public void AddTimeZoneOffset(TimeSpan? timeZoneOffset)
        {
            if (timeZoneOffset.HasValue)
            {
                this.Add(TimeZoneOffsetParam, timeZoneOffset.Value.ToString());
            }
        }

        /// <summary>
        /// Adds the disable throttling value.
        /// </summary>
        /// <param name="disableThrottling">The disable throttling value.</param>
        public void AddDisableThrottling(bool? disableThrottling)
        {
            if (disableThrottling.HasValue)
            {
                this.Add(DisableThrottlingParam, disableThrottling.Value.ToString());
            }
        }
    }
}
