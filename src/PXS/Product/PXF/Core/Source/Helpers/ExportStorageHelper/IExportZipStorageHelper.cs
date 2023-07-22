// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper
{
    using System.IO;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ZipWriter;
    using Microsoft.Azure.Storage.Blob;

    /// <summary>
    ///     Export Zip Storage facilitates Writing and getting the zip file within the zip container.
    ///     This and other export storage interfaces are accessed via the IExportStorageProvider
    ///     This class must be initialized by the storage provider for the puid and requestId.
    /// </summary>
    public interface IExportZipStorageHelper
    {
        /// <summary>
        ///     Delete the zip file
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        Task<bool> DeleteZipStorageAsync(string requestId);

        /// <summary>
        ///     Get the zip file's azure storage uri
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        CloudBlob GetZipBlob(string requestId);

        /// <summary>
        ///     Get the zip file's size in bytes
        /// </summary>
        /// <param name="exportId"></param>
        /// <returns></returns>
        Task<long> GetZipFileSizeAsync(string exportId);

        /// <summary>
        ///     Get the zip file stream
        /// </summary>
        /// <param name="exportId"></param>
        /// <returns></returns>
        Task<Stream> GetZipStreamAsync(string exportId);

        /// <summary>
        ///     Write the zip stream
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        Task<ZipWriter> WriteZipStreamAsync(string requestId);
    }
}
