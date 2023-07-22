namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Reader
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    using Azure.Documents.SystemFunctions;

    using DocumentDB.Models;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Kusto;

    using Microsoft.PrivacyServices.DataManagement.DataAccess.Scheduler;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    using Newtonsoft.Json;

    using AssetGroup = DataManagement.Models.V2.AssetGroup;
    using DataAgent = DataManagement.Models.V2.DataAgent;
    using DataOwner = DataManagement.Models.V2.DataOwner;
    using Entity = DataManagement.Models.V2.Entity;
    using HistoryItem = DataManagement.Models.V2.HistoryItem;
    using SharingRequest = DataManagement.Models.V2.SharingRequest;
    using TransferRequest = DataManagement.Models.V2.TransferRequest;
    using VariantDefinition = DataManagement.Models.V2.VariantDefinition;
    using VariantRequest = DataManagement.Models.V2.VariantRequest;

    /// <summary>
    /// Implementation for reading data from storage.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class PrivacyDataStorageReader : IPrivacyDataStorageReader
    {
        private const int MaxQueryResultSize = 1000;
        private static readonly Tuple<string, string>[] DataAgentSelectOverrides =
            new[]
            {
                Tuple.Create("SharingEnabled", "sharingEnabled"),
                Tuple.Create("IsThirdPartyAgent", "isThirdPartyAgent"),
                Tuple.Create("DeploymentLocation", "deploymentLocation"),
                Tuple.Create("SupportedClouds", "supportedClouds"),
                Tuple.Create("DataResidencyBoundary","dataResidencyBoundary"),
            };

        private readonly DocumentModule.DocumentContext documentContext;
        private readonly IDocumentQueryFactory documentQueryFactory;

        private readonly IKustoClient kustoClient;
        private readonly IKustoClientConfig kustoClientConfig;
        private Uri collectionUri = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivacyDataStorageReader"/> class.
        /// </summary>
        /// <param name="documentClient">DocumentDB client.</param>
        /// <param name="configuration">Configuration for DocumentDB.</param>
        /// <param name="documentQueryFactory">A factory for decorating the document query.</param>
        /// <param name="kustoClient">A client to query pending commands.</param>
        /// <param name="kustoClientConfig">Client config containing function names.</param>
        public PrivacyDataStorageReader(
            IDocumentClient documentClient,
            IDocumentDatabaseConfig configuration,
            IDocumentQueryFactory documentQueryFactory,
            IKustoClient kustoClient,
            IKustoClientConfig kustoClientConfig)
        {
            this.documentContext = new DocumentModule.DocumentContext
            {
                CollectionName = configuration.EntityCollectionName,
                DatabaseName = configuration.DatabaseName,
                DocumentClient = documentClient
            };

            this.documentQueryFactory = documentQueryFactory;
            this.kustoClient = kustoClient;
            this.kustoClientConfig = kustoClientConfig;
        }

        private Uri CollectionUri
        {
            get
            {
                if (this.collectionUri == null)
                {
                    this.collectionUri = UriFactory.CreateDocumentCollectionUri(this.documentContext.DatabaseName, this.documentContext.CollectionName);
                }

                return this.collectionUri;
            }
        }

        /// <summary>
        /// Get the data owner for a given id. Returns null if not found.
        /// </summary>
        /// <param name="dataOwnerId">Id of the data owner.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <param name="includeServiceTree">Whether or not service tree should be retrieved.</param>
        /// <returns>Data owner for the given id.</returns>
        public async Task<DataOwner> GetDataOwnerAsync(Guid dataOwnerId, bool includeTrackingDetails, bool includeServiceTree)
        {
            var value = await this.GetDocument<DataOwner>(dataOwnerId, includeTrackingDetails).ConfigureAwait(false);

            if (!includeServiceTree && value != null)
            {
                value.ServiceTree = null;
            }

            return value;
        }

        /// <summary>
        /// Get all data owners based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for data owner.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <param name="includeServiceTree">Whether or not service tree should be retrieved.</param>
        /// <returns>Data owners matching the filter criteria.</returns>
        public Task<FilterResult<DataOwner>> GetDataOwnersAsync(IFilterCriteria<DataOwner> filterCriteria, bool includeTrackingDetails, bool includeServiceTree)
        {
            return this.GetDocuments(filterCriteria, this.DynamicSelectGenerator<DataOwner>(includeTrackingDetails, includeServiceTree));
        }

        /// <summary>
        /// Get all data owners based on a list of owner ids.
        /// </summary>
        /// <param name="ownerIds">The set of owner ids to retrieve.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <param name="includeServiceTree">Whether or not service tree should be retrieved.</param>
        /// <returns>Data owners matching the given owner ids.</returns>
        public Task<IEnumerable<DataOwner>> GetDataOwnersAsync(IEnumerable<Guid> ownerIds, bool includeTrackingDetails, bool includeServiceTree)
        {
            return this.GetDocuments<DataOwner>(ownerIds, this.DynamicSelectGenerator<DataOwner>(includeTrackingDetails, includeServiceTree));
        }

        /// <summary>
        /// Get all data owners based on a list of security groups.
        /// </summary>
        /// <param name="securityGroups">The set of security groups to retrieve.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <param name="includeServiceTree">Whether or not service tree should be retrieved.</param>
        /// <returns>Data owners matching the given security groups.</returns>
        public async Task<IEnumerable<DataOwner>> GetDataOwnersBySecurityGroupsAsync(IEnumerable<Guid> securityGroups, bool includeTrackingDetails, bool includeServiceTree)
        {
            //// This has been left as a reference for what the old approach was.
            //// To use this, you also need to ensure that the securityGroups has a max count of 200.
            //// That was previously handled in the business layer.
            ////var sqlString = "SELECT VALUE root " +
            ////                "FROM root " +
            ////                "JOIN x in root.writeSecurityGroups " +
            ////                "WHERE(x IN({0}) AND root.entityType = \"DataOwner\" AND root.contractVersion = \"V2\")";

            ////var securityGroupString = string.Join(",", securityGroups.Select(x => $"\"{x.ToString()}\""));

            ////return this.documentContext.DocumentClient
            ////    .CreateDocumentQuery<DataOwner>(this.CollectionUri, string.Format(sqlString, securityGroupString))
            ////    .QueryAsync(this.documentQueryFactory);

            var sqlString = "SELECT root.id AS id, root.writeSecurityGroups as writeSecurityGroups " +
                            "FROM root " +
                            "WHERE ((NOT(IS_DEFINED(root.isDeleted)) OR root.isDeleted = false) " +
                            "AND (root.entityType = \"DataOwner\" AND root.contractVersion = \"V2\" AND root.writeSecurityGroups != null))";

            var documentIds = await
                this.documentContext.DocumentClient
                .CreateDocumentQuery<SelectObject>(this.CollectionUri, sqlString, new FeedOptions { MaxItemCount = MaxQueryResultSize })
                .QueryAsync(this.documentQueryFactory)
                .ConfigureAwait(false);

            var values =
                documentIds
                .Where(x => securityGroups.Any(sg => x.writeSecurityGroups != null && x.writeSecurityGroups.Contains(sg)))
                .Select(x => x.id);

            if (values.Any())
            {
                return await this.GetDataOwnersAsync(values, includeTrackingDetails, includeServiceTree).ConfigureAwait(false);
            }
            else
            {
                return Enumerable.Empty<DataOwner>();
            }
        }

        /// <summary>
        /// Get all data owners who have the given user alias as a service admin.
        /// </summary>
        /// <param name="userAlias">The user alias to search for.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <param name="includeServiceTree">Whether or not service tree should be retrieved.</param>
        /// <returns>Data owners matching the given user alias.</returns>
        public async Task<IEnumerable<DataOwner>> GetDataOwnersByServiceAdminAsync(string userAlias, bool includeTrackingDetails, bool includeServiceTree)
        {
            var value = await this.documentContext.DocumentClient
                .CreateDocumentQuery<SelectObject>(this.CollectionUri, new FeedOptions { MaxItemCount = MaxQueryResultSize })
                .Where(m =>
                    m.entityType == EntityType.DataOwner &&
                    m.serviceTree != null &&
                    m.serviceTree.ServiceAdmins != null &&
                    m.serviceTree.ServiceAdmins.Contains(userAlias) &&
                    (!m.isDeleted.IsDefined() || m.isDeleted == false))
                .Select(this.DynamicSelectGenerator<DataOwner>(includeTrackingDetails, includeServiceTree))
                .QueryAsync(this.documentQueryFactory)
                .ConfigureAwait(false);

            return value.Select(y => (DataOwner)y);
        }

        /// <summary>
        /// Get the data agent for a given id. Returns null if not found.
        /// </summary>
        /// <param name="dataAgentId">Id of the data agent.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Data agent for the given id.</returns>
        public Task<DataAgent> GetDataAgentAsync(Guid dataAgentId, bool includeTrackingDetails)
        {
            return this.GetDocument<DataAgent>(dataAgentId, includeTrackingDetails);
        }

        /// <summary>
        /// Get all data agents based on filter criteria.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="filterCriteria">Filter criteria for data agent.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Data agents matching the filter criteria.</returns>
        public Task<FilterResult<TDataAgent>> GetDataAgentsAsync<TDataAgent>(IFilterCriteria<TDataAgent> filterCriteria, bool includeTrackingDetails) where TDataAgent : DataAgent
        {
            return this.GetDocuments(filterCriteria, this.DynamicSelectGenerator<TDataAgent>(includeTrackingDetails, false, DataAgentSelectOverrides));
        }

        /// <summary>
        /// Get all data agents based on a list of agent ids.
        /// </summary>
        /// <typeparam name="TDataAgent">The data agent type.</typeparam>
        /// <param name="agentIds">The set of agent ids to retrieve.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Data agents matching the given agent ids.</returns>
        public Task<IEnumerable<TDataAgent>> GetDataAgentsAsync<TDataAgent>(IEnumerable<Guid> agentIds, bool includeTrackingDetails) where TDataAgent : DataAgent
        {
            return this.GetDocuments<TDataAgent>(agentIds, this.DynamicSelectGenerator<TDataAgent>(includeTrackingDetails, false, DataAgentSelectOverrides));
        }

        /// <summary>
        /// Get the asset group for a given id. Returns null if not found.
        /// </summary>
        /// <param name="assetGroupId">Id of the asset group.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Asset group for the given id.</returns>
        public Task<AssetGroup> GetAssetGroupAsync(Guid assetGroupId, bool includeTrackingDetails)
        {
            return this.GetDocument<AssetGroup>(assetGroupId, includeTrackingDetails);
        }

        /// <summary>
        /// Get all asset groups based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for asset group.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Asset groups matching the filter criteria.</returns>
        public Task<FilterResult<AssetGroup>> GetAssetGroupsAsync(IFilterCriteria<AssetGroup> filterCriteria, bool includeTrackingDetails)
        {
            return this.GetDocuments(filterCriteria, this.DynamicSelectGenerator<AssetGroup>(includeTrackingDetails));
        }

        /// <summary>
        /// Get all asset groups based on a list of asset group ids.
        /// </summary>
        /// <param name="assetGroupIds">The set of asset group ids to retrieve.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Asset groups matching the given asset group ids.</returns>
        public Task<IEnumerable<AssetGroup>> GetAssetGroupsAsync(IEnumerable<Guid> assetGroupIds, bool includeTrackingDetails)
        {
            return this.GetDocuments<AssetGroup>(assetGroupIds, this.DynamicSelectGenerator<AssetGroup>(includeTrackingDetails));
        }

        /// <summary>
        /// Get the variant definition for a given id. Returns null if not found.
        /// </summary>
        /// <param name="variantDefinitionId">Id of the variant definition.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Variant definition for the given id.</returns>
        public Task<VariantDefinition> GetVariantDefinitionAsync(Guid variantDefinitionId, bool includeTrackingDetails)
        {
            return this.GetDocument<VariantDefinition>(variantDefinitionId, includeTrackingDetails);
        }

        /// <summary>
        /// Get all variant definitions based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for variant definition.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Variant definitions matching the filter criteria.</returns>
        public Task<FilterResult<VariantDefinition>> GetVariantDefinitionsAsync(IFilterCriteria<VariantDefinition> filterCriteria, bool includeTrackingDetails)
        {
            return this.GetDocuments(filterCriteria, this.DynamicSelectGenerator<VariantDefinition>(includeTrackingDetails));
        }

        /// <summary>
        /// Get all variant definitions based on a list of variant definition ids.
        /// </summary>
        /// <param name="variantDefinitionIds">The set of variant definition ids to retrieve.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Variant definitions matching the given variant definition ids.</returns>
        public Task<IEnumerable<VariantDefinition>> GetVariantDefinitionsAsync(IEnumerable<Guid> variantDefinitionIds, bool includeTrackingDetails)
        {
            return this.GetDocuments<VariantDefinition>(variantDefinitionIds, this.DynamicSelectGenerator<VariantDefinition>(includeTrackingDetails));
        }

        /// <summary>
        /// Get all history items based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for history item.</param>
        /// <returns>History items matching the filter criteria.</returns>
        public Task<FilterResult<HistoryItem>> GetHistoryItemsAsync(IFilterCriteria<HistoryItem> filterCriteria)
        {
            return this.GetDocuments(filterCriteria, this.DynamicSelectGenerator<HistoryItem>(false));
        }

        /// <summary>
        /// Determines if there are any other entities linked to the data agent entity.
        /// </summary>
        /// <param name="dataAgentId">Id of the data agent.</param>
        /// <returns>True if the data agent entity is linked to any other entities, False otherwise.</returns>
        public async Task<bool> IsDataAgentLinkedToAnyOtherEntities(Guid dataAgentId)
        {
            var sqlString = "SELECT TOP 1 root.id AS id " +
                            "FROM root " +
                            "WHERE ((NOT(IS_DEFINED(root.isDeleted)) OR root.isDeleted = false) " +
                            "AND (root.deleteAgentId = @dataAgentId OR root.accountCloseAgentId = @dataAgentId OR root.exportAgentId = @dataAgentId))";

            var id = await
                this.documentContext.DocumentClient
                .CreateDocumentQuery<SelectObject>(
                    this.CollectionUri,
                    new SqlQuerySpec
                    {
                        QueryText = sqlString,
                        Parameters = new SqlParameterCollection()
                        {
                            new SqlParameter("@dataAgentId", dataAgentId)
                        }
                    },
                    new FeedOptions { MaxItemCount = MaxQueryResultSize })
                .QueryAsync(this.documentQueryFactory)
                .ConfigureAwait(false);

            return id.Any();
        }

        /// <summary>
        /// Determines if Data Agent has pending commands.
        /// </summary>
        /// <param name="dataAgentId">Id of the data agent.</param>
        /// <returns>True if the data agent has pending commands, False otherwise.</returns>
        public async Task<bool> DataAgentHasPendingCommands(Guid dataAgentId)
        {
            var kustoResponse = await this.kustoClient.QueryAsync($"{this.kustoClientConfig.KustoFunctionPendingCommands}('{dataAgentId}')").ConfigureAwait(false);

            var result = kustoResponse.Response;
            if (result.Rows?.Count > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if there are any other entities linked to the data owner entity.
        /// </summary>
        /// <param name="dataOwnerId">Id of the data owner.</param>
        /// <returns>True if the data owner entity is linked to any other entities, False otherwise.</returns>
        public async Task<bool> IsDataOwnerLinkedToAnyOtherEntities(Guid dataOwnerId)
        {
            var sqlString = "SELECT TOP 1 root.id AS id " +
                            "FROM root " +
                            "WHERE ((NOT(IS_DEFINED(root.isDeleted)) OR root.isDeleted = false) " +
                            "AND root.ownerId = @dataOwnerId)";

            var id = await
                this.documentContext.DocumentClient
                .CreateDocumentQuery<SelectObject>(
                    this.CollectionUri,
                    new SqlQuerySpec
                    {
                        QueryText = sqlString,
                        Parameters = new SqlParameterCollection()
                        {
                            new SqlParameter("@dataOwnerId", dataOwnerId)
                        }
                    },
                    new FeedOptions { MaxItemCount = MaxQueryResultSize })
                .QueryAsync(this.documentQueryFactory)
                .ConfigureAwait(false);

            return id.Any();
        }

        /// <summary>
        /// Get the sharing request for a given id. Returns null if not found.
        /// </summary>
        /// <param name="sharingRequestId">Id of the sharing request.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Sharing request for the given id.</returns>
        public Task<SharingRequest> GetSharingRequestAsync(Guid sharingRequestId, bool includeTrackingDetails)
        {
            return this.GetDocument<SharingRequest>(sharingRequestId, includeTrackingDetails);
        }

        /// <summary>
        /// Get all sharing requests based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for sharing request.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Sharing requests matching the filter criteria.</returns>
        public Task<FilterResult<SharingRequest>> GetSharingRequestsAsync(IFilterCriteria<SharingRequest> filterCriteria, bool includeTrackingDetails)
        {
            return this.GetDocuments(filterCriteria, this.DynamicSelectGenerator<SharingRequest>(includeTrackingDetails));
        }

        /// <summary>
        /// Get all sharing requests based on a list of sharing request ids.
        /// </summary>
        /// <param name="sharingRequestIds">The set of sharing request ids to retrieve.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Sharing requests matching the given view source ids.</returns>
        public Task<IEnumerable<SharingRequest>> GetSharingRequestsAsync(IEnumerable<Guid> sharingRequestIds, bool includeTrackingDetails)
        {
            return this.GetDocuments<SharingRequest>(sharingRequestIds, this.DynamicSelectGenerator<SharingRequest>(includeTrackingDetails));
        }

        /// <summary>
        /// Get the variant request for a given id. Returns null if not found.
        /// </summary>
        /// <param name="variantRequestId">Id of the variant request.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Variant request for the given id.</returns>
        public Task<VariantRequest> GetVariantRequestAsync(Guid variantRequestId, bool includeTrackingDetails)
        {
            return this.GetDocument<VariantRequest>(variantRequestId, includeTrackingDetails);
        }

        /// <summary>
        /// Get all variant requests based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for variant request.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Variant requests matching the filter criteria.</returns>
        public Task<FilterResult<VariantRequest>> GetVariantRequestsAsync(IFilterCriteria<VariantRequest> filterCriteria, bool includeTrackingDetails)
        {
            return this.GetDocuments(filterCriteria, this.DynamicSelectGenerator<VariantRequest>(includeTrackingDetails));
        }

        /// <summary>
        /// Get all variant requests based on a list of variant request ids.
        /// </summary>
        /// <param name="variantRequestIds">The set of variant request ids to retrieve.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Variant requests matching the given view source ids.</returns>
        public Task<IEnumerable<VariantRequest>> GetVariantRequestsAsync(IEnumerable<Guid> variantRequestIds, bool includeTrackingDetails)
        {
            return this.GetDocuments<VariantRequest>(variantRequestIds, this.DynamicSelectGenerator<VariantRequest>(includeTrackingDetails));
        }

        /// <summary>
        /// Get the transfer request for a given id. Returns null if not found.
        /// </summary>
        /// <param name="transferRequestId">Id of the transfer request.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Transfer request for the given id.</returns>
        public Task<TransferRequest> GetTransferRequestAsync(Guid transferRequestId, bool includeTrackingDetails)
        {
            return this.GetDocument<TransferRequest>(transferRequestId, includeTrackingDetails);
        }

        /// <summary>
        /// Get all transfer requests based on filter criteria.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for transfer request.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Transfer requests matching the filter criteria.</returns>
        public Task<FilterResult<TransferRequest>> GetTransferRequestsAsync(IFilterCriteria<TransferRequest> filterCriteria, bool includeTrackingDetails)
        {
            return this.GetDocuments(filterCriteria, this.DynamicSelectGenerator<TransferRequest>(includeTrackingDetails));
        }

        /// <summary>
        /// Get all transfer requests based on a list of transfer request ids.
        /// </summary>
        /// <param name="transferRequestIds">The set of transfer request ids to retrieve.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Transfer requests matching the given view source ids.</returns>
        public Task<IEnumerable<TransferRequest>> GetTransferRequestsAsync(IEnumerable<Guid> transferRequestIds, bool includeTrackingDetails)
        {
            return this.GetDocuments<TransferRequest>(transferRequestIds, this.DynamicSelectGenerator<TransferRequest>(includeTrackingDetails));
        }

        /// <inheritdoc/>
        public async Task<bool> IsVariantDefinitionLinkedToAnyOtherEntities(Guid variantId)
        {
            var sqlStrings = new string[]
            {
                // Check AssertGroups
                "SELECT TOP 1 root.variants AS Variant " +
                "FROM root " +
                "JOIN (SELECT VALUE variant FROM variant IN root.variants WHERE variant.variantId = @variantId) " +
                "WHERE (NOT(IS_DEFINED(root.isDeleted)) OR root.isDeleted = false)",

                // Check VariantRequests
                "SELECT TOP 1 root.requestedVariants AS Variant " +
                "FROM root " +
                "JOIN (SELECT VALUE variant FROM variant IN root.requestedVariants WHERE variant.variantId = @variantId) " +
                "WHERE (NOT(IS_DEFINED(root.isDeleted)) OR root.isDeleted = false)",
            };

            foreach (var sqlString in sqlStrings)
            {
                var linkedEntities = await
                    this.documentContext.DocumentClient.CreateDocumentQuery<SelectObject>(
                        this.CollectionUri,
                        new SqlQuerySpec
                        {
                            QueryText = sqlString,
                            Parameters = new SqlParameterCollection()
                            {
                                new SqlParameter("@variantId", variantId)
                            }
                        },
                        new FeedOptions { MaxItemCount = MaxQueryResultSize })
                    .QueryAsync(this.documentQueryFactory)
                    .ConfigureAwait(false);

                if (linkedEntities.Any())
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AssetGroup>> GetLinkedAssetGroups(Guid variantId)
        {
            string sqlString = "SELECT root.id AS id, root.variants AS Variant " +
                               "FROM root " +
                               "JOIN (SELECT VALUE variant FROM variant IN root.variants WHERE variant.variantId = @variantId) " +
                               "WHERE (NOT(IS_DEFINED(root.isDeleted)) OR root.isDeleted = false)";

            var assetGroupIds = await
                this.documentContext.DocumentClient.CreateDocumentQuery<SelectObject>(
                    this.CollectionUri,
                    new SqlQuerySpec
                    {
                        QueryText = sqlString,
                        Parameters = new SqlParameterCollection()
                        {
                            new SqlParameter("@variantId", variantId)
                        }
                    },
                    new FeedOptions { MaxItemCount = MaxQueryResultSize })
                .QueryAsync(this.documentQueryFactory)
                .ConfigureAwait(false);

            return this.GetAssetGroupsAsync(assetGroupIds.Select(x => x.id), true).Result;
        }

        /// <summary>
        /// Get document from PDMS collection. If document does not exist then null will be returned.
        /// </summary>
        /// <typeparam name="T">Document type.</typeparam>
        /// <param name="id">Document Id.</param>
        /// <param name="includeTrackingDetails">Whether or not tracking details should be retrieved.</param>
        /// <returns>Document for given id.</returns>
        private async Task<T> GetDocument<T>(Guid id, bool includeTrackingDetails) where T : Entity
        {
            Func<T, T> validateDocument = document =>
            {
                EntityType requiredEntityType = (EntityType)Enum.Parse(typeof(EntityType), typeof(T).Name);
                if (document == null || requiredEntityType != document.EntityType)
                {
                    return null;
                }
                else if (document.IsDeleted)
                {
                    return null;
                }
                else
                {
                    return document;
                }
            };

            var value = await DocumentModule.Read(id, this.documentContext, validateDocument).ConfigureAwait(false);

            // Point queries cannot minimize the returned data.
            // However, we simulate it instead so that this method honors the expected behavior.
            if (!includeTrackingDetails && value != null)
            {
                value.TrackingDetails = null;
            }

            return value;
        }

        /// <summary>
        /// Get documents from PDMS collection based on filter criteria.
        /// </summary>
        /// <typeparam name="T">Document type.</typeparam>
        /// <param name="filterCriteria">Entity filter criteria.</param>
        /// <param name="select">The selection expression.</param>
        /// <returns>Documents matching filter criteria.</returns>
        [ExcludeFromCodeCoverage]
        private async Task<FilterResult<T>> GetDocuments<T>(IFilterCriteria<T> filterCriteria, Expression<Func<SelectObject, dynamic>> select)
            where T : DocumentBase<Guid>
        {
            // Document DB does not support the Skip behavior.
            // As such, we simulate this by first querying all IDs,
            // manually calculate the set of IDs for the current page,
            // and then query docDB again to retrieve the full data for the specific page.
            var documentIds =
                await this.documentContext.DocumentClient.CreateDocumentQuery<T>(this.CollectionUri, new FeedOptions { MaxItemCount = MaxQueryResultSize })
                .Where(filterCriteria.BuildExpression())
                .Select(m => m.Id)
                .QueryAsync(this.documentQueryFactory).ConfigureAwait(false);

            var index = filterCriteria.Index.Value;
            var count = filterCriteria.Count.Value;

            var pagedDocumentIds = documentIds.Skip(index).Take(count);

            var results = Enumerable.Empty<T>();

            if (pagedDocumentIds.Any())
            {
                results = await this.GetDocuments<T>(pagedDocumentIds, select).ConfigureAwait(false);
            }

            return new FilterResult<T>
            {
                Values = results,
                Index = index,
                Count = count,
                Total = documentIds.Count()
            };
        }

        /// <summary>
        /// Get documents from PDMS collection based on a set of ids.
        /// </summary>
        /// <typeparam name="T">Document type.</typeparam>
        /// <param name="documentIds">The set of ids.</param>
        /// <param name="select">The selection expression.</param>
        /// <returns>Documents matching the set of ids.</returns>
        [ExcludeFromCodeCoverage]
        private async Task<IEnumerable<T>> GetDocuments<T>(IEnumerable<Guid> documentIds, Expression<Func<SelectObject, dynamic>> select)
            where T : DocumentBase<Guid>
        {
            var x = this.documentContext.DocumentClient
                .CreateDocumentQuery<SelectObject>(this.CollectionUri, new FeedOptions { MaxItemCount = MaxQueryResultSize })
                .Where(m => documentIds.Contains(m.id))
                .Select(select);

            var value = await x.QueryAsync(this.documentQueryFactory).ConfigureAwait(false);

            return value.Select(y => (T)y).ToList(); // Force evaluation so that data is consistent up the stack.
        }

        private Expression<Func<SelectObject, dynamic>> DynamicSelectGenerator<T>(bool includeTrackingDetails, bool includeServiceTree = false, IEnumerable<Tuple<string, string>> explicitProperties = null)
        {
            explicitProperties = explicitProperties ?? Enumerable.Empty<Tuple<string, string>>();

            var destinationProperties = typeof(SelectObject).GetProperties();

            // get Properties of the T
            var properties = typeof(T).GetProperties();
            var entityFields =
                properties
                .Select(propertyInfo =>
                {
                    var source = propertyInfo.Name.Trim();

                    var destination =
                        propertyInfo.GetCustomAttributes(true)
                        .Select(x => x as JsonPropertyAttribute)
                        .Where(x => x != null)
                        .Select(x => x.PropertyName)
                        .FirstOrDefault();

                    return Tuple.Create(source, destination);
                })
                .Where(y => destinationProperties.Any(z => z.Name == y.Item2))
                .Where(x => x.Item2 != null)
                .Where(x => includeTrackingDetails || x.Item1 != nameof(Entity.TrackingDetails))
                .Where(x => includeServiceTree || x.Item1 != nameof(DataOwner.ServiceTree))
                .Concat(explicitProperties)
                .ToList();

            // Must de-dup the property names.
            int i = 0;
            while (i < entityFields.Count)
            {
                if (entityFields.Count(y => y.Item2 == entityFields[i].Item2) == 1)
                {
                    i++;
                }
                else
                {
                    entityFields.RemoveAt(i);
                }
            }

            // input parameter "o"
            var xParameter = Expression.Parameter(typeof(SelectObject), "o");

            // new statement "new Data()"
            var xNew = Expression.New(typeof(SelectObject));

            // create initializers
            var bindings =
                entityFields.Select(o =>
                {
                    // property "Field1"
                    var mi = typeof(SelectObject).GetProperty(o.Item2);

                    // original value "o.Field1"
                    var xOriginal = Expression.Property(xParameter, mi);

                    // set value "Field1 = o.Field1"
                    return Expression.Bind(mi, xOriginal);
                });

            // initialization "new Data { Field1 = o.Field1, Field2 = o.Field2 }"
            var xInit = Expression.MemberInit(xNew, bindings);

            // expression "o => new Data { Field1 = o.Field1, Field2 = o.Field2 }"
            var lambda = Expression.Lambda<Func<SelectObject, dynamic>>(xInit, xParameter);

            // compile to Func<Data, Data>
            return lambda;
        }
    }
}