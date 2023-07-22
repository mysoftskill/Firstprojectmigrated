// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models
{
    using System;
    using System.Globalization;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;

    /// <summary>
    /// Update recurring deletes args
    /// </summary>
    public class CreateOrUpdateRecurringDeletesArgs : PrivacyExperienceClientBaseArgs
    {
        /// <summary>
        /// Data type (card type).
        /// </summary>
        public string DataType { get; }

        /// <summary>
        /// Recurrent deletes effective date />
        /// </summary>
        public DateTimeOffset NextDeleteDate { get; }

        /// <summary>
        /// Recurring deletes period in days (2, 30, 90, 180).
        /// </summary>
        public RecurringIntervalDays RecurringIntervalDays { get; }

        /// <summary>
        /// Recurrent delete status
        /// </summary>
        RecurrentDeleteStatus Status { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateOrUpdateRecurringDeletesArgs"/> class.
        /// </summary>
        /// <param name="userProxyTicket">The user proxy ticket.</param>
        /// <param name="dataType">The card types to retrieve that should come from "TimelineCard.CardTypes".</param>
        /// <param name="nextDeleteDate">Next delete date.</param>
        /// <param name="recurringIntervalDays">Recurring deletes period in days (2, 30, 90, 180).</param>
        /// <param name="status">Recurrent delete status.</param>
        public CreateOrUpdateRecurringDeletesArgs(
            string userProxyTicket,
            string dataType,
            DateTimeOffset nextDeleteDate,
            RecurringIntervalDays recurringIntervalDays,
            RecurrentDeleteStatus status = RecurrentDeleteStatus.Active)
            : base(userProxyTicket)
        {
            this.NextDeleteDate = nextDeleteDate;
            this.DataType = dataType;
            this.RecurringIntervalDays = recurringIntervalDays;
            this.Status = status;
        }

        /// <summary>
        ///     Creates the appropriate query string collection
        /// </summary>
        public QueryStringCollection CreateQueryStringCollection()
        {
            var queryString = new QueryStringCollection();
            queryString.Add("dataType", this.DataType);
            queryString.Add("nextDeleteDate", this.NextDeleteDate.ToUniversalTime().ToString(CultureInfo.InvariantCulture));
            queryString.Add("recurringIntervalDays", this.RecurringIntervalDays.ToString());
            queryString.Add("status", this.Status.ToString());

            return queryString;
        }
    }
}
