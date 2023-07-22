// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers
{
    using System;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Web;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.Converters;

    /// <summary>
    ///     A collection of query parameters
    /// </summary>
    public sealed class QueryStringCollection : NameValueCollection
    {
        private const string FilterParam = "$filter";

        private const string OrderByParam = "$orderBy";

        private const string SearchParam = "$search";

        /// <summary>
        ///     This is equivelent to the sortable format 's' described at: https://msdn.microsoft.com/en-us/library/az4se3k1(v=vs.110).aspx
        ///     But with the addition of '.'fff to the end. This is to get millisecond precision when formatting DateTime into OData style
        ///     datetime strings described here: https://www.odata.org/documentation/odata-version-2-0/overview/
        /// </summary>
        private const string SortableWithFractionalFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff";

        /// <summary>
        ///     Adds a date filter
        /// </summary>
        /// <param name="dateOption">Date Option. Start/End date parameters may be required. (required)</param>
        /// <param name="startDate">State date/time (optional)</param>
        /// <param name="endDate">End date/time (optional)</param>
        public void AddDateFilter(DateOption? dateOption, DateTime? startDate, DateTime? endDate)
        {
            if (!dateOption.HasValue)
                return;

            const string SingleDateFilterTemplate = "date eq datetime'{0}'";
            const string BetweenDateFilterTemplate = "date ge datetime'{0}' and date le datetime'{1}'";

            startDate.ThrowOnNull("startDate");

            if (dateOption.Value == DateOption.SingleDay)
            {
                string dateFilter = string.Format(
                    CultureInfo.InvariantCulture,
                    SingleDateFilterTemplate,
                    startDate.Value.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                if (this.Get(FilterParam) != null)
                    throw new ArgumentException("Cannot add date filter with existing filter already set");
                this.Add(FilterParam, dateFilter);
            }
            else if (dateOption.Value == DateOption.Between)
            {
                endDate.ThrowOnNull("endDate");
                string dateFilter = string.Format(
                    CultureInfo.InvariantCulture,
                    BetweenDateFilterTemplate,
                    startDate.Value.ToUniversalTime().ToString(SortableWithFractionalFormat, CultureInfo.InvariantCulture),
                    endDate.Value.ToUniversalTime().ToString(SortableWithFractionalFormat, CultureInfo.InvariantCulture));
                if (this.Get(FilterParam) != null)
                    throw new ArgumentException("Cannot add date filter with existing filter already set");
                this.Add(FilterParam, dateFilter);
            }
        }

        /// <summary>
        ///     Add an order by
        /// </summary>
        /// <param name="orderBy">The order by type.</param>
        public void AddOrderBy(OrderByType orderBy)
        {
            if (this.Get(OrderByParam) != null)
                throw new ArgumentException("Cannot add second order by, already set");

            switch (orderBy)
            {
                case OrderByType.DateTime:
                    this.Add(OrderByParam, "dateTime");
                    break;
                case OrderByType.SearchTerms:
                    this.Add(OrderByParam, "searchTerms");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderBy), orderBy, null);
            }
        }

        public void AddSearch(string search)
        {
            if (string.IsNullOrEmpty(search))
                return;

            if (this.Get(SearchParam) != null)
                throw new ArgumentException("Cannot add search with existing search already set");

            this.Add(SearchParam, search);
        }
    }
}
