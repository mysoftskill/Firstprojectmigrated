// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;
    using Microsoft.Azure.Storage.Blob;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Export Status Record Helper provides CRUD on the status record for the export request
    /// </summary>
    public class ExportStatusRecordHelper : SingleRecordBlobHelper<ExportStatusRecord>, IExportStatusRecordHelper
    {
        private const string ExportStatusContainerName = "v2exportstatus";

        /// <summary>
        ///     constructor
        /// </summary>
        /// <param name="client"></param>
        /// <param name="log"></param>
        public ExportStatusRecordHelper(CloudBlobClient client, ILogger log)
            : base(client, ExportStatusContainerName, log)
        {
        }

        /// <summary>
        ///     Get a Status Record
        /// </summary>
        /// <param name="allowNotFound"></param>
        /// <returns></returns>
        public async Task<ExportStatusRecord> GetStatusRecordAsync(bool allowNotFound)
        {
            return await this.GetRecordAsync(allowNotFound).ConfigureAwait(false);
        }

        /// <summary>
        ///     List Status Record in Date ascending order
        /// </summary>
        /// <param name="top">the first n records in ascending/descending order</param>
        /// <returns></returns>
        public async Task<IList<ExportStatusRecord>> ListStatusRecordsAscendingAsync(int top)
        {
            return await this.ListRecordsAscendingAsync(null, top).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<string> UpsertStatusRecordAsync(ExportStatusRecord record)
        {
            return await this.UpsertRecordAsync(record).ConfigureAwait(false);
        }
    }
}
