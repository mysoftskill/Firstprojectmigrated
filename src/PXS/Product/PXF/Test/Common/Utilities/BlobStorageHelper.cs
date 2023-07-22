namespace Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Blob;

    public static class BlobStorageHelper
    {
        public static async Task<CloudBlobContainer> GetCloudBlobContainerAsync(
            string connectionString,
            string containerName)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
            CloudBlobContainer container = account.CreateCloudBlobClient().GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync().ConfigureAwait(false);

            return container;
        }

        public static async Task<Uri> GetSharedAccessSignatureAsync(
            CloudBlobContainer container,
            SharedAccessBlobPermissions blobContainerPermissions = SharedAccessBlobPermissions.Add | SharedAccessBlobPermissions.Create | SharedAccessBlobPermissions.Write)
        {
            await container.CreateIfNotExistsAsync().ConfigureAwait(false);

            var policy = new SharedAccessBlobPolicy
            {
                Permissions = blobContainerPermissions,
                SharedAccessExpiryTime = DateTimeOffset.UtcNow + TimeSpan.FromDays(60)
            };
            string token = container.GetSharedAccessSignature(policy);

            return new Uri(container.Uri, token);
        }
    }
}
