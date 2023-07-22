// -------------------------------------------------------------------------
// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Table entity for tracking daily workers
    /// </summary>
    public class DailyWorkerTracking : TableEntityBase
    {
        public const string TableName = "DailyWorkerTracking";
        public const string RowQualifier = "Date";
        public const string DateFormat = "yyyy-MM-dd";

        // Field names in the row property bag
        public const string WorkStartedTimeProperty = "WorkStartedTime";
        public const string WorkCompletedProperty = "WorkCompleted";

        /// <summary>
        /// Constructor
        /// </summary>
        public DailyWorkerTracking()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="workItemName">Work item name</param>
        /// <param name="date">Date for the work</param>
        public DailyWorkerTracking(string workItemName, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(workItemName))
            {
                throw new ArgumentNullException("workItemName");
            }

            this.WorkItemName = workItemName;
            this.Date = date;
        }

        /// <summary>
        /// Work item name
        /// </summary>
        public string WorkItemName
        {
            get { return this.Entity.GetPartitionKeyString(); }
            set { this.Entity.SetPartitionKey(value); }
        }

        /// <summary>
        /// Date for the work
        /// </summary>
        public DateTime? Date
        {
            get { return ConvertToDateTime(this.Entity.GetUnqualifiedRowKey(RowQualifier)); }
            set { this.Entity.SetQualifiedRowKey(RowQualifier, ConvertDateTimeToRowKey(value)); }
        }

        /// <summary>
        /// Time the last worker started the work
        /// </summary>
        public DateTimeOffset? WorkStartedTime
        {
            get { return this.Entity.GetDateTimeOffset(WorkStartedTimeProperty); }
            set { this.Entity.Set(WorkStartedTimeProperty, value); }
        }

        /// <summary>
        /// True if the work has been completed for this date
        /// </summary>
        public bool? WorkCompleted
        {
            get { return this.Entity.GetBool(WorkCompletedProperty); }
            set { this.Entity.Set(WorkCompletedProperty, value); }
        }

        private static DateTime? ConvertToDateTime(string rowKey)
        {
            DateTime date;
            if (DateTime.TryParseExact(rowKey, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date))
            {
                return date.ToUniversalTime();
            }

            return null;
        }

        public static string ConvertDateTimeToRowKey(DateTime? date)
        {
            if (date.HasValue)
            {
                if (date.Value.Kind != DateTimeKind.Utc)
                {
                    throw new ArgumentException("Date must be specified as a UTC date");
                }

                if ((date.Value - date.Value.Date).Ticks != 0)
                {
                    throw new ArgumentException("Date specified must be a pure date, no time component is allowed");
                }

                return date.Value.ToString(DateFormat, CultureInfo.InvariantCulture);
            }

            return null;
        }
    }
}
