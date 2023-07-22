// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper
{
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Blob;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     the Single Record Blob Helper is a wrapper for the AzureBlobWriter that facilitates CRUD on single row files.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SingleRecordBlobHelper<T> : ISingleRecordBlobHelper<T>
        where T : class
    {
        private readonly CloudBlobClient client;

        private AzureBlobWriter writer;

        /// <summary>
        ///     gets or sets the container for the blob rows
        /// </summary>
        public CloudBlobContainer Container { get; private set; }

        /// <summary>
        ///     gets the container name for the blob helper
        /// </summary>
        public string ContainerName { get; }

        /// <summary>
        ///     gets the Geneva trace logger
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        ///     Construct the single record helper
        /// </summary>
        /// <param name="client"></param>
        /// <param name="containerName"></param>
        /// <param name="log"></param>
        public SingleRecordBlobHelper(CloudBlobClient client, string containerName, ILogger log)
        {
            this.client = client;
            this.ContainerName = containerName;
            this.Logger = log;
        }

        /// <summary>
        ///     Creates the record
        /// </summary>
        /// <param name="record"></param>
        /// <returns>the eTag</returns>
        public async Task<string> CreateRecordAsync(T record)
        {
            return await this.writer.CreateSingleRecordBlobAsync(record).ConfigureAwait(false);
        }

        /// <summary>
        ///     Deletes the container and everything in it
        /// </summary>
        /// <returns>true if the container has been deleted, false if the container didn't exist</returns>
        public async Task<bool> DeleteContainerAsync()
        {
            return await AzureBlobWriter.DeleteContainerAsync(this.client, this.ContainerName).ConfigureAwait(false);
        }

        /// <summary>
        ///     Delete the record, return true if exists, false if already deleted
        /// </summary>
        /// <returns>true if record has been deleted, false if the record didn't exist</returns>
        public async Task<bool> DeleteRecordAsync()
        {
            return await this.writer.DeleteBlobIfExistsAsync().ConfigureAwait(false);
        }

        /// <summary>
        ///     Gets a record, assuming that InitializeAsync has been called.
        /// </summary>
        /// <param name="allowNotFound"></param>
        /// <returns>the record</returns>
        public async Task<T> GetRecordAsync(bool allowNotFound = false)
        {
            try
            {
                return await this.writer.GetSingleRecordBlobAsync<T>().ConfigureAwait(false);
            }
            catch (StorageException ex) when (allowNotFound && ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <summary>
        ///     Initializes and Deletes a record for a key.
        ///     This is used when the class is not initialized with a key, for Listing all the records in the container.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>true if the record is deleted, false if record didn't exist</returns>
        public async Task<bool> InitializeAndDeleteAsync(string key)
        {
            this.writer.InitializeNewFile(key);
            return await this.DeleteRecordAsync().ConfigureAwait(false);
        }

        /// <summary>
        ///     Initializes the class to work with a particular record in the container.
        ///     If the key is null or whitespace, this class and still be used to list records in the container.
        ///     Initialize can be called later.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task InitializeAsync(string key)
        {
            this.Container = await AzureBlobWriter.GetContainerAsync(this.client, this.ContainerName).ConfigureAwait(false);
            this.writer = new AzureBlobWriter(this.Container, this.Logger, true, key);
        }

        /// <summary>
        ///     lists records in the container in ascending order up to the max specified by top
        /// </summary>
        /// <param name="prefix">filters the selection of records</param>
        /// <param name="top">max number of records returned</param>
        /// <returns></returns>
        public async Task<IList<T>> ListRecordsAscendingAsync(string prefix, int top)
        {
            return await this.writer.ListRecordsAscendingAsync<T>(this.Container, prefix, top).ConfigureAwait(false);
        }

        /// <summary>
        ///     lists records in the container in descending order up to the max specified by top
        ///     Note: This api assumes that number of blobs within the container is less that MaxBlobsToList
        /// </summary>
        /// <param name="prefix">filters the selection of records</param>
        /// <param name="top">max number of records returned</param>
        /// <returns></returns>
        public async Task<IList<T>> ListRecordsDescendingAsync(string prefix, int top)
        {
            return await this.writer.ListRecordsDescendingAsync<T>(this.Container, prefix, top).ConfigureAwait(false);
        }

        /// <summary>
        ///     updates or inserts a record
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public async Task<string> UpsertRecordAsync(T record)
        {
            return await this.writer.WriteSingleRecordBlobAsync(record).ConfigureAwait(false);
        }
    }
}
