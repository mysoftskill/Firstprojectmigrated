namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.CommandStatusAggregation
{
    using global::Azure.Core;
    using global::Azure.Identity;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Auth;
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using System;
    using System.Threading.Tasks;

    public class PCFV2ExportInfoProvider : IPCFv2ExportInfoProvider
    {
        public async Task<DateTime> GetExpectionWorkerLastestRunTimeAsync()
        {
            string genericAzurestorageResourceUri = "https://storage.azure.com/";

            var uamiCred = new DefaultAzureCredential();
            AccessToken token = await uamiCred.GetTokenAsync(new TokenRequestContext(new[] { genericAzurestorageResourceUri }));
            CloudStorageAccount account = new CloudStorageAccount(new StorageCredentials(new Azure.Storage.Auth.TokenCredential(token.Token)), Config.Instance.PCFV2Storage.StorageAccountName, "core.windows.net", true);

            var blobClient = account.CreateCloudBlobClient();

            var blobContainer = blobClient.GetContainerReference(Config.Instance.PCFV2Storage.BlobContainerName.ToLowerInvariant());
            var blob = blobContainer.GetBlockBlobReference(Config.Instance.PCFV2Storage.BlobName);

            string blobContents = await blob.DownloadTextAsync().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(blobContents))
            {
                long ticks = long.Parse(blobContents);
                return new DateTime(ticks);
            }
            return DateTime.MinValue;
        }
    }
}
