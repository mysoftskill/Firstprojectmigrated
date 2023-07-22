namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandHistory
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Newtonsoft.Json;

    internal class CommandHistoryBlobClient : ICommandHistoryBlobClient
    {
        private const string ContainerNamePrefix = "commandhistoryblob";
        private const string CompressionAlgorithmMetadataKey = "compressionAlgorithm";

        /// <summary>
        /// Containers known to exist.
        /// </summary>
        private readonly HashSet<(string accountName, string containerName)> knownContainers = new HashSet<(string accountName, string containerName)>();

        private readonly Dictionary<string, CloudBlobClient> storageAccounts;
        private readonly List<CloudBlobClient> blobClients;

        public CommandHistoryBlobClient()
        {
            this.storageAccounts = new Dictionary<string, CloudBlobClient>();
            this.blobClients = new List<CloudBlobClient>();

            foreach (var item in Config.Instance.AzureStorageAccounts)
            {
                CloudStorageAccount account = CloudStorageAccount.Parse(item);
                var blobClient = account.CreateCloudBlobClient();

                this.storageAccounts[blobClient.Credentials.AccountName] = blobClient;
                this.blobClients.Add(blobClient);
            }
        }

        public Task<BlobPointer> CreateBlobAsync(object contents)
        {
            return Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async ev =>
                {
                    CloudBlobClient client = this.GetBlobClient();

                    string accountName = client.Credentials.AccountName;
                    string containerName = $"{ContainerNamePrefix}-{DateTimeOffset.UtcNow:yyyy-MM-dd}";
                    string blobName = Guid.NewGuid().ToString("n");

                    ev["AcocuntName"] = accountName;
                    ev["ContainerName"] = containerName;
                    ev["BlobName"] = blobName;

                    CloudBlobContainer container = client.GetContainerReference(containerName);
                    if (!this.knownContainers.Contains((accountName, containerName)))
                    {
                        // Make sure the container exists.
                        bool created = await container.CreateIfNotExistsAsync();

                        lock (this.knownContainers)
                        {
                            this.knownContainers.Add((accountName, containerName));
                        }

                        if (created)
                        {
                            // If we were lucky and got to create the next day's container, then
                            // take a look and see if there is old stuff to delete.
                            Task fireAndForget = PurgeOldContainersAsync(client);
                        }
                    }

                    CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
                    AccessCondition accessCondition = AccessCondition.GenerateIfNotExistsCondition();

                    await UploadBlobAsync(blockBlob, contents, accessCondition, ev);

                    return new BlobPointer
                    {
                        AccountName = accountName,
                        BlobName = blobName,
                        ContainerName = containerName,
                    };
                });
        }

        public Task<(T value, string etag)> ReadBlobAsync<T>(BlobPointer blobPointer)
        {
            return Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async ev =>
                {
                    CloudBlobClient client = this.storageAccounts[blobPointer.AccountName];
                    CloudBlobContainer container = client.GetContainerReference(blobPointer.ContainerName);
                    CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobPointer.BlobName);

                    ev["AcocuntName"] = blobPointer.AccountName;
                    ev["ContainerName"] = blobPointer.ContainerName;
                    ev["BlobName"] = blobPointer.BlobName;

                    using (Stream memoryStream = new MemoryStream())
                    {
                        await HandleAzureHttpErrors(
                            () => blockBlob.DownloadToStreamAsync(memoryStream),
                            ev);

                        memoryStream.Position = 0;

                        if (blockBlob.Metadata[CompressionAlgorithmMetadataKey] != CompressionTools.Brotli.Name)
                        {
                            throw new InvalidOperationException("Expecting brotli compession!");
                        }

                        T value = CompressionTools.Brotli.DecompressJson<T>(memoryStream);
                        return (value, blockBlob.Properties.ETag);
                    }
                });
        }

        public Task ReplaceBlobAsync(BlobPointer blobPointer, object contents, string etag)
        {
            return Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                ev =>
                {
                    CloudBlobClient client = this.storageAccounts[blobPointer.AccountName];
                    CloudBlobContainer container = client.GetContainerReference(blobPointer.ContainerName);
                    CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobPointer.BlobName);

                    AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(etag);

                    ev["AcocuntName"] = blobPointer.AccountName;
                    ev["ContainerName"] = blobPointer.ContainerName;
                    ev["BlobName"] = blobPointer.BlobName;

                    return UploadBlobAsync(blockBlob, contents, accessCondition, ev);
                });
        }

        private static async Task UploadBlobAsync(
            CloudBlockBlob blob,
            object data,
            AccessCondition accessCondition,
            OutgoingEvent ev)
        {
            using (var memoryStream = new MemoryStream())
            {
                blob.Metadata[CompressionAlgorithmMetadataKey] = CompressionTools.Brotli.Name;

                CompressionTools.Brotli.CompressJson(data, memoryStream);
                memoryStream.Position = 0;

                await HandleAzureHttpErrors(
                    () => blob.UploadFromStreamAsync(memoryStream, accessCondition, null, null),
                    ev);
            }
        }

        private static Task PurgeOldContainersAsync(CloudBlobClient client)
        {
            return Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async ev =>
                {
                    BlobContinuationToken token = null;

                    do
                    {
                        ContainerResultSegment containerResult = await client.ListContainersSegmentedAsync(ContainerNamePrefix, null);
                        token = containerResult.ContinuationToken;

                        foreach (var container in containerResult.Results)
                        {
                            // Format is prefix-year-month-day
                            string[] nameParts = container.Name.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

                            if (nameParts.Length == 4 && nameParts[0] == ContainerNamePrefix)
                            {
                                if (int.TryParse(nameParts[1], out int year) &&
                                    int.TryParse(nameParts[2], out int month) &&
                                    int.TryParse(nameParts[3], out int day))
                                {
                                    DateTimeOffset date = new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);
                                    if (DateTimeOffset.UtcNow - date >= TimeSpan.FromDays(Config.Instance.CommandHistory.DefaultTimeToLiveDays + 5))
                                    {
                                        Task t = Task.Run(container.DeleteIfExistsAsync);
                                    }
                                }
                            }
                        }
                    }
                    while (token != null);
                });
        }

        private static async Task HandleAzureHttpErrors(Func<Task> callback, OutgoingEvent ev)
        {
            try
            {
                await callback();
            }
            catch (StorageException ex)
            {
                int errorCode = ex.RequestInformation.HttpStatusCode;
                ev["ErrorCode"] = errorCode.ToString();

                if (errorCode == 503)
                {
                    // Service unavailable.
                    throw new CommandFeedException(ex) { ErrorCode = CommandFeedInternalErrorCode.Throttle };
                }

                if (errorCode == 412)
                {
                    // Precondition failed.
                    throw new CommandFeedException(ex) { ErrorCode = CommandFeedInternalErrorCode.Conflict, IsExpected = true };
                }

                throw;
            }
        }

        private CloudBlobClient GetBlobClient()
        {
            int value = RandomHelper.Next(0, this.blobClients.Count - 1);
            for (int i = value; i < value + this.blobClients.Count; i++)
            {
                var index = value % this.blobClients.Count;
                if (FlightingUtilities.IsStringValueEnabled(FlightingNames.CommandHistoryBlobClientDisabled, this.blobClients[index].Credentials.AccountName))
                {
                    continue;
                }

                return this.blobClients[index];
            }

            throw new InvalidOperationException("No avaiable Storage Account to use. Check Carbon Flight settings.");
        }
    }
}
