// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.ScheduleDbClient
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Exceptions;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Model;

    /// <summary>
    ///     ScheduleDbClient client
    /// </summary>
    public interface IScheduleDbClient
    {
        /// <summary>
        ///     Get recurring delete records from schedule db based on puid
        /// </summary>
        /// <param name="puidValue">Puid.</param>
        /// <param name="cancellationToken">Cancellation token for underlying task</param>
        /// <returns>All existing recurrent delete records from Schedule DB based on puid.</returns>
        Task<IList<RecurrentDeleteScheduleDbDocument>> GetRecurringDeletesScheduleDbAsync(long puidValue, CancellationToken cancellationToken);

        /// <summary>
        ///     Get recurring delete records from schedule db by documentId
        /// </summary>
        /// <param name="documentId">The documentId for the recurrent delete schedule db document.</param>
        /// <param name="cancellationToken">Cancellation token for underlying task</param>
        /// <returns>The recurrent delete schedule db document response if found, null if not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when documentId is null or empty</exception>
        /// <exception cref="ScheduleDbClientException">Thrown when more than one document for document id or the call failed</exception>
        Task<RecurrentDeleteScheduleDbDocument> GetRecurringDeletesScheduleDbDocumentAsync(string documentId, CancellationToken cancellationToken);

        /// <summary>
        ///     Get recurring delete records from schedule db by puid and data type
        /// </summary>
        /// <param name="puidValue">Puid value.</param>
        /// <param name="dataType">Data type.</param>
        /// <param name="cancellationToken">Cancellation token for underlying task</param>
        /// <returns></returns>
        /// <exception cref="ScheduleDbClientException">Thrown when more than one document is found for given puid and data type.</exception>
        Task<RecurrentDeleteScheduleDbDocument> GetRecurringDeletesScheduleDbDocumentAsync(long puidValue, string dataType, CancellationToken cancellationToken);

        /// <summary>
        ///     Delete recurring delete records from schedule db based on puid and datatype
        /// </summary>
        /// <param name="puidValue">Puid.</param>
        /// <param name="dataType">DataType.</param>
        /// <param name="cancellationToken">Cancellation token for underlying task</param>
        /// <returns>Task result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when dataType is null or empty</exception>
        /// <exception cref="ScheduleDbClientException">Thrown when document is not found or more than one document found</exception>
        Task DeleteRecurringDeletesScheduleDbAsync(long puidValue, string dataType, CancellationToken cancellationToken);

        /// <summary>
        ///     Update recurring delete records in schedule db
        /// </summary>
        /// <param name="args">The recurrent delete schedule db document.</param>
        /// <returns>The recurrent delete schedule db document response.</returns>
        /// <exception cref="ArgumentNullException">Thrown when any required arguements is null</exception>
        /// <exception cref="ScheduleDbClientException">Thrown when document to update is not in the latest state or when document creation failed</exception>
        Task<RecurrentDeleteScheduleDbDocument> CreateOrUpdateRecurringDeletesScheduleDbAsync(RecurrentDeleteScheduleDbDocument args);

        /// <summary>
        ///     Checks if requested recurring delete schedule db record exists based on puid and datatype
        /// </summary>
        /// <param name="puidValue">Puid.</param>
        /// <param name="dataType">DataType.</param>
        /// <param name="cancellationToken">Cancellation token for underlying task</param>
        /// <returns>True/False.</returns>
        /// <exception cref="ArgumentNullException">Thrown when dataType is null or empty</exception>
        Task<bool> HasRecurringDeletesScheduleDbRecordAsync(long puidValue, string dataType, CancellationToken cancellationToken);

        /// <summary>
        ///     Deletes all recurring delete records from schedule db for given puid
        /// </summary>
        /// <param name="puidValue">Puid.</param>
        /// <param name="cancellationToken">Cancellation token for underlying task</param>
        /// <returns>Task result.</returns>
        Task DeleteRecurringDeletesByPuidScheduleDbAsync(long puidValue, CancellationToken cancellationToken);

        /// <summary>
        ///     Gets Expired PreVerifier Records for Recurring Deletes based on expiration date 
        /// </summary>
        /// <param name="preVerifierExpirationDate">ExpirationDateTimeOffset.</param>
        /// <param name="continuationToken">ContinuationToken.</param>
        /// <param name="maxItemCount">MaxItemCount.</param>
        /// <returns> The schedule db recurrent delete response and continuation token.</returns>
        /// <exception cref="ArgumentNullException">Thrown when preVerifierExpirationDate is null</exception>
        Task<(IList<RecurrentDeleteScheduleDbDocument>, string continuationToken)> GetExpiredPreVerifiersRecurringDeletesScheduleDbAsync(DateTimeOffset preVerifierExpirationDate, string continuationToken = null, int maxItemCount = 1000);

        /// <summary>
        ///     Gets Applicable Recurring Deletes Records from Schedule Db Async based on expected Next Delete OccuranceUtc 
        /// </summary>
        /// <param name="expectedNextDeleteOccuranceUtc">ExpectedNextDeleteOccuranceUtc.</param>
        /// <param name="continuationToken">ContinuationToken.</param>
        /// <param name="maxItemCount">MaxItemCount.</param>
        /// <returns> The schedule db recurrent delete response and continuation token.</returns>
        /// <exception cref="ArgumentNullException">Thrown when expectedNextDeleteOccuranceUtc is null</exception>
        Task<(IList<RecurrentDeleteScheduleDbDocument>, string continuationToken)> GetApplicableRecurringDeletesScheduleDbAsync(DateTimeOffset expectedNextDeleteOccuranceUtc, string continuationToken = null, int maxItemCount = 1000);
    }
}
