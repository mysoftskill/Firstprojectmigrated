// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models
{
    using System;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;

    /// <summary>
    /// GetHistoryBaseFilterArgs
    /// </summary>
    public abstract class GetHistoryBaseFilterArgs : PrivacyExperienceClientBaseArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetHistoryBaseFilterArgs"/> class.
        /// </summary>
        /// <param name="userProxyTicket">The user proxy ticket.</param>
        protected GetHistoryBaseFilterArgs(string userProxyTicket) : base(userProxyTicket)
        {
        }

        /// <summary>
        /// Gets or sets the type of the order by.
        /// </summary>
        public OrderByType? OrderByType { get; set; }

        /// <summary>
        /// Gets or sets the date option.
        /// </summary>
        public DateOption? DateOption { get; set; }

        /// <summary>
        /// Gets or sets the start date.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Creates the query string collection from the date filter arguments.
        /// </summary>
        /// <returns>A new instance of <see cref="QueryStringCollection"/> created from the date filter arguments.</returns>
        internal QueryStringCollection CreateQueryStringCollection()
        {
            var queryParameters = new QueryStringCollection();
            queryParameters.AddDateFilter(this.DateOption, this.StartDate, this.EndDate);
            queryParameters.AddOrderBy(this.OrderByType);
            return queryParameters;
        }
    }
}