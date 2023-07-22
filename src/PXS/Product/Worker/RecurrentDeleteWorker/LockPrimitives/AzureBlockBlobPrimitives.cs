namespace Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.LockPrimitives
{
    using global::Azure;
    using global::Azure.Storage.Blobs;
    using global::Azure.Storage.Blobs.Models;
    using global::Azure.Storage.Blobs.Specialized;

    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.DistributedLocking;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json;

    /// <summary>
    /// AzureBlockBlobPrimitives
    /// </summary>
    public class AzureBlockBlobPrimitives : IDistributedLockPrimitives<DistributedBackgroundWorker.LockState>
    {
        private const string ComponentName = nameof(AzureBlockBlobPrimitives);
        private readonly BlobClient blobClient;

        /// <summary>
        /// Create an instance of AzureBlockBlobPrimitives
        /// </summary>
        /// <param name="blobClient">blobClient</param>
        public AzureBlockBlobPrimitives(BlobClient blobClient)
        {
            this.blobClient = blobClient;
        }

        /// <summary>
        /// Create a container if it doesn't exist and upload a new blob
        /// </summary>
        /// <returns></returns>
        public async Task CreateIfNotExistsAsync()
        {
            var containerClient = this.blobClient.GetParentBlobContainerClient();
            try
            {
                // Create the container if it doesn't exist yet 
                await containerClient.CreateIfNotExistsAsync();

                var originalStatus = new DistributedLockStatus<DistributedBackgroundWorker.LockState>
                {
                    ExpirationTime = DateTimeOffset.MinValue,
                    OwnerId = null,
                    State = null,
                };

                var condition = new BlobUploadOptions
                {
                    Conditions = new BlobRequestConditions { IfNoneMatch = new ETag("*") }
                };

                var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(originalStatus));

                using (var stream = new MemoryStream(bytes))
                {
                    await this.blobClient.UploadAsync(stream, condition).ConfigureAwait(false);
                }
                
                DualLogger.Instance.Information(ComponentName, $"created a blob. BlobName={blobClient.Name}, ContainerName={blobClient.BlobContainerName}, StorageAccountName={blobClient.AccountName}");
            }
            catch (RequestFailedException ex)
            {
                // We should expect conflict errors (409) when the blob already exists.
                if (ex.Status != 409)
                {
                    DualLogger.Instance.Error(ComponentName, $"Exception occurred while creating container={blobClient.BlobContainerName} or blob={blobClient.Name}: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Get and return the current lock status from existing blob
        /// </summary>
        /// <returns></returns>
        public async Task<DistributedLockStatus<DistributedBackgroundWorker.LockState>> GetStatusAsync()
        {
            DistributedLockStatus<DistributedBackgroundWorker.LockState> status;
            
            try
            {
                var downloadResult = await this.blobClient.DownloadContentAsync().ConfigureAwait(false);

                string text = downloadResult.Value.Content.ToString();
                status = JsonConvert.DeserializeObject<DistributedLockStatus<DistributedBackgroundWorker.LockState>>(text);
                status.ETag = downloadResult.Value.Details.ETag.ToString();
            }
            catch(Exception ex)
            {
                DualLogger.Instance.Error(ComponentName, $"Exception occurred while getting lock status: {ex.Message}");
                throw;
            }

            return status;
        }

        /// <summary>
        /// Try to update the blob using Etag
        /// </summary>
        /// <param name="value"></param>
        /// <param name="expirationTime"></param>
        /// <param name="ownerId"></param>
        /// <param name="etag"></param>
        /// <returns></returns>
        public async Task<bool> TryAcquireOrExtendLeaseAsync(DistributedBackgroundWorker.LockState value, DateTimeOffset expirationTime, string ownerId, string etag)
        {
            var status = new DistributedLockStatus<DistributedBackgroundWorker.LockState>
            {
                ExpirationTime = expirationTime,
                OwnerId = ownerId,
                State = value,
            };

            try
            {
                var condition = new BlobUploadOptions
                {
                    Conditions = new BlobRequestConditions { IfMatch = new ETag(etag) }
                };

                var valueBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(status));

                using (var stream = new MemoryStream(valueBytes))
                {
                    await this.blobClient.UploadAsync(stream, condition).ConfigureAwait(false);
                }

                return true;
            }
            catch (RequestFailedException ex)
            {
                if (ex.Status == (int)HttpStatusCode.PreconditionFailed)
                {
                    DualLogger.Instance.Warning(ComponentName, $"Uploading to blob={blobClient.Name} failed. Blob's ETag does not match ETag provided: {etag}.");
                }
                return false;
            }
            catch (Exception ex)
            {
                DualLogger.Instance.Error(ComponentName, $"Exception occurred while updating the blob={blobClient.Name}: {ex.Message}");
                throw;
            }
        }
    }
}
