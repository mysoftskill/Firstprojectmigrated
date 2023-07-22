// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Threading.Tasks;
    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ZipWriter;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Blob;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Export Staging Helper facilitates writing files to the staging container, zipping the container once everything is gathered and then deleting the container.
    ///     This and other export storage interfaces are accessed via the IExportStorageProvider
    ///     This class must be initialized by the storage provider for the puid and requestId.
    /// </summary>
    public class ExportStagingStorageHelper : IExportStagingStorageHelper
    {
        private readonly CloudBlobClient client;

        private readonly ILogger logger;

        private readonly ExportZipStorageHelper zipStorage;

        private long id;

        private string requestId;

        private CloudBlobContainer stagingContainer;

        /// <summary>
        ///     Get Staging Container Name
        /// </summary>
        /// <param name="puid"></param>
        /// <param name="requestId"></param>
        /// <returns></returns>
        public static string GetStagingContainerName(long puid, string requestId)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}stg",
                ExportStorageProvider.GetIdHash(puid),
                requestId.ToLowerInvariant());
        }

        /// <summary>
        ///     constructor of ExportStagingStorageHelper
        /// </summary>
        /// <param name="client"></param>
        /// <param name="log"></param>
        public ExportStagingStorageHelper(CloudBlobClient client, ILogger log)
        {
            this.logger = log;
            this.client = client;
            this.zipStorage = new ExportZipStorageHelper(client, log);
        }

        /// <summary>
        ///     Delete the Staging Container and everything under it
        /// </summary>
        /// <returns></returns>
        public async Task DeleteStagingContainerAsync()
        {
            await this.DeleteStagingContainerAsync(this.id, this.requestId).ConfigureAwait(false);
        }

        /// <summary>
        ///     Delete the Staging Container and everything under it
        /// </summary>
        /// <param name="puid"></param>
        /// <param name="requestId"></param>
        /// <returns></returns>
        public async Task<bool> DeleteStagingContainerAsync(long puid, string requestId)
        {
            string containerName = GetStagingContainerName(puid, requestId);
            return await AzureBlobWriter.DeleteContainerAsync(this.client, containerName).ConfigureAwait(false);
        }

        /// <summary>
        ///     Get the staging file within the staging container
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public IExportStagingFile GetStagingFile(string relativePath)
        {
            return new AzureBlobWriter(this.stagingContainer, this.logger, true, relativePath);
        }

        /// <summary>
        ///     Initialize the Staging file helper with the user's id and requestId
        /// </summary>
        /// <param name="idString"></param>
        /// <param name="requestId"></param>
        /// <returns></returns>
        public async Task InitializeStagingAsync(string idString, string requestId)
        {
            if (!ExportStatusRecord.ParseUserId(idString, out this.id))
            {
                this.logger.Error(nameof(ExportStagingStorageHelper), $"id string is not a long, it is expected to be a puid in the form of a long {idString}");
                return;
            }
            this.requestId = requestId;
            string containerName = this.GetStagingContainerName();
            this.stagingContainer = await AzureBlobWriter.GetContainerAsync(this.client, containerName).ConfigureAwait(false);
        }

        /// <summary>
        ///     Zip the files in the staging container
        /// </summary>
        /// <returns></returns>
        public async Task<CloudBlob> ZipStagingAsync(ICounterFactory counterFactory, string counterCategoryName)
        {
            // Enumerate the result segment returned.
            BlobContinuationToken continuationToken = null;
            await this.zipStorage.InitializeAsync(this.id).ConfigureAwait(false);
            try
            {
                using (ZipWriter zipWriter = await this.zipStorage.WriteZipStreamAsync(this.requestId).ConfigureAwait(false))
                {
                    ulong fileCount = 0;
                    ulong byteCount = 0;
                    do
                    {
                        // only committed blobs, don't need metadata.
                        BlobResultSegment resultSegment = await this.stagingContainer.ListBlobsSegmentedAsync(
                            null,
                            true,
                            BlobListingDetails.None,
                            1000,
                            continuationToken,
                            null,
                            null).ConfigureAwait(false);
                        foreach (IListBlobItem blobItem in resultSegment.Results)
                        {
                            if (!(blobItem is CloudBlockBlob blob))
                                continue;
                            fileCount++;
                            byteCount += (ulong)blob.Properties.Length;
                            await zipWriter.WriteEntryAsync(blob.Name, blob).ConfigureAwait(false);
                        }

                        // Get the continuation token, if there are additional segments of results.
                        continuationToken = resultSegment.ContinuationToken;
                    } while (continuationToken != null);

                    counterFactory.GetCounter(counterCategoryName, "ExportFileCount", CounterType.Number).SetValue(fileCount);
                    counterFactory.GetCounter(counterCategoryName, "ExportByteCount", CounterType.Number).SetValue(byteCount);
                }
                
                CloudBlob zipBlob = this.zipStorage.GetZipBlob(this.requestId);
                await zipBlob.FetchAttributesAsync().ConfigureAwait(false);
                counterFactory.GetCounter(counterCategoryName, "ExportArchiveByteCount", CounterType.Number).SetValue((ulong)zipBlob.Properties.Length);
                return zipBlob;
            }
            catch (StorageException e)
            {
                this.logger.Log(IfxTracingLevel.Error, nameof(ExportStagingStorageHelper), e.ToString());
                throw;
            }
        }
        public async Task<UserDelegationKey> GetUserDelegationKey(DateTimeOffset start, DateTimeOffset end)
        {
            return await this.zipStorage.GetUserDelegationKey(start, end);
        }

        private string GetStagingContainerName()
        {
            return GetStagingContainerName(this.id, this.requestId);
        }
    }
}
