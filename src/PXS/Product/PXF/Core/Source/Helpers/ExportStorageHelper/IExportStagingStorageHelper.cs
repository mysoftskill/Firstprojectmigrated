// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.Azure.Storage;

    /// <summary>
    ///     Export Staging Helper facilitates writing files to the staging container, zipping the container once everything is gathered and then deleting the container.
    ///     This and other export storage interfaces are accessed via the IExportStorageProvider
    ///     This class must be initialized by the storage provider for the puid and requestId.
    /// </summary>
    public interface IExportStagingStorageHelper
    {
        /// <summary>
        ///     Delete the Staging Container and everything under it
        /// </summary>
        /// <returns></returns>
        Task DeleteStagingContainerAsync();

        /// <summary>
        ///     Get a staging file within the staging container given the relative path
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        IExportStagingFile GetStagingFile(string relativePath);

        /// <summary>
        ///     Zip the files under the staging container
        /// </summary>
        Task<CloudBlob> ZipStagingAsync(ICounterFactory counterFactory, string counterCategoryName);

        /// <summary>
        ///     Gets a UserDelegationKey for the blob
        /// </summary>
        /// <param name="start">when the key becomes valid</param>
        /// <param name="end">When the key invalidates(should be seven days or less)</param>
        /// <returns>UserDelegationKey</returns>
        Task<UserDelegationKey> GetUserDelegationKey(DateTimeOffset start, DateTimeOffset end);

        /// <summary>
        ///     Delete the Staging Container and everything under it
        /// </summary>
        /// <returns></returns>
        Task<bool> DeleteStagingContainerAsync(long puid, string requestId);
    }
}
