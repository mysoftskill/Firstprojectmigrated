// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Blob;

    using Newtonsoft.Json;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <remark>
    ///     Blob writer class facilitates writing blobs to Azure Storage.
    ///     ---------------------------------------------------
    ///     Pattern 1 writing a binary file to a container
    ///     var container = await AzureBlobWriter.GetContainerAsync(GlobalSingletons.Dequeuer.Value.BlobClient, containerName);
    ///     var blobWriter = new AzureBlobWriter(container,
    ///     dataResourceConfiguration.CategoryName.ToString().ToLowerInvariant()
    ///     + "/"
    ///     + resourceStatus.ResourceDataType.ToString().ToLowerInvariant()
    ///     + fileNumber.ToString("000")
    ///     + ".jpg");
    ///     await blobWriter.WriteFromBinaryFileAsync("test.jpg");
    ///     await blobWriter.CommitAsync();
    ///     ---------------------------------------------------
    ///     Pattern 2 writing a text file to a container
    ///     var container = await AzureBlobWriter.GetContainerAsync(GlobalSingletons.Dequeuer.Value.BlobClient, containerName);
    ///     var blobWriter = new AzureBlobWriter(container,
    ///     dataResourceConfiguration.CategoryName.ToString().ToLowerInvariant()
    ///     + "/"
    ///     + resourceStatus.ResourceDataType.ToString().ToLowerInvariant()
    ///     + fileNumber.ToString("000")
    ///     + ".txt");
    ///     await blobWriter.WriteFromTextFileAsync("test.txt");
    ///     await blobWriter.CommitAsync();
    ///     ---------------------------------------------------
    ///     Pattern 3 writing blocks of data
    ///     var container = await AzureBlobWriter.GetContainerAsync(GlobalSingletons.Dequeuer.Value.BlobClient, containerName);
    ///     var blobWriter = new AzureBlobWriter(container,
    ///     dataResourceConfiguration.CategoryName.ToString().ToLowerInvariant()
    ///     + "/"
    ///     + resourceStatus.ResourceDataType.ToString().ToLowerInvariant()
    ///     + fileNumber.ToString("000")
    ///     + ".txt");
    ///     await blobWriter.AddBlockAsync("this is a block of sample content", blobWriter.NextBlockId);
    ///     await blobWriter.AddBlockAsync("this is another block of sample content", blobWriter.NextBlockId);
    ///     await blobWriter.CommitAsync();
    ///     ---------------------------------------------------
    ///     Pattern 4 updating a single record blob
    ///     var writer = new AzureBlobWriter(this.statusContainer, this.logger, true, record.RequestId);
    ///     return await writer.WriteSingleRecordBlobAsync&lt;ExportStatusRecord&gt;(record);
    /// </remark>
    public class AzureBlobWriter : IExportStagingFile
    {
        /// <summary>
        ///     maximum number of bytes allowed in a single block
        /// </summary>
        public const int MaxBlobBlockSizeInBytes = 4 * 1024 * 1024;

        /// <summary>
        ///     maximum number of blobs that will be returned in a listing operation
        /// </summary>
        public const int MaxBlobsToList = 99999;

        /// <summary>
        ///     minimum number of kilobytes met to cut a new block
        /// </summary>
        public const int MinBlobBlockSizeKDefault = 3072;

        private readonly List<string> blockIds;

        private readonly CloudBlobContainer container;

        private readonly OperationContext context;

        private readonly ILogger logger;

        private readonly int minimumBlobBlockSizeK;

        private readonly BlobRequestOptions options;

        private readonly bool useEtags;

        private CloudBlockBlob blob;

        private int currentBlock;

        private bool? exists;

        private bool? isTextData;

        private MemoryStream memoryStream;

        private StreamWriter streamWriter;

        private Encoding utf8;

        /// <summary>
        ///     Delete a blob container
        /// </summary>
        /// <param name="client"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async Task<bool> DeleteContainerAsync(CloudBlobClient client, string path)
        {
            CloudBlobContainer container = client.GetContainerReference(path);
            return await container.DeleteIfExistsAsync().ConfigureAwait(false);
        }

        /// <summary>
        ///     Get a blob container or create it if it doesn't exist
        /// </summary>
        /// <param name="client"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async Task<CloudBlobContainer> GetContainerAsync(CloudBlobClient client, string path)
        {
            CloudBlobContainer container = client.GetContainerReference(path);
            await container.CreateIfNotExistsAsync().ConfigureAwait(false);
            return container;
        }

        /// <summary>
        ///     gets the name of the blob within the staging container
        /// </summary>
        public string FileName => this.blob.Name;

        /// <summary>
        ///     gets the Next Block Name for an azure blob block
        /// </summary>
        public string NextBlockName
        {
            get
            {
                string name = $"b{this.currentBlock:0000000000}{Guid.NewGuid().ToString("N").ToLowerInvariant()}";
                this.currentBlock++;
                return name;
            }
        }

        /// <summary>
        ///     constructor
        /// </summary>
        /// <param name="blobContainer"></param>
        /// <param name="log"></param>
        /// <param name="useEtags"></param>
        /// <param name="name"></param>
        /// <param name="minBlockSize"></param>
        public AzureBlobWriter(CloudBlobContainer blobContainer, ILogger log, bool useEtags = false, string name = null, int minBlockSize = MinBlobBlockSizeKDefault)
        {
            this.logger = log;
            this.container = blobContainer;
            this.blockIds = new List<string>();
            this.options = new BlobRequestOptions();
            this.context = new OperationContext();
            if (!string.IsNullOrWhiteSpace(name))
            {
                this.InitializeNewFile(name);
            }
            this.isTextData = null;
            this.minimumBlobBlockSizeK = minBlockSize;
            this.useEtags = useEtags;
        }

        /// <summary>
        ///     Add a string to the current block
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task AddBlockAsync(string content)
        {
            if (content == null)
            {
                return;
            }
            if (!this.isTextData.HasValue)
            {
                this.InitializeAsText();
            }

            if (!this.isTextData.Value)
            {
                throw new InvalidOperationException("invalid use of AzureBlobWriter");
            }

            int contentBytes = this.utf8.GetByteCount(content);
            if ((contentBytes + this.memoryStream.Position > MaxBlobBlockSizeInBytes)
                && this.memoryStream.Position > 0)
            {
                await this.WriteBlockAsync(this.NextBlockName).ConfigureAwait(false);
            }

            if (contentBytes > MaxBlobBlockSizeInBytes)
            {
                int offset = 0;
                do
                {
                    int length = this.FindUtf8MaxByteCount(content, offset);
                    await this.streamWriter.WriteAsync(content.Substring(offset, length)).ConfigureAwait(false);
                    await this.WriteBlockAsync(this.NextBlockName).ConfigureAwait(false);
                    offset += length;
                    contentBytes = offset < content.Length ? this.utf8.GetByteCount(content.Substring(offset)) : 0;
                } while (contentBytes > MaxBlobBlockSizeInBytes);
                if (offset < content.Length)
                {
                    await this.streamWriter.WriteAsync(content.Substring(offset)).ConfigureAwait(false);
                }
            }
            else
            {
                await this.streamWriter.WriteAsync(content).ConfigureAwait(false);
            }

            if (this.memoryStream.Position / 1024 > this.minimumBlobBlockSizeK)
            {
                await this.WriteBlockAsync(this.NextBlockName).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Add a byte array to the current block
        /// </summary>
        /// <param name="buff"></param>
        /// <returns></returns>
        public async Task AddBlockAsync(byte[] buff)
        {
            if (!this.isTextData.HasValue)
            {
                this.InitializeAsBinary();
            }

            if (this.isTextData.Value)
            {
                throw new InvalidOperationException("invalid use of AzureBlobWriter");
            }

            if ((buff.Length + this.memoryStream.Position > MaxBlobBlockSizeInBytes)
                && this.memoryStream.Position > 0)
            {
                await this.WriteBlockAsync(this.NextBlockName).ConfigureAwait(false);
            }

            if (buff.Length > MaxBlobBlockSizeInBytes)
            {
                int offset = 0;
                int length = MaxBlobBlockSizeInBytes;
                do
                {
                    this.memoryStream.Write(buff, offset, length);
                    await this.WriteBlockAsync(this.NextBlockName).ConfigureAwait(false);
                    offset += length;
                } while ((buff.Length - offset) > MaxBlobBlockSizeInBytes);
                length = buff.Length - offset;
                if (length > 0)
                {
                    this.memoryStream.Write(buff, offset, length);
                }
            }
            else
            {
                this.memoryStream.Write(buff, 0, buff.Length);
            }

            if (this.memoryStream.Position / 1024 > this.minimumBlobBlockSizeK)
            {
                await this.WriteBlockAsync(this.NextBlockName).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Commit what is written
        /// </summary>
        /// <returns></returns>
        public async Task CommitAsync()
        {
            if (this.memoryStream != null && this.memoryStream.Position != 0)
            {
                await this.WriteBlockAsync(this.NextBlockName).ConfigureAwait(false);
            }

            if (this.blockIds.Any())
            {
                AccessCondition access = await this.GenerateAccessConditionAsync().ConfigureAwait(false);
                await this.blob.PutBlockListAsync(this.blockIds, access, this.options, this.context).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Create a blob with a single block
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task<string> CreateSingleRecordBlobAsync<T>(T obj)
        {
            AccessCondition createCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
            return await this.WriteSingleRecordBlobAsync(obj, createCondition).ConfigureAwait(false);
        }

        /// <summary>
        ///     Delete the blob if it exists
        /// </summary>
        /// <returns></returns>
        public async Task<bool> DeleteBlobIfExistsAsync()
        {
            return await this.blob.DeleteIfExistsAsync().ConfigureAwait(false);
        }

        /// <summary>
        ///     Clean up streams if used
        /// </summary>
        public void Dispose()
        {
            if (this.streamWriter != null)
            {
                this.streamWriter.Dispose();
            }
            if (this.memoryStream != null)
            {
                this.memoryStream.Dispose();
            }
        }

        /// <summary>
        ///     Returns true if the blob exists
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ExistsAsync()
        {
            if (this.blob == null)
            {
                throw new InvalidOperationException("in order to use etags, the blob client needs to be created either in the constructor or CreateFile");
            }

            this.exists = await this.blob.ExistsAsync().ConfigureAwait(false);
            return this.exists.Value;
        }

        /// <summary>
        ///     Fetch the information for an existing blob
        /// </summary>
        /// <returns></returns>
        public async Task FetchExistingBlockInfoAsync()
        {
            IEnumerable<ListBlockItem> blocks;
            try
            {
                blocks = await this.blob.DownloadBlockListAsync(
                    BlockListingFilter.Committed,
                    AccessCondition.GenerateEmptyCondition(),
                    this.options,
                    this.context).ConfigureAwait(false);
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation != null && ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    return;
                }
                this.logger.Log(IfxTracingLevel.Error, nameof(AzureBlobWriter), ex.ToString());
                throw;
            }

            if (blocks != null)
            {
                ListBlockItem last = blocks.LastOrDefault();
                if (last != null)
                {
                    byte[] lastBytes = Convert.FromBase64String(last.Name);
                    char[] lastChars = Encoding.ASCII.GetChars(lastBytes);
                    if (lastChars.Length >= 10)
                    {
                        string lastBlockIdStr = new string(lastChars).Substring(1, 10);
                        if (int.TryParse(lastBlockIdStr, out int lastBlockId)
                            && lastBlockId > 1)
                        {
                            this.currentBlock = lastBlockId + 1;
                            this.blockIds.Clear();
                            this.blockIds.AddRange(blocks.Select(blk => blk.Name));
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Generate the access condition used to write to the blob
        /// </summary>
        /// <returns></returns>
        public async Task<AccessCondition> GenerateAccessConditionAsync()
        {
            AccessCondition access;
            if (!this.useEtags)
            {
                access = AccessCondition.GenerateEmptyCondition();
                return access;
            }

            if (!this.exists.HasValue || !this.exists.Value)
            {
                await this.ExistsAsync().ConfigureAwait(false);
            }

            if (this.exists.Value)
            {
                if (this.blob == null)
                {
                    throw new ArgumentNullException("this.blob is null");
                }
                if (this.blob.Properties == null)
                {
                    throw new ArgumentNullException("this.blob.Properties is null");
                }
                access = AccessCondition.GenerateIfMatchCondition(this.blob.Properties.ETag);
            }
            else
            {
                access = AccessCondition.GenerateIfNoneMatchCondition("*");
            }
            return access;
        }

        /// <summary>
        ///     Gets a single block blob
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<T> GetSingleRecordBlobAsync<T>()
        {
            return await this.InternalGetSingleRecordBlobAsync<T>(this.blob).ConfigureAwait(false);
        }

        /// <summary>
        ///     Initialize the blob writer class with the filename
        /// </summary>
        /// <param name="name"></param>
        public void InitializeNewFile(string name)
        {
            if (this.blockIds.Any())
            {
                this.blockIds.Clear();
            }
            this.blob = this.container.GetBlockBlobReference(name);
            this.currentBlock = 1;
        }

        /// <summary>
        ///     List the top n blob items from the container in ascending order
        /// </summary>
        /// <param name="container">the blob container</param>
        /// <param name="prefix">the prefix used to filter blobs in the container or null</param>
        /// <param name="top">the max number of blobs returned</param>
        /// <returns>the list of blob items</returns>
        public async Task<IList<T>> ListRecordsAscendingAsync<T>(CloudBlobContainer container, string prefix, int top)
        {
            var returnList = new List<T>();
            foreach (IListBlobItem blob in await GetBlobItemsAsync(container, prefix, top).ConfigureAwait(false))
            {
                if (blob is CloudBlockBlob blockBlob)
                {
                    T record = await this.InternalGetSingleRecordBlobAsync<T>(blockBlob).ConfigureAwait(false);
                    if (record != null)
                    {
                        returnList.Add(record);
                    }
                }
            }
            return returnList;
        }

        /// <summary>
        ///     List the top n blob items from the container in descending order
        ///     NOTE: this is intended for use with only a relatively small number of blobs in the container.
        /// </summary>
        /// <param name="container">the blob container</param>
        /// <param name="prefix">the prefix used to filter blobs in the container or null</param>
        /// <param name="top">the max number of blobs returned</param>
        /// <param name="maxBlobsToHandle"></param>
        /// <returns>the list of blob items</returns>
        public async Task<IList<T>> ListRecordsDescendingAsync<T>(CloudBlobContainer container, string prefix, int top, int maxBlobsToHandle = MaxBlobsToList)
        {
            var returnList = new List<T>();
            var blobs = new List<IListBlobItem>(await GetBlobItemsAsync(container, prefix, maxBlobsToHandle).ConfigureAwait(false));
            for (int i = blobs.Count - 1; i >= 0 && returnList.Count < top; i--)
            {
                if (blobs[i] is CloudBlockBlob blockBlob)
                {
                    T record = await this.InternalGetSingleRecordBlobAsync<T>(blockBlob).ConfigureAwait(false);
                    if (record != null)
                    {
                        returnList.Add(record);
                    }
                }
            }
            return returnList;
        }

        /// <summary>
        ///     Write a single block blob
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="selectedAccess"></param>
        /// <returns></returns>
        public async Task<string> WriteSingleRecordBlobAsync<T>(T obj, AccessCondition selectedAccess = null)
        {
            string content = JsonConvert.SerializeObject(obj);
            AccessCondition access = selectedAccess ?? await this.GenerateAccessConditionAsync().ConfigureAwait(false);
            await this.blob.UploadTextAsync(content, Encoding.UTF8, access, this.options, this.context).ConfigureAwait(false);
            return this.blob.Properties.ETag;
        }

        /// <summary>
        ///     Since the actual byte count for a utf8 string converted from unicode could vary widely
        ///     hunt around until we chunk the string correctly.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private int FindUtf8MaxByteCount(string content, int offset)
        {
            int length = (content.Length - offset > MaxBlobBlockSizeInBytes) ? MaxBlobBlockSizeInBytes : content.Length - offset;
            int decrement = 1024 * 512;
            int actual = this.utf8.GetByteCount(content.Substring(offset, length));
            while (actual > MaxBlobBlockSizeInBytes)
            {
                length -= decrement;
                actual = this.utf8.GetByteCount(content.Substring(offset, length));
            }
            return length;
        }

        private void InitializeAsBinary()
        {
            this.isTextData = false;
            this.memoryStream = new MemoryStream();
            this.streamWriter = null;
            this.utf8 = null;
            this.ResetMemoryStream();
        }

        private void InitializeAsText()
        {
            this.isTextData = true;
            this.memoryStream = new MemoryStream();
            this.streamWriter = new StreamWriter(this.memoryStream) { AutoFlush = true };
            this.utf8 = Encoding.UTF8;
            this.ResetMemoryStream();
        }

        private async Task<T> InternalGetSingleRecordBlobAsync<T>(CloudBlockBlob blockBlob)
        {
            string str = await blockBlob.DownloadTextAsync(Encoding.UTF8, AccessCondition.GenerateEmptyCondition(), this.options, this.context).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(str);
        }

        private void ResetMemoryStream()
        {
            this.memoryStream.SetLength(0);
            this.memoryStream.Position = 0;
        }

        private async Task WriteBlockAsync(string blockName)
        {
            string blockId = Convert.ToBase64String(Encoding.ASCII.GetBytes(blockName));
            this.memoryStream.Position = 0;
            string streamStringHash =
                Convert.ToBase64String(
                    MD5.Create() // lgtm[cs/weak-crypto] Suppressing warning because MD5 is the best supported option for PutBlockAsync.
                        .ComputeHash(this.memoryStream));

            this.memoryStream.Position = 0;
            AccessCondition access = await this.GenerateAccessConditionAsync().ConfigureAwait(false);
            await this.blob.PutBlockAsync(blockId, this.memoryStream, streamStringHash, access, this.options, this.context).ConfigureAwait(false);
            this.blockIds.Add(blockId);
            this.ResetMemoryStream();
        }

        /// <summary>
        ///     Gets the top n blob items from the container
        /// </summary>
        /// <param name="container">the blob container</param>
        /// <param name="prefix">the prefix used to filter blobs in the container or null</param>
        /// <param name="top">the max number of blobs returned</param>
        /// <returns>the list of blob items</returns>
        private static async Task<IEnumerable<IListBlobItem>> GetBlobItemsAsync(CloudBlobContainer container, string prefix, int top)
        {
            var items = new List<IListBlobItem>();
            BlobContinuationToken continuationToken = null;
            do
            {
                BlobResultSegment resultSegment = await container.ListBlobsSegmentedAsync(prefix, false, BlobListingDetails.None, top, continuationToken, null, null)
                    .ConfigureAwait(false);
                if (resultSegment != null)
                {
                    if (resultSegment.Results != null)
                    {
                        items.AddRange(resultSegment.Results);
                    }
                    continuationToken = resultSegment.ContinuationToken;
                }
            } while (continuationToken != null && items.Count < top);

            return items;
        }
    }
}
