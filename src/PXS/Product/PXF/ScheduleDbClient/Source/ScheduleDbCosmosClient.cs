// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.ScheduleDbClient
{
    using global::Azure.Identity;
    using global::Azure.Core;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.SecretStore;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Model;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Exceptions;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     ScheduleDbClient Cosmos client
    /// </summary>
    public class ScheduleDbCosmosClient : IScheduleDbClient
    {
        /// <summary>
        /// Defines the cosmos client.
        /// </summary>
        public CosmosClient cosmosClient;

        private Container container;

        private readonly ILogger logger;

        private const string emulatorEndPoint = "https://localhost:8081";

        private const string partitionKey = "/id";

        private IScheduleDbConfiguration scheduleDbConfig;

        private IPrivacyConfigurationManager privacyConfig;

        private IRecurringDeleteWorkerConfiguration recurringDeleteWorkerConfig;

        private readonly IAppConfiguration appConfiguration;

        /// <summary>
        ///  Creates an instance of <see cref="ScheduleDbCosmosClient" />
        /// </summary>
        /// <param name="privacyConfigurationManager"></param>
        /// <param name="appConfiguration"></param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ScheduleDbCosmosClient(IPrivacyConfigurationManager privacyConfigurationManager, IAppConfiguration appConfiguration, ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            this.InitializeConfigurations(privacyConfigurationManager);

            var clientOption = new CosmosClientOptions
            {
                // Do they need to be configurable?
                MaxRetryAttemptsOnRateLimitedRequests = 5,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(60),
                ConnectionMode = ConnectionMode.Gateway,
                SerializerOptions = new CosmosSerializationOptions()
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            };

            if (this.scheduleDbConfig.UseEmulator)
            {
                this.logger.Information(nameof(ScheduleDbCosmosClient), "Creating CosmosClient for emulator.");
                CreateEmulatorClient(clientOption);
            }
            else
            {
                try
                {
                    this.logger.Information(nameof(ScheduleDbCosmosClient), "Getting CosmosClient for cloud environment.");
                    TokenCredential credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = this.recurringDeleteWorkerConfig.RecurringDeleteUamiId });
                    this.cosmosClient = new CosmosClient(this.scheduleDbConfig.CosmosDbEndPoint, credential, clientOption);
                    this.container = this.cosmosClient.GetContainer(this.scheduleDbConfig.DataBaseName, this.scheduleDbConfig.ContainerName);
                    this.logger.Information(nameof(ScheduleDbCosmosClient), "CosmosClient for cloud environment created successfully.");
                }
                catch (Exception ex)
                { 
                    this.logger.Error(nameof(ScheduleDbCosmosClient), ex, $"CosmosClient initialization failed with error  = {ex.Message}");
                    throw new ScheduleDbClientException(nameof(ScheduleDbCosmosClient), ex.Message, ex);
                }
            }
        }

        // Used for Unit test.
        public ScheduleDbCosmosClient(CosmosClient cosmosClient, IPrivacyConfigurationManager privacyConfigurationManager, IAppConfiguration appConfiguration, ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            this.InitializeConfigurations(privacyConfigurationManager);
            this.cosmosClient = cosmosClient;
            this.container = this.cosmosClient.GetContainer(this.scheduleDbConfig.DataBaseName, this.scheduleDbConfig.ContainerName);
        }

        /// <inheritdoc />
        public async Task<RecurrentDeleteScheduleDbDocument> CreateOrUpdateRecurringDeletesScheduleDbAsync(RecurrentDeleteScheduleDbDocument createOrUpdateRecurringDeletesArgs)
        {
            if (createOrUpdateRecurringDeletesArgs == null)
                throw new ArgumentNullException(nameof(createOrUpdateRecurringDeletesArgs));

            if (string.IsNullOrEmpty(createOrUpdateRecurringDeletesArgs.DocumentId))
                createOrUpdateRecurringDeletesArgs.DocumentId = Guid.NewGuid().ToString();

            if (string.IsNullOrEmpty(createOrUpdateRecurringDeletesArgs.DataType))
                throw new ArgumentNullException(nameof(createOrUpdateRecurringDeletesArgs.DataType));

            createOrUpdateRecurringDeletesArgs.UpdateDateUtc = DateTimeOffset.UtcNow;

            try
            {
                IList<RecurrentDeleteScheduleDbDocument> itemResponseList = await this.DoWorkInstrumentedAsync(
                                        nameof(ScheduleDbCosmosClient.CreateOrUpdateRecurringDeletesScheduleDbAsync),
                                        HttpMethod.Get,
                                        this.ReadEntriesAsync(this.GetPuidAndDataTypeQueryText(createOrUpdateRecurringDeletesArgs.Puid, createOrUpdateRecurringDeletesArgs.DataType), CancellationToken.None))
                                        .ConfigureAwait(false);

                if (itemResponseList.Count > 1)
                {
                    throw new ScheduleDbClientException("More than one record found in scheduledb for create or update operation");
                }
                if (!itemResponseList.Any())
                {
                    return await this.CreateRecurringDeletesScheduleDbAsync(createOrUpdateRecurringDeletesArgs).ConfigureAwait(false);
                }
                else
                {
                    var existingDocument = itemResponseList.First();
                    existingDocument = RecurrentDeleteScheduleDbDocument.UpdateRecurringDeletesScheduleDbAsync(existingDocument, createOrUpdateRecurringDeletesArgs);
                    return await this.UpdateRecurringDeletesScheduleDbAsync(existingDocument).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(nameof(ScheduleDbCosmosClient.CreateRecurringDeletesScheduleDbAsync), ex, $"Create Or Update Recurring Deletes failed with error = {ex.Message}");
                throw new ScheduleDbClientException(nameof(ScheduleDbCosmosClient.CreateRecurringDeletesScheduleDbAsync), ex.Message, ex);
            }
        }

        /// <inheritdoc />
        public async Task<RecurrentDeleteScheduleDbDocument> GetRecurringDeletesScheduleDbDocumentAsync(string documentId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(documentId))
            { 
                throw new ArgumentNullException(nameof(documentId));
            }

            try
            {
                var result = new List<RecurrentDeleteScheduleDbDocument>();
                using (FeedIterator<RecurrentDeleteScheduleDbDocument> feedIterator = this.container.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                $"SELECT * FROM ScheduleDb sdb WHERE sdb.id = \"{documentId}\""))
                {
                    while (feedIterator.HasMoreResults)
                    {
                        var response = await this.DoWorkInstrumentedAsync(
                            nameof(ScheduleDbCosmosClient.GetRecurringDeletesScheduleDbDocumentAsync),
                            HttpMethod.Get,
                            feedIterator.ReadNextAsync(cancellationToken))
                            .ConfigureAwait(false);
                        
                        result.AddRange(response.AsEnumerable().ToList());
                    }
                }

                if (result.Count > 1)
                {
                    this.logger.Error(nameof(ScheduleDbCosmosClient.GetRecurringDeletesScheduleDbDocumentAsync), $"More than one document found for documentId");
                    throw new ScheduleDbClientException($"{nameof(ScheduleDbCosmosClient.GetRecurringDeletesScheduleDbDocumentAsync)} More than one document found for documentId") ;
                }

                return result.Any() ? result.First() : null;
            }
            catch (CosmosException ex)
            {
                this.logger.Error(nameof(ScheduleDbCosmosClient.GetRecurringDeletesScheduleDbDocumentAsync), ex, $"Get document by documentId call failed with error = {ex.Message}");
                throw new ScheduleDbClientException(nameof(ScheduleDbCosmosClient.GetRecurringDeletesScheduleDbDocumentAsync), ex.Message, ex);
            }
        }

        /// <inheritdoc />
        public async Task DeleteRecurringDeletesByPuidScheduleDbAsync(long puidValue, CancellationToken cancellationToken)
        {
            IList<RecurrentDeleteScheduleDbDocument> items = await this.DoWorkInstrumentedAsync(
                        nameof(ScheduleDbCosmosClient.DeleteRecurringDeletesByPuidScheduleDbAsync),
                        HttpMethod.Get,
                        this.ReadEntriesAsync(this.GetPuidQueryText(puidValue), cancellationToken))
                        .ConfigureAwait(false);

            foreach (var item in items)
            {
                await this.DoWorkInstrumentedAsync(
                    nameof(ScheduleDbCosmosClient.DeleteRecurringDeletesByPuidScheduleDbAsync),
                    HttpMethod.Delete,
                    this.container.DeleteItemAsync<RecurrentDeleteScheduleDbDocument>(item.DocumentId, new PartitionKey(item.DocumentId)))
                    .ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task DeleteRecurringDeletesScheduleDbAsync(long puidValue, string dataType, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(dataType))
                throw new ArgumentNullException(nameof(dataType));

            IList<RecurrentDeleteScheduleDbDocument> items = await this.DoWorkInstrumentedAsync(
                        nameof(ScheduleDbCosmosClient.DeleteRecurringDeletesScheduleDbAsync),
                        HttpMethod.Get,
                        this.ReadEntriesAsync(this.GetPuidAndDataTypeQueryText(puidValue, dataType), cancellationToken))
                        .ConfigureAwait(false);

            if (!items.Any())
            {
                throw new ScheduleDbClientException("No record found in scheduledb for delete operation");
            }

            if (items.Count > 1)
            {
                throw new ScheduleDbClientException("More than one record found in scheduledb for delete operation");
            }

            await this.DoWorkInstrumentedAsync(
                        nameof(ScheduleDbCosmosClient.DeleteRecurringDeletesScheduleDbAsync),
                        HttpMethod.Delete,
                        this.container.DeleteItemAsync<RecurrentDeleteScheduleDbDocument>(items.FirstOrDefault().DocumentId, new PartitionKey(items.FirstOrDefault().DocumentId)))
                        .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<(IList<RecurrentDeleteScheduleDbDocument>, string continuationToken)> GetApplicableRecurringDeletesScheduleDbAsync(DateTimeOffset expectedNextDeleteOccuranceUtc, string continuationToken = null, int maxItemCount = 100)
        {
            return await this.DoWorkInstrumentedAsync(
                        nameof(ScheduleDbCosmosClient.GetApplicableRecurringDeletesScheduleDbAsync),
                        HttpMethod.Get,
                        this.ReadEntriesByContinuationTokenAsync(this.GetExpectedNextDeleteOccurrenceQueryText(expectedNextDeleteOccuranceUtc), continuationToken, maxItemCount))
                        .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<(IList<RecurrentDeleteScheduleDbDocument>, string continuationToken)> GetExpiredPreVerifiersRecurringDeletesScheduleDbAsync(DateTimeOffset preVerifierExpirationDate, string continuationToken = null, int maxItemCount = 100)
        {
            return await this.DoWorkInstrumentedAsync(
                        nameof(ScheduleDbCosmosClient.GetExpiredPreVerifiersRecurringDeletesScheduleDbAsync),
                        HttpMethod.Get,
                        this.ReadEntriesByContinuationTokenAsync(this.GetPreVerifierExpirationDateQueryText(preVerifierExpirationDate), continuationToken, maxItemCount))
                        .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IList<RecurrentDeleteScheduleDbDocument>> GetRecurringDeletesScheduleDbAsync(long puidValue, CancellationToken cancellationToken)
        {
            return await this.DoWorkInstrumentedAsync(
                    nameof(ScheduleDbCosmosClient.GetRecurringDeletesScheduleDbAsync),
                    HttpMethod.Get,
                    this.ReadEntriesAsync(this.GetPuidQueryText(puidValue), cancellationToken))
                    .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> HasRecurringDeletesScheduleDbRecordAsync(long puidValue, string dataType, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(dataType))
                throw new ArgumentNullException(nameof(dataType));

            IList<RecurrentDeleteScheduleDbDocument> items = await this.DoWorkInstrumentedAsync(
                    nameof(ScheduleDbCosmosClient.HasRecurringDeletesScheduleDbRecordAsync),
                    HttpMethod.Get,
                    this.ReadEntriesAsync(this.GetPuidAndDataTypeQueryText(puidValue, dataType), cancellationToken))
                    .ConfigureAwait(false);

            return items.Any() && items.Count == 1;
        }

        /// <inheritdoc />
        public async Task<RecurrentDeleteScheduleDbDocument> GetRecurringDeletesScheduleDbDocumentAsync(long puidValue, string dataType, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(dataType))
                throw new ArgumentNullException(nameof(dataType));

            IList<RecurrentDeleteScheduleDbDocument> items = await this.DoWorkInstrumentedAsync(
                    nameof(ScheduleDbCosmosClient.GetRecurringDeletesScheduleDbDocumentAsync),
                    HttpMethod.Get,
                    this.ReadEntriesAsync(this.GetPuidAndDataTypeQueryText(puidValue, dataType), cancellationToken))
                    .ConfigureAwait(false);

            if (items.Count() > 1)
            {
                throw new ScheduleDbClientException("More than one record found in scheduledb for given puid and datatype.");
            }

            return items.FirstOrDefault<RecurrentDeleteScheduleDbDocument>();
        }

        private void InitializeConfigurations(IPrivacyConfigurationManager privacyConfigurationManager)
        {
            this.privacyConfig = privacyConfigurationManager ?? throw new ArgumentNullException(nameof(privacyConfigurationManager));
            this.recurringDeleteWorkerConfig = this.privacyConfig.RecurringDeleteWorkerConfiguration ?? throw new ArgumentNullException(nameof(this.privacyConfig.RecurringDeleteWorkerConfiguration));
            this.scheduleDbConfig = this.recurringDeleteWorkerConfig.ScheduleDbConfig ?? throw new ArgumentNullException(nameof(this.recurringDeleteWorkerConfig.ScheduleDbConfig));
            this.logger.Information(nameof(ScheduleDbCosmosClient), "Initialized ScheduleDbCosmosClient configs and logger.");
        }

        private void CreateEmulatorClient(CosmosClientOptions clientOption)
        {
            var azureKeyVaultReader = new AzureKeyVaultReader(this.privacyConfig, new Clock(), this.logger);
            if (azureKeyVaultReader == null)
            {
                throw new ArgumentNullException(nameof(azureKeyVaultReader));
            }

            var azureKeyVaultConfig = this.privacyConfig.AzureKeyVaultConfiguration ?? throw new ArgumentNullException(nameof(this.privacyConfig.AzureKeyVaultConfiguration));
            var cosmosDbEmulatorPrimaryKey = azureKeyVaultConfig.CosmosDbEmulatorPrimaryKey;
            if (string.IsNullOrEmpty(cosmosDbEmulatorPrimaryKey))
            {
                throw new ArgumentNullException(nameof(cosmosDbEmulatorPrimaryKey));
            }

            var primaryKey = azureKeyVaultReader.ReadSecretByNameAsync(cosmosDbEmulatorPrimaryKey).GetAwaiter().GetResult();
            if (string.IsNullOrEmpty(primaryKey))
            { 
                throw new ArgumentNullException(nameof(primaryKey));
            }

            this.cosmosClient = new CosmosClient(emulatorEndPoint, primaryKey, clientOption);
            Database dataBase = this.cosmosClient.CreateDatabaseIfNotExistsAsync(this.scheduleDbConfig.DataBaseName).GetAwaiter().GetResult();
            this.container = dataBase.CreateContainerIfNotExistsAsync(this.scheduleDbConfig.ContainerName, partitionKey).GetAwaiter().GetResult();
        }

        private async Task<RecurrentDeleteScheduleDbDocument> CreateRecurringDeletesScheduleDbAsync(RecurrentDeleteScheduleDbDocument createRecurringDeletesArgs)
        {
            CheckRecurrentDeleteScheduleDbDocumentArgs(createRecurringDeletesArgs);

            try
            {
                var itemResponse = await this.DoWorkInstrumentedAsync(
                    nameof(ScheduleDbCosmosClient.CreateRecurringDeletesScheduleDbAsync),
                    HttpMethod.Post,
                    this.container.CreateItemAsync(createRecurringDeletesArgs, new PartitionKey(createRecurringDeletesArgs.DocumentId)))
                    .ConfigureAwait(false);
                this.logger.Information(nameof(ScheduleDbCosmosClient.CreateRecurringDeletesScheduleDbAsync), "Document created successfully.");
                return itemResponse;
            }
            catch (CosmosException ex)
            {
                this.logger.Error(nameof(ScheduleDbCosmosClient.CreateRecurringDeletesScheduleDbAsync), ex, $"Document creation failed with error = {ex.Message}");
                throw new ScheduleDbClientException(nameof(ScheduleDbCosmosClient.CreateRecurringDeletesScheduleDbAsync), ex.Message, ex);
            }
        }

        private async Task<RecurrentDeleteScheduleDbDocument> UpdateRecurringDeletesScheduleDbAsync(RecurrentDeleteScheduleDbDocument documentToUpdate)
        {
            try
            {
                ItemRequestOptions requestOptions = new ItemRequestOptions { IfMatchEtag = documentToUpdate.ETag };
                
                var updateItemResponse = await this.DoWorkInstrumentedAsync(
                    nameof(ScheduleDbCosmosClient.UpdateRecurringDeletesScheduleDbAsync),
                    HttpMethod.Put, 
                    this.container.ReplaceItemAsync(documentToUpdate, documentToUpdate.DocumentId, new PartitionKey(documentToUpdate.DocumentId), requestOptions))
                    .ConfigureAwait(false);
                this.logger.Information(nameof(ScheduleDbCosmosClient.UpdateRecurringDeletesScheduleDbAsync), "Document updated successfully.");
                return updateItemResponse;
            }
            catch (CosmosException ex)
            {
                this.logger.Error(nameof(ScheduleDbCosmosClient.UpdateRecurringDeletesScheduleDbAsync), ex, $"Document updation failed with error = {ex.Message}");
                throw new ScheduleDbClientException(nameof(ScheduleDbCosmosClient.UpdateRecurringDeletesScheduleDbAsync), ex.Message, ex);
            }
        }

        /// <summary>
        /// Reads all items using the query and request options.
        /// </summary>
        /// <param name="query">The query definition for the entry to be read.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="documentId">The document id for the entry to be read.</param>
        /// <returns>A list of cosmos db records found using the query.</returns>
        private async Task<IList<RecurrentDeleteScheduleDbDocument>> ReadEntriesAsync(QueryDefinition query, CancellationToken cancellationToken, string documentId = default)
        {
            var result = new List<RecurrentDeleteScheduleDbDocument>();
            string continuationToken = null;

            var correlationId = Guid.NewGuid().ToString();
            this.logger.Information(nameof(ScheduleDbCosmosClient.ReadEntriesAsync), $"correlationId {correlationId} for query {query} with MaxItemCount 1000");

            // Limit the number of items per iteration to 1000
            var queryRequestOptions = new QueryRequestOptions
            {
                // Enable parallel query execution in the Cosmos DB. A negative value means
                // the system automatically decides the number of concurrent operations to run.
                MaxConcurrency = -1,
                MaxItemCount = 1000
            };

            if (documentId != default)
            {
                this.logger.Information(nameof(ScheduleDbCosmosClient.ReadEntriesAsync), $"documentId {documentId} for correlationId {correlationId}");
                queryRequestOptions.PartitionKey = new PartitionKey(documentId);
            }
            else 
            {
                this.logger.Information(nameof(ScheduleDbCosmosClient.ReadEntriesAsync), $"documentId default for correlationId {correlationId}");
            }

            var scheduleDbDiagnosticLoggingEnabled = await
                 this.appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PXS.ScheduleDbDiagnosticLoggingEnabled).ConfigureAwait(false);
            
            this.logger.Information(nameof(ScheduleDbCosmosClient.ReadEntriesAsync), $"Feature PXS.ScheduleDbDiagnosticLoggingEnabled is {scheduleDbDiagnosticLoggingEnabled} for correlationId {correlationId}");

            try
            {
                using (FeedIterator<RecurrentDeleteScheduleDbDocument> feedIterator = this.container.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                        query,
                        continuationToken,
                        queryRequestOptions))
                {
                    while (feedIterator.HasMoreResults)
                    {
                        var response = await feedIterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);

                        if (scheduleDbDiagnosticLoggingEnabled)
                        {
                            this.logger.Information(nameof(ScheduleDbCosmosClient.ReadEntriesAsync), $"Diagnostic string: {response.Diagnostics.ToString()} for correlationId {correlationId}");
                        }

                        continuationToken = response.ContinuationToken;
                        result.AddRange(response.AsEnumerable().ToList());
                        this.logger.Information(nameof(ScheduleDbCosmosClient.ReadEntriesAsync), $"added {response.Count} items, RequestCharge: {response.RequestCharge}, for correlationId {correlationId}");
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                this.logger.Error(nameof(ScheduleDbCosmosClient.ReadEntriesAsync), ex, $"{nameof(OperationCanceledException)} occurred with as CancellationToken.IsCancellationRequested = {ex.CancellationToken.IsCancellationRequested}");
            }

            this.logger.Information(nameof(ScheduleDbCosmosClient.ReadEntriesAsync), $"total {result.Count} items returned for correlationId {correlationId}");
            return result;
        }

        /// <summary>
        /// Reads all items using the query and request options.
        /// </summary>
        /// <param name="query">query definition.</param>
        /// <param name="continuationToken">The continuation token.</param>
        /// <param name="maxItemCount">The max number of items to return for each call.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of cosmos db records found using the query.</returns>
        private async Task<(IList<RecurrentDeleteScheduleDbDocument>, string continuationToken)> ReadEntriesByContinuationTokenAsync(QueryDefinition query, string continuationToken, int maxItemCount = 1000, CancellationToken cancellationToken = default)
        {
            var result = new List<RecurrentDeleteScheduleDbDocument>();

            var correlationId = Guid.NewGuid().ToString();
            this.logger.Information(nameof(ScheduleDbCosmosClient.ReadEntriesByContinuationTokenAsync), $"correlationId {correlationId} for query {query} with maxItemCount {maxItemCount} and continuationToken {continuationToken}");

            // Limit the number of items per iteration to 1000
            var queryRequestOptions = new QueryRequestOptions
            {
                // Enable parallel query execution in Cosmos DB. A negative value means
                // the system automatically decides the number of concurrent operations to run.
                MaxConcurrency = -1,
                MaxItemCount = maxItemCount
            };

            var scheduleDbDiagnosticLoggingEnabled = await
                 this.appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PXS.ScheduleDbDiagnosticLoggingEnabled).ConfigureAwait(false);

            this.logger.Information(nameof(ScheduleDbCosmosClient.ReadEntriesByContinuationTokenAsync), $"Feature PXS.ScheduleDbDiagnosticLoggingEnabled is {scheduleDbDiagnosticLoggingEnabled} for correlationId {correlationId}");

            using (FeedIterator<RecurrentDeleteScheduleDbDocument> feedIterator = this.container.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                query,
                continuationToken,
                queryRequestOptions))
            {
                while (feedIterator.HasMoreResults)
                {
                    var response = await feedIterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);

                    if (scheduleDbDiagnosticLoggingEnabled)
                    {
                        this.logger.Information(nameof(ScheduleDbCosmosClient.ReadEntriesByContinuationTokenAsync), $"Diagnostic string: {response.Diagnostics.ToString()} for correlationId {correlationId}");
                    }

                    result.AddRange(response.AsEnumerable().ToList());
                    this.logger.Information(nameof(ScheduleDbCosmosClient.ReadEntriesByContinuationTokenAsync), $"added {response.Count} items, RequestCharge: {response.RequestCharge} for correlationId {correlationId}");
                    if (response.Count > 0)
                    {
                        this.logger.Information(nameof(ScheduleDbCosmosClient.ReadEntriesByContinuationTokenAsync), $"paginated queries not completed yet. Old continuationToken {continuationToken} new continuationToken {response.ContinuationToken} for correlationId {correlationId}");
                        continuationToken = response.ContinuationToken;
                        break;
                    }
                    this.logger.Information(nameof(ScheduleDbCosmosClient.ReadEntriesByContinuationTokenAsync), $"paginated queries completed for continuationToken {continuationToken} and correlationId {correlationId}");
                }
            }

            var isPaginationComplete = continuationToken == null;
            this.logger.Information(nameof(ScheduleDbCosmosClient.ReadEntriesByContinuationTokenAsync), $"total {result.Count} items returned for correlationId {correlationId} isPaginationComplete = {isPaginationComplete}");
            return (result, continuationToken);
        }

        private QueryDefinition GetExpectedNextDeleteOccurrenceQueryText(DateTimeOffset expectedNextDeleteOccurrenceUtc)
        {
            var query = $"SELECT * FROM ScheduleDb sdb WHERE sdb.nextDeleteOccurrenceUtc <= @expectedNextDeleteOccurrenceUtc AND sdb.recurrentDeleteStatus = @status";

            return new QueryDefinition(query)
                .WithParameter("@expectedNextDeleteOccurrenceUtc", expectedNextDeleteOccurrenceUtc)
                .WithParameter("@status", (int)RecurrentDeleteStatus.Active);
        }

        private QueryDefinition GetPreVerifierExpirationDateQueryText(DateTimeOffset preVerifierExpirationDate)
        {
            var query = $"SELECT * FROM ScheduleDb sdb WHERE sdb.preVerifierExpirationDateUtc <= @preVerifierExpirationDate AND sdb.recurrentDeleteStatus = @status";

            return new QueryDefinition(query)
                .WithParameter("@preVerifierExpirationDate", preVerifierExpirationDate)
                .WithParameter("@status", (int)RecurrentDeleteStatus.Active);
        }

        private QueryDefinition GetPuidQueryText(long puid)
        {
            var query = $"SELECT * FROM ScheduleDb sdb WHERE sdb.puid = @puid";

            return new QueryDefinition(query)
                .WithParameter("@puid", puid);
        }

        private QueryDefinition GetPuidAndDataTypeQueryText(long puid, string dataType)
        {
            var query = $"SELECT * FROM ScheduleDb sdb WHERE sdb.puid = @puid AND sdb.dataType = @dataType";
            return new QueryDefinition(query)
                .WithParameter("@puid", puid)
                .WithParameter("@dataType", dataType); 
        }

        private void CheckRecurrentDeleteScheduleDbDocumentArgs(RecurrentDeleteScheduleDbDocument args)
        {
            if (string.IsNullOrEmpty(args.PreVerifier) && this.recurringDeleteWorkerConfig.EnablePreVerifierWorker)
                throw new ArgumentNullException(nameof(args.PreVerifier));

            if (args.PreVerifierExpirationDateUtc == null && this.recurringDeleteWorkerConfig.EnablePreVerifierWorker)
                throw new ArgumentNullException(nameof(args.PreVerifierExpirationDateUtc));

            if(args.CreateDateUtc == null)
                args.CreateDateUtc = DateTime.UtcNow;
        }

        private async Task<T> DoWorkInstrumentedAsync<T>(string operationName, HttpMethod httpMethod, Task<T> func)
        {
            var apiEvent = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                partnerId: nameof(ScheduleDbCosmosClient),
                operationName: $"{operationName} on Container {this.container.Id}",
                operationVersion: "v1",
                targetUri: this.scheduleDbConfig.CosmosDbEndPoint,
                requestMethod: httpMethod,
                dependencyType: "CosmosDb");

            apiEvent.Success = false;

            try
            {
                this.logger.Information(nameof(ScheduleDbCosmosClient.DoWorkInstrumentedAsync), $"starting {httpMethod} {operationName} call on {this.container.Id}");
                apiEvent.Start();
                var response = await func.ConfigureAwait(false);
                apiEvent.ExtraData["DoWorkStatus"] = true.ToString();
                apiEvent.Success = true;
                return response;
            }
            catch (Exception ex)
            {
                this.logger.Error(nameof(ScheduleDbCosmosClient.DoWorkInstrumentedAsync), ex, $"{httpMethod} {operationName} call failed on {this.container.Id} with exception");
                apiEvent.Success = false;
                apiEvent.ExceptionTypeName = ex.GetType().Name;
                apiEvent.ErrorMessage = ex.Message;
                apiEvent.RequestStatus = Ms.Qos.ServiceRequestStatus.ServiceError;
                apiEvent.ExtraData["DoWorkStatus"] = false.ToString();
                throw;
            }
            finally
            {
                this.logger.Information(nameof(ScheduleDbCosmosClient.DoWorkInstrumentedAsync), $"exiting {nameof(this.DoWorkInstrumentedAsync)} with status {apiEvent.Success}");
                apiEvent.Finish();
            }
        }
    }
}
