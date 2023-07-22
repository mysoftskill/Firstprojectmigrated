// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;

    /// <summary>
    ///     Export Status Record Helpder provides CRUD on the status record for the export request
    /// </summary>
    public interface IExportStatusRecordHelper
    {
        /// <summary>
        ///     Get a Status Record
        /// </summary>
        /// <param name="allowNotFound"></param>
        /// <returns></returns>
        Task<ExportStatusRecord> GetStatusRecordAsync(bool allowNotFound);

        /// <summary>
        ///     Initialize and Delete the status record
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        Task<bool> InitializeAndDeleteAsync(string requestId);

        /// <summary>
        ///     List Status Record in Date ascending order
        /// </summary>
        /// <param name="top">the first n records in ascending/descending order</param>
        /// <returns></returns>
        Task<IList<ExportStatusRecord>> ListStatusRecordsAscendingAsync(int top);

        /// <summary>
        ///     Update a status record
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        Task<string> UpsertStatusRecordAsync(ExportStatusRecord record);
    }
}
