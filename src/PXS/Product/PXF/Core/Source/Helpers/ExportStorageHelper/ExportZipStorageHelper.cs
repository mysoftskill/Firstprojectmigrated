// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ZipWriter;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Blob;

    using Microsoft.PrivacyServices.Common.Azure;
    using System.Threading;


    /// <summary>
    ///     Export Zip Storage facilitates Writing and getting the zip file within the zip container.
    ///     This and other export storage interfaces are accessed via the IExportStorageProvider
    ///     This class must be initialized by the storage provider for the puid and requestId.
    /// </summary>
    public class ExportZipStorageHelper : IExportZipStorageHelper
    {
        private readonly CloudBlobClient client;

        private CloudBlobContainer zipContainer;

        /// <summary>
        ///     Get the zip file's azure blob name
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        public static string GetZipBlobName(string requestId)
        {
            return "Export-" + requestId.ToLowerInvariant() + ".zip";
        }

        /// <summary>
        ///     get the zip file's container
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string GetZipContainerName(long id)
        {
            return ExportStorageProvider.GetIdHash(id);
        }

        /// <summary>
        ///     gets the Id for this zip file
        /// </summary>
        public long Id { get; private set; }

        /// <summary>
        ///     constructor
        /// </summary>
        /// <param name="client"></param>
        /// <param name="log"></param>
        public ExportZipStorageHelper(CloudBlobClient client, ILogger log)
        {
            this.client = client;
        }

        /// <summary>
        ///     Delete the zip file
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        public async Task<bool> DeleteZipStorageAsync(string requestId)
        {
            string zipFileName = GetZipBlobName(requestId);
            CloudBlockBlob zipBlob = this.zipContainer.GetBlockBlobReference(zipFileName);
            return await zipBlob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, null, null, null, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        ///     Get the zip file's container name
        /// </summary>
        /// <returns></returns>
        public string GetZipContainerName()
        {
            return GetZipContainerName(this.Id);
        }

        /// <summary>
        ///     Get the zip file's size in bytes
        /// </summary>
        /// <param name="exportId"></param>
        /// <returns></returns>
        public async Task<long> GetZipFileSizeAsync(string exportId)
        {
            string zipFileName = GetZipBlobName(exportId);
            CloudBlockBlob zipBlob = this.zipContainer.GetBlockBlobReference(zipFileName);
            await zipBlob.FetchAttributesAsync().ConfigureAwait(false);
            return zipBlob.Properties.Length;
        }

        /// <summary>
        ///     Get the zip file's azure storage uri
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        public CloudBlob GetZipBlob(string requestId)
        {
            string zipFileName = GetZipBlobName(requestId);
            return this.zipContainer.GetBlockBlobReference(zipFileName);
        }

        /// <summary>
        ///     Get the zip file stream
        /// </summary>
        /// <param name="exportId"></param>
        /// <returns></returns>
        public async Task<Stream> GetZipStreamAsync(string exportId)
        {
            string zipFileName = GetZipBlobName(exportId);
            CloudBlockBlob zipBlob = this.zipContainer.GetBlockBlobReference(zipFileName);
            return await zipBlob.OpenReadAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the User delegation key from client
        /// </summary>
        /// <param name="start">Start of validity for key</param>
        /// <param name="end">End of validity for key</param>
        /// <returns>User Delegation Key</returns>
        public async Task<UserDelegationKey> GetUserDelegationKey(DateTimeOffset start, DateTimeOffset end)
        {
            return await this.client.GetUserDelegationKeyAsync(start, end);
        }

        /// <summary>
        ///     Initialize the zip storage helper
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task InitializeAsync(long id)
        {
            this.Id = id;
            this.zipContainer = await AzureBlobWriter.GetContainerAsync(this.client, this.GetZipContainerName()).ConfigureAwait(false);
        }

        /// <summary>
        ///     Write the zip stream
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        public async Task<ZipWriter> WriteZipStreamAsync(string requestId)
        {
            string zipFileName = GetZipBlobName(requestId);
            CloudBlockBlob zipBlob = this.zipContainer.GetBlockBlobReference(zipFileName);
            CloudBlobStream zipBlobStream = await zipBlob.OpenWriteAsync().ConfigureAwait(false);
            return new ZipWriter(zipBlobStream);
        }
    }
}
