// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.ScheduleDbClient.Model
{
    using System;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Newtonsoft.Json;

    /// <summary>
    ///     Represents a record in Schedule DB 
    /// </summary>
    public class RecurrentDeleteScheduleDbDocument
    {
        /// <summary>
        ///     Puid.
        /// </summary>
        public long Puid { get; set; }

        /// <summary>
        ///     CreateDateUtc.
        /// </summary>
        public DateTimeOffset? CreateDateUtc { get; set; }

        /// <summary>
        ///     UpdateDateUtc.
        /// </summary>
        public DateTimeOffset? UpdateDateUtc { get; set; }

        /// <summary>
        ///     DocumentId.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string DocumentId { get; set; }

        /// <summary>
        ///     DataType.
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        ///     RecurringIntervalDays.
        /// </summary>
        public RecurringIntervalDays? RecurringIntervalDays { get; set; }

        /// <summary>
        ///     RecurrentDeleteStatus.
        /// </summary>
        public RecurrentDeleteStatus? RecurrentDeleteStatus { get; set; }

        /// <summary>
        ///     LastDeleteOccurrenceUtc.
        /// </summary>
        public DateTimeOffset? LastDeleteOccurrenceUtc { get; set; }

        /// <summary>
        ///     NextDeleteOccurrenceUtc.
        /// </summary>
        public DateTimeOffset? NextDeleteOccurrenceUtc { get; set; }

        /// <summary>
        ///     LastSucceededDeleteOccurrenceUtc.
        /// </summary>
        public DateTimeOffset? LastSucceededDeleteOccurrenceUtc { get; set; }

        /// <summary>
        ///     NumberOfRetries.
        /// </summary>
        public int? NumberOfRetries { get; set; }

        /// <summary>
        ///     PreVerifier.
        /// </summary>
        public string PreVerifier { get; set; }

        /// <summary>
        ///     PreVerifierExpirationDateUtc.
        /// </summary>
        public DateTimeOffset? PreVerifierExpirationDateUtc { get; set; }

        /// <summary>
        ///     The ETag Property for updates.
        /// </summary>
        [JsonProperty(PropertyName = "_etag")]
        public string ETag { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecurrentDeleteScheduleDbDocument"/> class.
        /// </summary>
        /// <param name="puidValue"></param>
        /// <param name="dataType">Data type.</param>
        /// <param name="documentId">DocumentId.</param>
        /// <param name="preVerifier">PreVerifier.</param>
        /// <param name="preVerifierExpirationDateUtc">PreVerifierExpirationDateUtc.</param>
        /// <param name="createDateUtc">CreateDateUtc</param>
        /// <param name="updateDateUtc">UpdateDateUtc</param>
        /// <param name="lastDeleteOccurrence">When was the last delete initiated.</param>
        /// <param name="nextDeleteOccurrence">When will the next delete be initiated.</param>
        /// <param name="lastSucceededDeleteOccurrence">When was the last succedded delete.</param>
        /// <param name="numberOfRetries">Current number of retries.</param>
        /// <param name="status">Current recurrent delete status.</param>
        /// <param name="recurringIntervalDays">Recurring deletes period in days (2, 30, 90, 180).</param>
        public RecurrentDeleteScheduleDbDocument(long puidValue,
                                        string dataType,
                                        string documentId,
                                        string preVerifier = null,
                                        DateTimeOffset? preVerifierExpirationDateUtc = null,
                                        DateTimeOffset? createDateUtc = null,
                                        DateTimeOffset? updateDateUtc = null,
                                        DateTimeOffset? lastDeleteOccurrence = null,
                                        DateTimeOffset? nextDeleteOccurrence = null,
                                        DateTimeOffset? lastSucceededDeleteOccurrence = null,
                                        int? numberOfRetries = null,
                                        RecurrentDeleteStatus? status = null,
                                        RecurringIntervalDays? recurringIntervalDays = null)
        {
            if (string.IsNullOrEmpty(dataType))
                throw new ArgumentNullException(nameof(dataType));

            this.Puid = puidValue;
            this.DataType = dataType;
            this.DocumentId = documentId;
            this.PreVerifier = preVerifier;
            this.PreVerifierExpirationDateUtc = preVerifierExpirationDateUtc;
            this.CreateDateUtc = createDateUtc;
            this.UpdateDateUtc = updateDateUtc;
            this.LastDeleteOccurrenceUtc = lastDeleteOccurrence;
            this.NextDeleteOccurrenceUtc = nextDeleteOccurrence;
            this.LastSucceededDeleteOccurrenceUtc = lastSucceededDeleteOccurrence;
            this.NumberOfRetries = numberOfRetries;
            this.RecurrentDeleteStatus = status;
            this.RecurringIntervalDays = recurringIntervalDays;
        }

        internal static RecurrentDeleteScheduleDbDocument UpdateRecurringDeletesScheduleDbAsync(RecurrentDeleteScheduleDbDocument existingDocument, RecurrentDeleteScheduleDbDocument newDocument)
        {
            existingDocument.RecurringIntervalDays = newDocument.RecurringIntervalDays.HasValue ? newDocument.RecurringIntervalDays : existingDocument.RecurringIntervalDays;
            existingDocument.RecurrentDeleteStatus = newDocument.RecurrentDeleteStatus.HasValue ? newDocument.RecurrentDeleteStatus : existingDocument.RecurrentDeleteStatus;
            existingDocument.LastDeleteOccurrenceUtc = newDocument.LastDeleteOccurrenceUtc.HasValue ? newDocument.LastDeleteOccurrenceUtc : existingDocument.LastDeleteOccurrenceUtc;
            existingDocument.NextDeleteOccurrenceUtc = newDocument.NextDeleteOccurrenceUtc.HasValue ? newDocument.NextDeleteOccurrenceUtc : existingDocument.NextDeleteOccurrenceUtc;
            existingDocument.LastSucceededDeleteOccurrenceUtc = newDocument.LastSucceededDeleteOccurrenceUtc.HasValue ? newDocument.LastSucceededDeleteOccurrenceUtc : existingDocument.LastSucceededDeleteOccurrenceUtc;
            existingDocument.NumberOfRetries = newDocument.NumberOfRetries.HasValue ? newDocument.NumberOfRetries : existingDocument.NumberOfRetries;
            existingDocument.PreVerifier = !string.IsNullOrEmpty(newDocument.PreVerifier) ? newDocument.PreVerifier : existingDocument.PreVerifier;
            existingDocument.PreVerifierExpirationDateUtc = newDocument.PreVerifierExpirationDateUtc.HasValue ? newDocument.PreVerifierExpirationDateUtc : existingDocument.PreVerifierExpirationDateUtc;
        
            return existingDocument;
        }
    }
}
