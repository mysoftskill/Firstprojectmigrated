// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Azure.Storage.Auth;
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.Azure.Storage.Queue;
    using Microsoft.Membership.MemberServices.Common.SecretStore;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.PrivacyServices.Common.Azure;
    using CloudStorageAccount = Microsoft.Azure.Storage.CloudStorageAccount;
    using StorageCredentials = Microsoft.Azure.Storage.Auth.StorageCredentials;
    using StorageUri = Microsoft.Azure.Storage.StorageUri;
    using TableStorageAccount = Microsoft.Azure.Cosmos.Table.CloudStorageAccount;
    using TableStorageCredentials = Microsoft.Azure.Cosmos.Table.StorageCredentials;

    /// <inheritdoc />
    public class AzureStorageProvider : IAzureStorageProvider
    {
        /// <summary>
        ///     The class name used for logging annotations.
        /// </summary>
        private const string ClassName = nameof(AzureStorageProvider);

        private readonly ILogger logger;

        private readonly ISecretStoreReader secretStoreReader;

        private CloudStorageAccount storageAccount;

        private TableStorageAccount tableStorageAccount;

        /// <summary>
        ///     Holds the AccountName information when MSI auth is used in which case storageAccount.Credentials.AccountName does not have a valid value.
        /// </summary>
        private string accountName = "<NONE>";

        /// <summary>
        ///     Gets the account name of the storage account
        /// </summary>
        public string AccountName => this.storageAccount?.Credentials?.AccountName ?? accountName;

        /// <summary>
        ///     Gets the endpoints for the Queue service at the primary and secondary location, as configured for the storage account
        /// </summary>
        public StorageUri QueueStorageUri => this.storageAccount?.QueueStorageUri;

        /// <summary>
        ///     Initializes a new instance of the AzureStorageProvider class
        /// </summary>
        /// <param name="log">trace logger</param>
        /// <param name="secretStoreReader">secret store reader</param>
        public AzureStorageProvider(
            ILogger log,
            ISecretStoreReader secretStoreReader)
        {
            this.logger = log;
            this.secretStoreReader = secretStoreReader;
        }

        /// <summary>
        ///     Get a cloud blob client
        /// </summary>
        /// <returns>a cloud blob client</returns>
        public CloudBlobClient CreateCloudBlobClient()
        {
            this.ValidateObjectInitialized();
            return this.storageAccount.CreateCloudBlobClient();
        }

        /// <summary>
        ///     Get a Cloud Queue Client
        /// </summary>
        /// <returns>a cloud queue client</returns>
        public CloudQueueClient CreateCloudQueueClient()
        {
            this.ValidateObjectInitialized();
            return this.storageAccount.CreateCloudQueueClient();
        }

        /// <summary>
        ///     Get a Cloud Table Client
        /// </summary>
        /// <returns>a cloud table client</returns>
        public CloudTableClient CreateCloudTableClient()
        {
            this.ValidateObjectInitialized();
            return this.tableStorageAccount.CreateCloudTableClient();
        }

        /// <summary>
        ///     Get the cloud table wrapper for the table
        /// </summary>
        /// <param name="name">name of the table to fetch</param>
        /// <param name="cancellationToken">cancellation token to monitor</param>
        /// <returns>a cloud queue</returns>
        public async Task<ICloudQueue> GetCloudQueueAsync(
            string name,
            CancellationToken cancellationToken)
        {
            CloudQueueClient client = this.CreateCloudQueueClient();
            CloudQueue result;

            result = client.GetQueueReference(name);
            await result.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);

            return new CloudQueueWrapper(result);
        }

        /// <summary>
        ///     Get the cloud table wrapper for the table
        /// </summary>
        /// <param name="name">name of the table to fetch</param>
        /// <returns>a cloud table</returns>
        public CloudTableWrapper GetCloudTable(string name)
        {
            CloudTableClient tableClient = this.CreateCloudTableClient();
            CloudTable cloudTable = tableClient.GetTableReference(name);

            cloudTable.CreateIfNotExists();

            return new CloudTableWrapper(cloudTable);
        }

        /// <summary>
        ///     Get the cloud table wrapper for the table
        /// </summary>
        /// <param name="name">name of the table to fetch</param>
        /// <returns>a cloud queue</returns>
        public async Task<ICloudTable> GetCloudTableAsync(string name)
        {
            CloudTableClient client = this.CreateCloudTableClient();
            CloudTable result;

            result = client.GetTableReference(name);
            await result.CreateIfNotExistsAsync().ConfigureAwait(false);

            return new CloudTableWrapper(result);
        }

        /// <summary>
        ///     Initialize Azure Storage using a custom connection string
        /// </summary>
        /// <param name="connectionString">connection string for storage account</param>
        public void Initialize(string connectionString)
        {
            this.storageAccount = CloudStorageAccount.Parse(connectionString);
        }

        /// <summary>
        ///     Initialize Azure Storage using the privacy experience configuration
        /// </summary>
        /// <param name="serviceConfig">service configuration</param>
        /// <returns>resulting task</returns>
        public Task InitializeAsync(IPrivacyExperienceServiceConfiguration serviceConfig)
        {
            return this.InitializeAsync(serviceConfig.AzureStorageConfiguration);
        }

        /// <summary>
        ///     Initialize Azure Storage using the azureStorageConfiguration
        /// </summary>
        /// <param name="config">azure storage configuration</param>
        /// <returns>resulting task</returns>
        public async Task InitializeAsync(IAzureStorageConfiguration config)
        {
            const string methodName = ClassName + "." + nameof(this.InitializeAsync) + "." + nameof(IAzureStorageConfiguration);
            ArgumentCheck.ThrowIfNull(this.logger, nameof(this.logger), null, methodName);

            if (this.storageAccount != null)
            {
                return;
            }

            if (!config.UseEmulator)
            {
                this.logger.Information(ClassName, "Initializing Azure storage provider with cloud storage");
                try
                {
                    this.accountName = config.AccountName;

                    // Retrieve the storage credentials using the app identity for the storage account for blob, queue, and file storage.
                    StorageCredentials storageCredentials = this.GetStorageWithMSICredentials(config.ResourceId);

                    // Retrieve the storage credentials for the storage account for table storage using the keyvault connection string.
                    TableStorageCredentials tableStorageCredentials = new TableStorageCredentials(
                        config.AccountName,
                        await this.secretStoreReader.ReadSecretByNameAsync(config.AuthKeyEncryptedFilePath).ConfigureAwait(false));

                    // Setup the storage accounts for blob, file, queue, and table access 
                    this.logger.Information(
                        ClassName,
                        $"Attempting to initialize using cloud configuration, endpoint suffix: {config.StorageEndpointSuffix}");

                    this.storageAccount = new CloudStorageAccount(
                        storageCredentials: storageCredentials,
                        accountName: config.AccountName,
                        useHttps: true,
                        endpointSuffix: config.StorageEndpointSuffix);
                    this.tableStorageAccount = new TableStorageAccount(
                        storageCredentials: tableStorageCredentials,
                        accountName: config.AccountName,
                        useHttps: true,
                        endpointSuffix: config.StorageEndpointSuffix);
                }
                catch (Exception e)
                {
                    this.logger.Error(
                        ClassName,
                        e,
                        $"Azure storage provider failed to initialize using storage account name: {config.AccountName}, endpoint suffix: {config.StorageEndpointSuffix}");
                    throw;
                }
            }
            else // Local box only
            {
                this.logger.Information(
                    ClassName,
                    "Initializing Azure storage provider with developer storage");

                // Use the develpment storage configuration
                this.storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
                this.tableStorageAccount = TableStorageAccount.DevelopmentStorageAccount;
            }

            try
            {
                ICollection<ITaggedServicePointConfiguration> svcPointCfgOverrides =
                    config.OverrideServicePointConfigurations;

                IServicePointConfiguration svcPointCfg = config.ServicePointConfiguration;

                if (svcPointCfg != null || (svcPointCfgOverrides != null && svcPointCfgOverrides.Any()))
                {
                    this.ApplyServicePointConfig(
                        this.storageAccount.BlobStorageUri,
                        AzureStorageObjectType.Blob,
                        svcPointCfgOverrides,
                        svcPointCfg);

                    this.ApplyServicePointConfig(
                        this.storageAccount.FileStorageUri,
                        AzureStorageObjectType.File,
                        svcPointCfgOverrides,
                        svcPointCfg);

                    this.ApplyServicePointConfig(
                        this.storageAccount.QueueStorageUri,
                        AzureStorageObjectType.Queue,
                        svcPointCfgOverrides,
                        svcPointCfg);

                    this.ApplyServicePointConfig(
                        this.storageAccount.TableStorageUri,
                        AzureStorageObjectType.Table,
                        svcPointCfgOverrides,
                        svcPointCfg);
                }
            }
            catch (Exception e)
            {
                this.logger.Error(
                    ClassName,
                    e,
                    "Azure storage provider failed to configure service point configurations for " + config.AccountName);
                throw;
            }

            this.logger.Information(ClassName, $"Successfully initialized storage account name: {config.AccountName}");
        }

        /// <summary>
        ///     Applies the service point conifg to the primary and backup storage URIs
        /// </summary>
        /// <param name="uris">URIs to apply to</param>
        /// <param name="type">storage object type</param>
        /// <param name="typeConfigs">storage object type-specific service point config set</param>
        /// <param name="defaultConfig">default service point config</param>
        private void ApplyServicePointConfig(
            StorageUri uris,
            AzureStorageObjectType type,
            IEnumerable<ITaggedServicePointConfiguration> typeConfigs,
            IServicePointConfiguration defaultConfig)
        {
            void Apply(
                Uri uri,
                IServicePointConfiguration config)
            {
                if (uri != null && config != null)
                {
                    ServicePoint sp = ServicePointManager.FindServicePoint(uri);
                    sp.ConnectionLeaseTimeout = config.ConnectionLeaseTimeout;
                    sp.UseNagleAlgorithm = config.UseNagleAlgorithm;
                    sp.ConnectionLimit = config.ConnectionLimit;
                    sp.MaxIdleTime = config.MaxIdleTime;
                }
            }

            if (uris != null)
            {
                IServicePointConfiguration configActual;
                string tag = type.ToString();

                configActual = typeConfigs.FirstOrDefault(o => tag.EqualsIgnoreCase(o.Tag)) ?? defaultConfig;

                Apply(uris.PrimaryUri, configActual);
                Apply(uris.SecondaryUri, configActual);
            }
        }

        /// <summary>
        ///     Validates the storage provider has been initialized
        /// </summary>
        private void ValidateObjectInitialized()
        {
            if (this.storageAccount == null)
            {
                throw new InvalidOperationException("Storage provider has not been initialized");
            }
        }

        /// <summary>
        ///     Get storage credentials using managed identity
        /// </summary>
        /// <returns>storage credentials</returns>
        private StorageCredentials GetStorageWithMSICredentials(string resourceId)
        {
            // Get the initial access token and the interval at which to refresh it.
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            NewTokenAndFrequency tokenAndFrequency = TokenRenewerAsync(resourceId, azureServiceTokenProvider, CancellationToken.None).GetAwaiter().GetResult();

            TokenCredential tokenCredential = new TokenCredential(
                tokenAndFrequency.Token,
                (tokenProvider, cancellationToken) => TokenRenewerAsync(resourceId, tokenProvider, cancellationToken),
                azureServiceTokenProvider,
                tokenAndFrequency.Frequency.Value);

            return new StorageCredentials(tokenCredential);
        }

        /// <summary>
        ///     Renews the authorization tokens for the managed identity connection before token expiration 
        /// </summary>
        /// <param name="resource">the resource ID for requesting Azure AD tokens </param>
        /// <param name="tokenProvider">the current Azure Service Token Provider</param>
        /// <param name="token">cancellation token to monitor</param>
        /// <returns>a new token with update frequency</returns>
        /// string resource, object tokenProvider, CancellationToken token
        private async Task<NewTokenAndFrequency> TokenRenewerAsync(string resource, Object tokenProvider, CancellationToken token)
        {
            AppAuthenticationResult authResult = await ((AzureServiceTokenProvider)tokenProvider).GetAuthenticationResultAsync(resource, cancellationToken: token);

            // Renew the token 5 minutes before it expires.
            TimeSpan next = authResult.ExpiresOn - DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5);
            if (next.Ticks <= 0)
            {
                next = default;
            }

            // Return the new token and the next refresh time.
            return new NewTokenAndFrequency(authResult.AccessToken, next);
        }
    }
}