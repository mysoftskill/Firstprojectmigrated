namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer
{

    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Implementation for writing data to storage.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class PrivacyDataStorageWriter : IPrivacyDataStorageWriter
    {
        private readonly DocumentModule.DocumentContext documentContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivacyDataStorageWriter"/> class.
        /// </summary>
        /// <param name="documentClient">DocumentDB client.</param>
        /// <param name="configuration">Configuration for DocumentDB.</param>
        public PrivacyDataStorageWriter(IDocumentClient documentClient, IDocumentDatabaseConfig configuration)
        {
            this.documentContext = new DocumentModule.DocumentContext
            {
                CollectionName = configuration.EntityCollectionName,
                DatabaseName = configuration.DatabaseName,
                DocumentClient = documentClient
            };
        }

        /// <summary>
        /// Create a data owner with history item.
        /// </summary>
        /// <param name="dataOwner">The data owner to be persisted.</param>
        /// <returns>The data owner that was stored.</returns>
        public Task<DataOwner> CreateDataOwnerAsync(DataOwner dataOwner)
        {
            return this.CreateEntityAsync<DataOwner>(dataOwner);
        }

        /// <summary>
        /// Update a data owner with history item.
        /// </summary>
        /// <param name="dataOwner">The data owner to update.</param>
        /// <returns>The data owner that was stored.</returns>
        public Task<DataOwner> UpdateDataOwnerAsync(DataOwner dataOwner)
        {
            return this.UpdateEntityAsync<DataOwner>(dataOwner);
        }

        /// <summary>
        /// Create a asset group.
        /// </summary>
        /// <param name="assetGroup">The asset group to be persisted.</param>
        /// <returns>The data that was stored.</returns>
        public Task<AssetGroup> CreateAssetGroupAsync(AssetGroup assetGroup)
        {
            return this.CreateEntityAsync<AssetGroup>(assetGroup);
        }

        /// <summary>
        /// Update a asset group. Fails if the ETag does not match.
        /// </summary>
        /// <param name="assetGroup">The asset group with updated properties.</param>
        /// <returns>The data that was stored.</returns>
        public Task<AssetGroup> UpdateAssetGroupAsync(AssetGroup assetGroup)
        {
            return this.UpdateEntityAsync<AssetGroup>(assetGroup);
        }

        /// <summary>
        /// Create or replace an asset group and additionally create or replace any entities that were changed as a side effect
        /// of the write action. Returns the modified asset group.
        /// </summary>
        /// <param name="assetGroup">The asset group with updated properties.</param>
        /// <param name="writeAction">The specific write action: create/update.</param>
        /// <param name="additionalEntities">Any additional entities that need to be created or replaced.</param>
        /// <returns>The data that was stored.</returns>
        public async Task<AssetGroup> UpsertAssetGroupWithSideEffectsAsync(AssetGroup assetGroup, WriteAction writeAction, IEnumerable<Entity> additionalEntities)
        {
            var entities = await this.UpsertDocuments<AssetGroup>(
                additionalEntities.Concat(new[] { assetGroup }),
                Enumerable.Empty<Entity>())
                .ConfigureAwait(false);

            return entities.Single(x => x.Id == assetGroup.Id);
        }

        /// <summary>
        /// Create a variant definition.
        /// </summary>
        /// <param name="variantDefinition">The variant definition to be persisted.</param>
        /// <returns>The data that was stored.</returns>
        public Task<VariantDefinition> CreateVariantDefinitionAsync(VariantDefinition variantDefinition)
        {
            return this.CreateEntityAsync<VariantDefinition>(variantDefinition);
        }

        /// <summary>
        /// Update a variant definition. Fails if the ETag does not match.
        /// </summary>
        /// <param name="variantDefinition">The variant definition with updated properties.</param>
        /// <returns>The data that was stored.</returns>
        public Task<VariantDefinition> UpdateVariantDefinitionAsync(VariantDefinition variantDefinition)
        {
            return this.UpdateEntityAsync<VariantDefinition>(variantDefinition);
        }

        /// <summary>
        /// Create or replace a variant request and additionally create or replace any entities that were changed as a side effect
        /// of the write action. Returns the modified variant request.
        /// </summary>
        /// <param name="variantRequest">The variant request with updated properties.</param>
        /// <param name="additionalEntities">Any additional entities that need to be created or replaced.</param>
        /// <returns>The data that was stored.</returns>
        public async Task<VariantRequest> UpsertVariantRequestWithSideEffectsAsync(VariantRequest variantRequest, IEnumerable<Entity> additionalEntities)
        {
            var entities = await this.UpsertDocuments<VariantRequest>(
                additionalEntities.Concat(new[] { variantRequest }),
                Enumerable.Empty<Entity>())
                .ConfigureAwait(false);

            return entities.Single(x => x.Id == variantRequest.Id);
        }

        /// <summary>
        /// Create a data agent.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="dataAgent">The data agent to be persisted.</param>
        /// <returns>The data that was stored.</returns>
        public Task<TDataAgent> CreateDataAgentAsync<TDataAgent>(TDataAgent dataAgent) where TDataAgent : DataAgent
        {
            return this.CreateEntityAsync<TDataAgent>(dataAgent);
        }

        /// <summary>
        /// Update a data agent. Fails if the ETag does not match.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="dataAgent">The data agent with updated properties.</param>
        /// <returns>The data that was stored.</returns>
        public Task<TDataAgent> UpdateDataAgentAsync<TDataAgent>(TDataAgent dataAgent) where TDataAgent : DataAgent
        {
            return this.UpdateEntityAsync<TDataAgent>(dataAgent);
        }

        /// <summary>
        /// Create a variant request.
        /// </summary>
        /// <param name="variantRequest">The variant request to be persisted.</param>
        /// <returns>The variant request that was stored.</returns>
        public Task<VariantRequest> CreateVariantRequestAsync(VariantRequest variantRequest)
        {
            return this.CreateEntityAsync<VariantRequest>(variantRequest);
        }

        /// <summary>
        /// Update a variant request.
        /// </summary>
        /// <param name="variantRequest">The variant request to update.</param>
        /// <returns>The variant request that was stored.</returns>
        public Task<VariantRequest> UpdateVariantRequestAsync(VariantRequest variantRequest)
        {
            return this.UpdateEntityAsync<VariantRequest>(variantRequest);
        }

        /// <summary>
        /// Update a set of entities in bulk.
        /// </summary>
        /// <param name="entities">The set of entities to update.</param>
        /// <returns>The updated entities.</returns>
        public async Task<IEnumerable<Entity>> UpdateEntitiesAsync(IEnumerable<Entity> entities)
        {
            var x = await this.UpsertDocuments<Entity>(entities, Enumerable.Empty<Entity>()).ConfigureAwait(false);
            return x.Where(y => y is Entity); // Filter out history items.
        }

        /// <summary>
        /// Create a transfer request. 
        /// Additionally update any entities that are affected by the creation of this transfer request.
        /// </summary>
        /// <param name="transferRequest">The transfer request with updated properties.</param>
        /// <param name="additionalEntities">Any additional entities that need to be created or replaced.</param>
        /// <returns>The transfer request that was created.</returns>
        public async Task<TransferRequest> CreateTransferRequestAsync(TransferRequest transferRequest, IEnumerable<Entity> additionalEntities)
        {
            var entities = await this.UpsertDocuments<TransferRequest>(
                additionalEntities.Concat(new[] { transferRequest }),
                Enumerable.Empty<Entity>())
                .ConfigureAwait(false);

            return entities.Single(x => x.Id == transferRequest.Id);
        }

        /// <summary>
        /// Update a transfer request.
        /// </summary>
        /// <param name="transferRequest">The transfer request to be updated.</param>
        /// <returns>The transfer request that was updated.</returns>
        public Task<TransferRequest> UpdateTransferRequestAsync(TransferRequest transferRequest)
        {
            return this.UpdateEntityAsync<TransferRequest>(transferRequest);
        }

        /// <summary>
        /// Create entity calling the <c>upsert</c> method.
        /// </summary>
        /// <typeparam name="T">Any entity type.</typeparam>
        /// <param name="entity">The entity to be created.</param>
        /// <returns>The entity data that was stored.</returns>
        private async Task<T> CreateEntityAsync<T>(T entity) where T : Entity
        {
            var entities = await this.UpsertDocuments<T>(
                new T[] { entity },
                Enumerable.Empty<T>())
                .ConfigureAwait(false);

            return entities.Single(x => x.Id == entity.Id);
        }

        /// <summary>
        /// Update entity calling the <c>upsert</c> method.
        /// </summary>
        /// <typeparam name="T">Any entity type.</typeparam>
        /// <param name="entity">The entity to be updated.</param>
        /// <returns>The entity data that was stored.</returns>
        private async Task<T> UpdateEntityAsync<T>(T entity) where T : Entity
        {
            var entities = await this.UpsertDocuments<T>(
                new T[] { entity },
                Enumerable.Empty<T>())
                .ConfigureAwait(false);

            return entities.Single(x => x.Id == entity.Id);
        }

        /// <summary>
        /// <c>Upserts</c> many documents in a single transaction.
        /// </summary>
        /// <typeparam name="T">The document type.</typeparam>
        /// <param name="upsertDocuments">The documents to <c>upsert</c>.</param>
        /// <param name="deleteDocuments">The documents to hard delete.</param>
        /// <returns>The updated documents.</returns>
        private async Task<IEnumerable<T>> UpsertDocuments<T>(
            IEnumerable<Entity> upsertDocuments,
            IEnumerable<Entity> deleteDocuments) where T : Entity
        {
            var storedProcedureUri = UriFactory.CreateStoredProcedureUri(this.documentContext.DatabaseName, this.documentContext.CollectionName, StoredProcedureNames.V1.BulkUpsert);

            var historyItems = this.CreateHistoryItems(upsertDocuments, deleteDocuments);

            try
            {
                var response = await
                    this.documentContext.DocumentClient
                        .ExecuteStoredProcedureAsync<Document[]>(
                            storedProcedureUri,
                            this.documentContext.DatabaseName,
                            this.documentContext.CollectionName,
                            upsertDocuments.Cast<object>().Concat(historyItems).ToArray(),
                            deleteDocuments.ToArray())
                        .ConfigureAwait(false);

                return response.Response.Select(d => (T)(dynamic)d).Where(x => x != null);
            }
            catch (DocumentClientException ex)
            {
                // If we get a PreconditionFailed exception, assume that it is an ETag mismatch
                if (ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                    ex.Error.Message.Contains("One of the specified pre-condition is not met"))
                {
                    throw new ETagMismatchException("One of the specified pre-condition is not met", ex, ex.Message);
                }

                throw ex;
            }
        }

        /// <summary>
        /// Helper function to create history items according to different operation type.
        /// </summary>
        /// <param name="upsertDocuments">The documents to <c>upsert</c>.</param>
        /// <param name="deleteDocuments">The documents to hard delete.</param>
        /// <returns>List of corresponding history items.</returns>
        private IEnumerable<HistoryItem> CreateHistoryItems(
            IEnumerable<Entity> upsertDocuments,
            IEnumerable<Entity> deleteDocuments)
        {
            var transactionId = Guid.NewGuid();
            IList<HistoryItem> historyItems = new List<HistoryItem>();

            foreach (var document in upsertDocuments)
            {
                WriteAction action;

                if (string.IsNullOrEmpty(document.ETag))
                {
                    action = WriteAction.Create;
                }
                else if (document.IsDeleted)
                {
                    action = WriteAction.SoftDelete;
                }
                else
                {
                    action = WriteAction.Update;
                }

                historyItems.Add(new HistoryItem(document, action, transactionId));
            }

            foreach (var document in deleteDocuments)
            {
                historyItems.Add(new HistoryItem(document, WriteAction.HardDelete, transactionId));
            }

            return historyItems;
        }
    }
}
