// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models
{
    using System;

    /// <summary>
    /// Get-History Arguments Base
    /// </summary>
    public abstract class GetHistoryBaseArgs : GetHistoryBaseFilterArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetHistoryBaseArgs"/> class.
        /// </summary>
        /// <param name="userProxyTicket">The user proxy ticket.</param>
        protected GetHistoryBaseArgs(string userProxyTicket)
            : base(userProxyTicket)
        {
            this.Count = 100;
        }

        /// <summary>
        /// Gets or sets the count.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets the time zone offset.
        /// </summary>
        public TimeSpan? TimeZoneOffset { get; set; }

        /// <summary>
        /// Creates the query string collection from the date filter arguments.
        /// </summary>
        /// <returns>A new instance of <see cref="QueryStringCollection"/> created from the date filter arguments.</returns>
        internal new QueryStringCollection CreateQueryStringCollection()
        {
            var queryParameters = new QueryStringCollection();
            queryParameters.AddDateFilter(this.DateOption, this.StartDate, this.EndDate);
            queryParameters.AddCount(this.Count);
            queryParameters.AddOrderBy(this.OrderByType);
            queryParameters.AddTimeZoneOffset(this.TimeZoneOffset);
            return queryParameters;
        }
    }
}