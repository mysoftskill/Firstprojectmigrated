// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;

    /// <summary>
    ///     Export Storage Provider is a singleton class which provides interfaces to manage storage and queuing for the Export feature.
    /// </summary>
    public interface IExportStorageProvider
    {
        /// <summary>
        ///     Gets or sets the ExportQueue interface
        /// </summary>
        IExportQueue ExportCreationQueue { get; }

        /// <summary>
        ///     Gets or sets the ExportQueue interface
        /// </summary>
        IExportQueue ExportArchiveDeletionQueue { get; }

        /// <summary>
        ///     Clean up a batch of old status records and the storage associated with them
        /// </summary>
        /// <param name="olderThan"></param>
        /// <param name="top"></param>
        /// <returns></returns>
        Task<IList<ExportStatusRecord>> CleanupBatchAsync(DateTime olderThan, int top);

        /// <summary>
        ///     Clean up old storage
        /// </summary>
        /// <param name="oldestStorage">export storage older than this is deleted</param>
        /// <param name="maxSeconds">max seconds before loop is forced to end</param>
        /// <param name="maxCleanupIterations">max number of iterations</param>
        /// <param name="maxStatusRecordsToCleanupPerIteration">max status records (and associated storage) to cleanup per iteration</param>
        /// <param name="cleanupIterationDelayMilliseconds">delay injected at the end of each loop</param>
        /// <param name="cancellationToken">thread cancellation token</param>
        /// <returns></returns>
        Task<int> CleanupOldStorageAsync(
            DateTime oldestStorage,
            int maxSeconds,
            int maxCleanupIterations,
            int maxStatusRecordsToCleanupPerIteration,
            int cleanupIterationDelayMilliseconds,
            CancellationToken cancellationToken);

        /// <summary>
        ///     Creates a status helper class to perform CRUD on a status record
        /// </summary>
        /// <param name="exportId"></param>
        /// <returns></returns>
        Task<IExportStatusRecordHelper> CreateExportStatusHelperAsync(string exportId);

        /// <summary>
        ///     Creates a helper class that facilitates writing/reading files to the export request staging container
        /// </summary>
        /// <param name="id"></param>
        /// <param name="exportId"></param>
        /// <returns></returns>
        Task<IExportStagingStorageHelper> CreateStagingStorageHelperAsync(string id, string exportId);

        /// <summary>
        ///     Creates a helper to manage history records for a user
        /// </summary>
        /// <param name="puidStr"></param>
        /// <returns></returns>
        Task<IExportHistoryRecordHelper> CreateStatusHistoryRecordHelperAsync(string puidStr);

        /// <summary>
        ///     Creates a helper to manage reading/writing to the export zip file
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<IExportZipStorageHelper> CreateZipStorageHelperAsync(long id);

        /// <summary>
        ///     initializes the storage helper
        /// </summary>
        /// <param name="serviceConfig"></param>
        /// <returns></returns>
        Task InitializeAsync(IPrivacyExperienceServiceConfiguration serviceConfig);
    }
}
