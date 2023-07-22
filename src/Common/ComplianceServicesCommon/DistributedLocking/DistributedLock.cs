namespace Microsoft.Azure.ComplianceServices.Common.DistributedLocking
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json;

    /// <summary>
    /// Uses an Azure Blob as a distributed locking mechanism.
    /// </summary>
    public class DistributedLock<T> where T : class
    {
        private readonly IDistributedLockPrimitives<T> primitives;
        private readonly string lockName;

        private readonly IEventLogger eventLogger;

        private readonly string ownerId = $"{Environment.MachineName}.{Guid.NewGuid():n}";
        
        private bool exists;

        /// <summary>
        /// Initializes a new lock with the given name.
        /// </summary>
        public DistributedLock(string lockName, string blobConnectionString, string blobContainerName, IEventLogger logger)
        {
            this.AbsoluteExpiration = DateTimeOffset.MinValue;

            CloudStorageAccount account = CloudStorageAccount.Parse(blobConnectionString);
            var blobClient = account.CreateCloudBlobClient();

            var blobContainer = blobClient.GetContainerReference(blobContainerName.ToLowerInvariant());
            var blob = blobContainer.GetBlockBlobReference(lockName);

            this.primitives = new AzureBlockBlobPrimitives(blob);
            this.lockName = lockName;
            this.eventLogger = logger;
        }

        /// <summary>
        /// Initializes a new lock with the given container and blob.
        /// </summary>
        public DistributedLock(string lockName, IDistributedLockPrimitives<T> primitives)
        {
            this.lockName = lockName;
            this.primitives = primitives;
        }

        /// <summary>
        /// The absolute expiration time.
        /// </summary>
        public DateTimeOffset AbsoluteExpiration { get; private set; }

        /// <summary>
        /// Remaining time. Computed as difference of absolute expiration and "now".
        /// </summary>
        public TimeSpan RemainingTime => this.AbsoluteExpiration - DateTimeOffset.UtcNow;

        /// <summary>
        /// Valid if there is any remaining time.
        /// </summary>
        public bool IsLocked => this.RemainingTime >= TimeSpan.Zero;
        
        /// <summary>
        /// Attempts to acquire the lease on the blob. If the operation succeeds, it returns the state of the blob. If it fails, it returns null.
        /// </summary>
        public async Task<AcquireResult> TryAcquireAsync(TimeSpan leaseTime, ILogger logger)
        {
            await this.EnsureExistsAsync().ConfigureAwait(false);

            DistributedLockStatus<T> status = await this.primitives.GetStatusAsync().ConfigureAwait(false);
            if (status.ExpirationTime >= DateTimeOffset.UtcNow && status.OwnerId != this.ownerId)
            {
                logger.Information("DistributedLock", $"[TryAcquireAsync] Lock name={this.lockName}, Lock status is not expired lease cannot be acquired/extended, Lock owned by ={status.OwnerId} requesterId = {this.ownerId} , and ExpirationTime={status.ExpirationTime} ExpirationTimeUTC={status.ExpirationTime.UtcDateTime}, currentTimeUTC={DateTimeOffset.UtcNow}");
                this.Reset();
                return new AcquireResult(null, false);
            }

            bool success = await this.primitives.TryAcquireOrExtendLeaseAsync(status.State, DateTimeOffset.UtcNow + leaseTime, this.ownerId, status.ETag).ConfigureAwait(false);
            if (!success)
            {
                logger.Information("DistributedLock", $"[TryAcquireAsync] Lock name={this.lockName}, Lease was available but Lease acquire failed.");
                this.Reset();
                return new AcquireResult(null, false);
            }

            this.eventLogger?.DistributedLockAcquiredEvent(this.lockName, this.RemainingTime);
            
            this.AbsoluteExpiration = DateTimeOffset.UtcNow + leaseTime;
            logger.Information("DistributedLock", $"[TryAcquireAsync] {this.ownerId} successfully acquired the lock name={this.lockName} to {this.AbsoluteExpiration} with ETag: {status.ETag}");
            return new AcquireResult(status.State, true);
        }
        
        /// <summary>
        /// Attempts to acquire the lease on the blob. If the operation succeeds, it returns the state of the blob. If it fails, it returns null.
        /// </summary>
        public async Task<bool> TryExtendAsync(TimeSpan leaseTime, T updatedState, ILogger logger)
        {
            await this.EnsureExistsAsync().ConfigureAwait(false);

            DistributedLockStatus<T> status = await this.primitives.GetStatusAsync().ConfigureAwait(false);
            if (status.ExpirationTime >= DateTimeOffset.UtcNow && status.OwnerId != this.ownerId)
            {
                logger.Information("DistributedLock", $"[TryExtendAsync] Lock name={this.lockName}, Lock status is not expired lease cannot be acquired/extended, Lock owned by ={status.OwnerId} requesterId = {this.ownerId} , and ExpirationTime={status.ExpirationTime} ExpirationTimeUTC={status.ExpirationTime.UtcDateTime}, currentTimeUTC={DateTimeOffset.UtcNow}");
                this.Reset();
                return false;
            }

            bool success = await this.primitives.TryAcquireOrExtendLeaseAsync(updatedState, DateTimeOffset.UtcNow + leaseTime, this.ownerId, status.ETag).ConfigureAwait(false);
            if (!success)
            {   
                logger.Information("DistributedLock", $"[TryExtendAsync] Lock name={this.lockName}, Lease was available but Lease acquire failed.");
                this.Reset();
                return false;
            }

            this.eventLogger?.DistributedLockAcquiredEvent(this.lockName, this.RemainingTime);

            this.AbsoluteExpiration = DateTimeOffset.UtcNow + leaseTime;
            logger.Information("DistributedLock", $"[TryExtendAsync] {ownerId} successfully extended the lock name={this.lockName} to {this.AbsoluteExpiration}.");
            return true;
        }

        /// <summary>
        /// Ensures the blob exists by uploading an initial value.
        /// </summary>
        private async Task EnsureExistsAsync()
        {
            if (this.exists)
            {
                return;
            }

            // Create the container if it doesn't exist.
            await this.primitives.CreateIfNotExistsAsync().ConfigureAwait(false);
            this.exists = true;
        }

        private void Reset()
        {
            this.AbsoluteExpiration = DateTimeOffset.MinValue;
        }

        /// <summary>
        /// Result of an operation to try to acquire a lock.
        /// </summary>
        public class AcquireResult
        {
            public AcquireResult(T status, bool succeeded)
            {
                this.Status = status;
                this.Succeeded = succeeded;
            }

            public T Status { get; }

            public bool Succeeded { get; }
        }

        private class AzureBlockBlobPrimitives : IDistributedLockPrimitives<T>
        {
            private readonly CloudBlockBlob blob;

            public AzureBlockBlobPrimitives(CloudBlockBlob blob)
            {
                this.blob = blob;
            }

            public async Task CreateIfNotExistsAsync()
            {
                await this.blob.Container.CreateIfNotExistsAsync().ConfigureAwait(false);

                try
                {
                    var defaultValue = new DistributedLockStatus<T>
                    {
                        ExpirationTime = DateTimeOffset.MinValue,
                        OwnerId = null,
                        State = null,
                    };

                    // access condition causes the upload to fail if the blob already exists.
                    await this.blob.UploadTextAsync(JsonConvert.SerializeObject(defaultValue), null, AccessCondition.GenerateIfNoneMatchCondition("*"), null, null).ConfigureAwait(false);
                }
                catch (StorageException ex)
                {
                    // We should expect conflict errors (409) when the blob already exists.
                    if (ex.RequestInformation.HttpStatusCode != 409)
                    {
                        throw;
                    }
                }
            }

            public async Task<DistributedLockStatus<T>> GetStatusAsync()
            {
                string text = await this.blob.DownloadTextAsync().ConfigureAwait(false);

                var status = JsonConvert.DeserializeObject<DistributedLockStatus<T>>(text);
                status.ETag = this.blob.Properties.ETag;

                return status;
            }

            public async Task<bool> TryAcquireOrExtendLeaseAsync(T value, DateTimeOffset expirationTime, string ownerId, string etag)
            {
                var status = new DistributedLockStatus<T>
                {
                    ExpirationTime = expirationTime,
                    OwnerId = ownerId,
                    State = value,
                };

                try
                {
                    await this.blob.UploadTextAsync(JsonConvert.SerializeObject(status), null, AccessCondition.GenerateIfMatchCondition(etag), null, null).ConfigureAwait(false);
                }
                catch (StorageException ex)
                {
                    // We should expect conflict errors (409) when the blob already exists.
                    if (ex.RequestInformation.HttpStatusCode == 409)
                    {
                        return false;
                    }
                    
                    throw;
                }

                return true;
            }
        }
    }
}
