// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2
{
    using System;

    /// <summary>
    /// Recurring deletes response
    /// List the recurring deletes for a user with
    ///  - when was the last delete initiated
    ///  - when will the next one be initiated
    /// </summary>
    public class GetRecurringDeleteResponse
    {
        /// <summary>
        /// User PUID.
        /// </summary>
        public long PuidValue { get; }

        /// <summary>
        /// Data type.
        /// </summary>
        public string DataType { get; }

        /// <summary>
        /// When recurrent delete record created.
        /// </summary>
        public DateTimeOffset CreateDate { get; }

        /// <summary>
        /// When recurrent delete record updated.
        /// </summary>
        public DateTimeOffset UpdateDate { get; }

        /// <summary>
        /// When was the last delete initiated.
        /// </summary>
        public DateTimeOffset? LastDeleteOccurrence { get; }

        /// <summary>
        /// When will the next one be initiated.
        /// </summary>
        public DateTimeOffset? NextDeleteOccurrence { get; }

        /// <summary>
        /// When was the last succeded delete initiated.
        /// </summary>
        public DateTimeOffset? LastSucceededDeleteOccurrence { get; }

        /// <summary>
        /// Number of retries.
        /// </summary>
        public int NumberOfRetries { get; }

        /// <summary>
        /// Max Number of retries. When NumberOfRetries reach MaxNumberOfRetries the schedule will be paused.
        /// </summary>
        public int MaxNumberOfRetries { get; }

        /// <summary>
        /// Recurrent delete status
        /// </summary>
        public RecurrentDeleteStatus Status { get; }

        /// <summary>
        /// Recurring deletes period in days (2, 30, 90, 180).
        /// </summary>
        public RecurringIntervalDays RecurringIntervalDays { get; }

        /// <summary>
        /// Create GetRecurringDeleteResponse.
        /// </summary>
        /// <param name="puidValue"></param>
        /// <param name="dataType">Data type.</param>
        /// <param name="createDate">Create Date</param>
        /// <param name="updateDate">Update date</param>
        /// <param name="lastDeleteOccurrence">When was the last delete initiated.</param>
        /// <param name="nextDeleteOccurrence">When was the next delete initiated.</param>
        /// <param name="lastSucceededDeleteOccurrence">When was the last succedded delete initiated.</param>
        /// <param name="numberOfRetries">Current number of retries.</param>
        /// <param name="maxNumberOfRetries">Max number of retries.</param>
        /// <param name="status">Current recurrent delete status.</param>
        /// <param name="recurringIntervalDays">Recurring deletes period in days (2, 30, 90, 180).</param>
        public GetRecurringDeleteResponse(long puidValue,
                                        string dataType,
                                        DateTimeOffset createDate,
                                        DateTimeOffset updateDate,
                                        DateTimeOffset? lastDeleteOccurrence,
                                        DateTimeOffset? nextDeleteOccurrence,
                                        DateTimeOffset? lastSucceededDeleteOccurrence,
                                        int numberOfRetries,
                                        int maxNumberOfRetries,
                                        RecurrentDeleteStatus status, 
                                        RecurringIntervalDays recurringIntervalDays)
        {
            if (string.IsNullOrEmpty(dataType))
            {
                throw new ArgumentNullException(nameof(dataType));
            }

            this.PuidValue = puidValue;
            this.DataType = dataType;
            this.CreateDate = createDate;
            this.UpdateDate = updateDate;
            this.LastDeleteOccurrence = lastDeleteOccurrence;
            this.NextDeleteOccurrence = nextDeleteOccurrence;
            this.LastSucceededDeleteOccurrence = lastSucceededDeleteOccurrence;
            this.NumberOfRetries = numberOfRetries;
            this.MaxNumberOfRetries = maxNumberOfRetries;
            this.Status = status;
            this.RecurringIntervalDays = recurringIntervalDays;
        }
    }
}
