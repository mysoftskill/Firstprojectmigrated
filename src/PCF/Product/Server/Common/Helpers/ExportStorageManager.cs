// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     This is responsible for provisioning staging storage.
    /// </summary>
    public class ExportStorageManager : IExportStorageManager
    {
        private const int ContainerNameLengthLimit = 63;

        private const string FinalContainerPrefix = "exp-fin-";

        private const string StagingContainerPrefix = "exp-stg-";

        private static readonly string[] SharedAccessQueryParameters =
        {
            "sv",
            "sr",
            "si",
            "sk",
            "sig",
            "spr",
            "sip",
            "st",
            "se",
            "sp",
            "rscc",
            "rsct",
            "rsce",
            "rscl",
            "rscd"
        };

        // https://docs.microsoft.com/en-us/rest/api/storageservices/common-rest-api-error-codes
        // https://docs.microsoft.com/en-us/rest/api/storageservices/blob-service-error-codes
        private static readonly string[] TerminalAzureStorageErrorCodes =
        {
            "ContainerBeingDeleted",
            "ContainerDisabled",
            "ContainerNotFound",
            "AccountIsDisabled",
            "AuthenticationFailed",
            "InsufficientAccountPermissions",
            "InvalidAuthenticationInfo",
            "AuthorizationFailure"
        };

        private readonly CloudStorageAccount[] storageAccounts;

        public static IExportStorageManager Instance { get; } = new ExportStorageManager(Config.Instance.ExportStorage);

        /// <inheritdoc />
        public IReadOnlyCollection<Uri> AccountUris => this.storageAccounts.Select(a => a.BlobEndpoint).ToList().AsReadOnly();

        /// <summary>
        ///     Constructs a new ExportStorageManager that looks up from config the managed storage accounts.
        /// </summary>
        public ExportStorageManager(Configuration_ExportStorage config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var storageAccounts = new List<CloudStorageAccount>();
            if (!string.IsNullOrWhiteSpace(config.ConnectionStringA))
            {
                storageAccounts.Add(CloudStorageAccount.Parse(config.ConnectionStringA));
            }

            if (!string.IsNullOrWhiteSpace(config.ConnectionStringB))
            {
                storageAccounts.Add(CloudStorageAccount.Parse(config.ConnectionStringB));
            }

            DualLogger.Instance.Information(nameof(ExportStorageManager), $"Found {storageAccounts.Count} storage accounts.");
            this.storageAccounts = storageAccounts.ToArray();
        }

        /// <inheritdoc />
        public async Task CleanupContainerAsync(Uri containerUri, CommandId commandId)
        {
            if (!this.IsManaged(containerUri))
            {
                // If the container is for a storage account that isn't ours, don't do any cleanup.
                return;
            }

            // Get the container
            CloudBlobContainer container = this.GetFullAccessContainer(containerUri);

            DualLogger.Instance.Information(nameof(ExportStorageManager), $"Cleaning up container {container.Name} in storage account {container.ServiceClient.Credentials.AccountName} for command {commandId}");

            // Delete the container
            try
            {
                await container.DeleteAsync();
                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "ExportContainerDelete").Increment();
            }
            catch (StorageException ex)
            {
                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "ExportContainerDelete:Failed").Increment();

                // If the container does not exist, just log and return.
                if (ex.RequestInformation?.ExtendedErrorInformation?.ErrorCode == "ContainerNotFound")
                {
                    DualLogger.Instance.Information(nameof(ExportStorageManager), $"Could not find container {container.Name} in storage account {container.ServiceClient.Credentials.AccountName} for command {commandId}");
                    return;
                }

                throw;
            }
        }

        /// <inheritdoc />
        public IEnumerable<Func<Task>> CleanupOldContainersAsync()
        {
            return this.storageAccounts.Select(a => new Func<Task>(() => CleanupOldContainersAsync(a)));
        }

        /// <inheritdoc />
        public CloudBlobContainer GetFullAccessContainer(Uri containerUri)
        {
            // First, remove any existing shared access signature parameters.
            NameValueCollection queryParameters = HttpUtility.ParseQueryString(containerUri.Query);
            foreach (string sharedAccessQueryParameter in SharedAccessQueryParameters)
            {
                queryParameters.Remove(sharedAccessQueryParameter);
            }

            var uriBuilder = new UriBuilder(containerUri) { Query = queryParameters.ToString() };

            // Next, find the storage account.
            CloudStorageAccount storageAccount = this.GetStorageAccount(containerUri);

            // Finally, return a blob container using our credentials.
            return new CloudBlobContainer(uriBuilder.Uri, storageAccount.Credentials);
        }

        /// <inheritdoc />
        public async Task<Uri> GetOrCreateFinalContainerAsync(Uri storageUri, CommandId commandId)
        {
            CloudStorageAccount account = this.GetStorageAccount(storageUri);

            CloudBlobContainer container = await CreateContainerAsync(account, commandId, FinalContainerPrefix, commandId.Value);

            return container.Uri;
        }

        /// <inheritdoc />
        public async Task<Uri> GetOrCreateStagingContainerAsync(Uri storageUri, CommandId commandId, AgentId agentId, AssetGroupId assetGroupId)
        {
            CloudStorageAccount account = this.GetStorageAccount(storageUri);

            CloudBlobContainer container = await CreateContainerAsync(account, commandId, StagingContainerPrefix, commandId.Value, agentId.Value, assetGroupId.Value);

            var policy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Add | SharedAccessBlobPermissions.Create | SharedAccessBlobPermissions.Write,
                SharedAccessExpiryTime = DateTimeOffset.UtcNow + TimeSpan.FromDays(30)
            };
            string token = container.GetSharedAccessSignature(policy);

            return new Uri(container.Uri, token);
        }

        /// <inheritdoc />
        [SuppressMessage("Microsoft.Usage", "CA2234:PassSystemUriObjectsInsteadOfStrings", Justification = "Incorrect analysis. The token below is not a Uri")]
        public Uri GetReadOnlyContainerUri(Uri containerUri)
        {
            if (!this.IsManaged(containerUri))
            {
                // If the container is for storage that isn't ours, there's no shared access signature to generate.
                return containerUri;
            }

            // Get the container
            CloudBlobContainer container = this.GetFullAccessContainer(containerUri);

            DualLogger.Instance.Information(nameof(ExportStorageManager), $"Creating readonly uri for final container {container.Name} in storage account {container.ServiceClient.Credentials.AccountName}");

            var policy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTimeOffset.UtcNow + TimeSpan.FromDays(30)
            };
            string token = container.GetSharedAccessSignature(policy);

            return new Uri(container.Uri, token);
        }

        /// <inheritdoc />
        public async Task<string> GetContainerErrorCodeAsync(Uri containerUri, string fileName, string fileContent)
        {
            try
            {
                var container = new CloudBlobContainer(containerUri);

                CloudBlockBlob blob = container.GetBlockBlobReference(fileName);
                await blob.UploadTextAsync(fileContent);
            }
            catch (StorageException storageException)
            {
                IncomingEvent.Current?.SetProperty("ExportStorageExceptionMessage", storageException.RequestInformation?.ExtendedErrorInformation?.ErrorMessage);
                IncomingEvent.Current?.SetProperty("ExportStorageExceptionErrorCode", storageException.RequestInformation?.ExtendedErrorInformation?.ErrorCode);

                if (TerminalAzureStorageErrorCodes.Contains(storageException.RequestInformation?.ExtendedErrorInformation?.ErrorCode))
                {
                    PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "ExportStorageLost").Increment();
                    return storageException.RequestInformation.ExtendedErrorInformation.ErrorCode;
                }
                
                if ((storageException.InnerException as WebException)?.Status == WebExceptionStatus.NameResolutionFailure)
                {
                    PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "ExportStorageLost").Increment();
                    return WebExceptionStatus.NameResolutionFailure.ToString();
                }

                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "ExportStorageLost:Temporary").Increment();
            }
            catch (Exception ex)
            {
                // Swallow everything
                DualLogger.Instance.Error(nameof(ExportStorageManager), ex, $"{nameof(this.GetContainerErrorCodeAsync)} exception");
                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "ExportStorageLost:Temporary").Increment();
            }

            return null;
        }

        /// <inheritdoc />
        public bool IsManaged(Uri storageUri)
        {
            if (storageUri == null)
            {
                return false;
            }

            return this.GetStorageAccount(storageUri) != null;
        }

        public Uri GetManagedStorageUri()
        {
            return new Uri("https://" + this.storageAccounts[0].BlobStorageUri.PrimaryUri.Host);
        }

        private CloudStorageAccount GetStorageAccount(Uri storageUri)
        {
            return this.storageAccounts.FirstOrDefault(a => a.BlobStorageUri.PrimaryUri.Host == storageUri.Host || a.BlobStorageUri.SecondaryUri.Host == storageUri.Host);
        }

        private static async Task CleanupOldContainersAsync(CloudStorageAccount account)
        {
            DualLogger.Instance.Information(nameof(ExportStorageManager), $"Cleaning up old containers in account {account.Credentials.AccountName}");

            try
            {
                DateTimeOffset oldestAllowed = DateTimeOffset.UtcNow.AddDays(-Config.Instance.Worker.Tasks.ExportStorageCleanupTask.MaxAgeDays);
                CloudBlobClient client = account.CreateCloudBlobClient();

                BlobContinuationToken continuation = null;
                do
                {
                    ContainerResultSegment segment = await client.ListContainersSegmentedAsync(FinalContainerPrefix, continuation);
                    foreach (CloudBlobContainer container in segment.Results)
                    {
                        if (container.Properties.LastModified != null && container.Properties.LastModified < oldestAllowed)
                        {
                            try
                            {
                                DualLogger.Instance.Information(nameof(ExportStorageManager), $"Deleting old container {container.Name} in storage account {account.Credentials.AccountName}");
                                await container.DeleteAsync(AccessCondition.GenerateIfMatchCondition(container.Properties.ETag), null, null);
                                DualLogger.Instance.Information(nameof(ExportStorageManager), $"Deleted old container {container.Name} in storage account {account.Credentials.AccountName}");
                                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "ExportOldContainerDelete").Increment();
                            }
                            catch (Exception ex)
                            {
                                DualLogger.Instance.Error(nameof(ExportStorageManager), ex, $"Error deleting container {container.Name} in storage account {account.Credentials.AccountName}");
                                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "ExportOldContainerDelete:Failed").Increment();
                            }
                        }
                    }

                    continuation = segment.ContinuationToken;
                }
                while (continuation != null);
            }
            catch (Exception ex)
            {
                DualLogger.Instance.Error(nameof(ExportStorageManager), ex, "Cleaning up old containers failed.");
            }

            DualLogger.Instance.Information(nameof(ExportStorageManager), $"Finished cleaning up old containers in account {account.Credentials.AccountName}");
        }

        private static async Task<CloudBlobContainer> CreateContainerAsync(CloudStorageAccount account, CommandId commandId, string containerPrefix, params string[] hashStrings)
        {
            // Get a container name that is consistent for the given tuple of command, agent, and asset group
            string containerName;
            using (SHA256 hash = SHA256.Create())
            {
                byte[] bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(string.Join("_", hashStrings)));

                containerName = containerPrefix + string.Join(string.Empty, bytes.Select(b => b.ToString("x2"))).Substring(0, ContainerNameLengthLimit - containerPrefix.Length);
            }

            // Create the container
            CloudBlobContainer container = account.CreateCloudBlobClient().GetContainerReference(containerName);
            try
            {
                if (await container.CreateIfNotExistsAsync())
                {
                    DualLogger.Instance.Information(nameof(ExportStorageManager), $"Created container {container.Name} in storage account {container.ServiceClient.Credentials.AccountName} for command {commandId}");
                    PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "ExportContainerCreate").Increment();
                }
            }
            catch (Exception ex)
            {
                DualLogger.Instance.Error(nameof(ExportStorageManager), ex, $"Failed to create container {container.Name} in storage account {container.ServiceClient.Credentials.AccountName} for command {commandId}");
                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "ExportContainerCreate:Failed").Increment();
                throw;
            }

            return container;
        }
    }
}
