namespace Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.Common
{
    using System;

    using global::Azure.Core;
    using global::Azure.Identity;
    using global::Azure.Storage.Blobs;

    using Microsoft.Azure.ComplianceServices.Common.AzureServiceUrlValidator;
    using Microsoft.Azure.ComplianceServices.Common.DistributedLocking;
    using Microsoft.Azure.Storage;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.LockPrimitives;

    public class DistributedLockBlobPrimitivesFactory : IDistributedLockBlobPrimitivesFactory
    {
        public IDistributedLockPrimitives<DistributedBackgroundWorker.LockState> CreateBloblockPrimitives(IDistributedLockConfiguration distributedLockConfig, string blobName, string uamiId)
        {
            var blobAccountName = distributedLockConfig.StorageAccountName;
            var blobContainerName = distributedLockConfig.ContainerName;
            var useEmulator = distributedLockConfig.UseEmulator;
            BlobContainerClient containerClient;

            if (useEmulator)
            {

                containerClient = new BlobContainerClient(blobAccountName, blobContainerName);
            }
            else
            {
                TokenCredential credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = uamiId });
                
                var result = StorageAccountValidator.IsValidStorageAccountName(blobAccountName);
                if (!result.IsValid)
                {
                    throw new StorageException($"Blob account name {blobAccountName} could not be validated. {result.Reason}");
                }

                var uri = new Uri($"https://{blobAccountName}.blob.core.windows.net/{blobContainerName}");
                containerClient = new BlobContainerClient(uri, credential);
            }
            var blobClient = containerClient.GetBlobClient(blobName);
            return new AzureBlockBlobPrimitives(blobClient);
        }
    }
}
