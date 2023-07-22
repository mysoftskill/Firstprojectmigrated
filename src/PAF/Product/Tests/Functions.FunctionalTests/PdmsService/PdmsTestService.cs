namespace Microsoft.PrivacyServices.AzureFunctions.FunctionalTests.PdmsService
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Client.AAD;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public class PdmsTestService
    {
        private const string ComponentName = nameof(PdmsTestService);

        private static readonly string AssetGroupPathPrefix = "/paftest";

        private readonly string clientId;
        private readonly X509Certificate2 cert;
        private readonly string baseUrl;
        private readonly ILogger logger;

        private readonly string additionalInformation = "Test Additional Information";

        /// <summary>
        /// Initializes a new instance of the <see cref="PdmsTestService"/> class.
        /// </summary>
        /// <param name="clientId">Client id to authenticate.</param>
        /// <param name="cert">Client certificate to access PDMS.</param>
        /// <param name="baseUrl">Base URL for PDMS.</param>
        /// <param name="logger">The logger.</param>
        public PdmsTestService(string clientId, X509Certificate2 cert, string baseUrl, ILogger logger)
            : base()
        {
            this.clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            this.cert = cert ?? throw new ArgumentNullException(nameof(cert));
            this.baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            BaseHttpServiceProxy httpServiceProxy = new HttpServiceProxy(
                new Uri(this.baseUrl),
                defaultTimeout: TimeSpan.FromSeconds(100));
            this.PdmsClient = new DataManagementClient(httpServiceProxy);

            var authProvider = new ServiceAzureActiveDirectoryProviderFactory(this.clientId, cert, targetProductionEnvironment: false, sendX5c: true);
            var authenticationProvider = authProvider.CreateForClient();

            this.RequestContext = new RequestContext
            {
                AuthenticationProvider = authenticationProvider
            };
        }

        private RequestContext RequestContext { get; }

        private IDataManagementClient PdmsClient { get; }

        /// <summary>
        /// Create a new variant request.
        /// </summary>
        /// <returns>The new variant request.</returns>
        public async Task<VariantRequest> CreateNewVariantRequestAsync()
        {
            // Create an AssetGroup and link it to the VariantDefinition
            var assetGroup = await this.CreateNewAssetGroupAsync().ConfigureAwait(false);

            var variantDefinition = await this.CreateNewVariantDefinitionAsync(assetGroup.OwnerId).ConfigureAwait(false);

            var requestedVariants = new List<AssetGroupVariant>
            {
                new AssetGroupVariant { VariantId = variantDefinition.Id },
            };
            var variantRelationships = new List<VariantRelationship>
            {
                new VariantRelationship
                {
                    AssetGroupId = assetGroup.Id,
                    AssetQualifier = assetGroup.Qualifier
                }
            };

            var ownerId = assetGroup.OwnerId;
            VariantRequest variantRequest = new VariantRequest
            {
                OwnerId = ownerId,
                OwnerName = "Owner" + ownerId,
                CelaContactAlias = "CelaContactAlias" + ownerId,
                GeneralContractorAlias = "GeneralContractorAlias" + ownerId,
                RequestedVariants = requestedVariants,
                RequesterAlias = "requesteralias",
                VariantRelationships = variantRelationships,
                AdditionalInformation = this.additionalInformation
            };

            IHttpResult<VariantRequest> response = null;
            try
            {
                response = await this.PdmsClient.VariantRequests.CreateAsync(variantRequest, this.RequestContext)
                .ConfigureAwait(false);
                Assert.IsTrue(response.HttpStatusCode == HttpStatusCode.Created);
            }
            catch (Exception)
            {
                // If request failed, clean up the resources we created
                _ = this.DeleteVariantDefinitionAsync(variantDefinition.Id);
                _ = this.DeleteAssetGroupAsync(assetGroup.Id);
            }

            return response.Response;
        }

        /// <summary>
        /// Delete the specified variant request.
        /// </summary>
        /// <param name="variantRequest">The variant request to delete.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task CleanupVariantRequestAsync(VariantRequest variantRequest)
        {
            try
            {
                // Must get the latest ETag for the request because it may have
                // been updated by the work item creator function
                var variantRequestResponse = await this.PdmsClient.VariantRequests
                    .ReadAsync(variantRequest.Id, this.RequestContext).ConfigureAwait(false);

                // If the variant request hasn't already been deleted, remove it
                if (variantRequestResponse.HttpStatusCode == HttpStatusCode.OK)
                {
                    var variantDeleteResponse = await this.PdmsClient.VariantRequests
                        .DeleteAsync(variantRequest.Id, variantRequestResponse.Response.ETag, this.RequestContext).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                // We don't care about the result of the delete, but really shouldn't be getting an error..
                this.logger.Warning(ComponentName, ex, $"Unable to delete Variant Request Id: {variantRequest.Id}");
            }
            finally
            {
                // Clean up the variant definitions that were created
                foreach (var variant in variantRequest.RequestedVariants)
                {
                    await this.DeleteVariantDefinitionAsync(variant.VariantId).ConfigureAwait(false);
                }

                // Clean up the asset groups that were created
                foreach (var assetGroup in variantRequest.VariantRelationships)
                {
                    await this.DeleteAssetGroupAsync(assetGroup.AssetGroupId).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Check if variantRequest with Id exists in pdms.
        /// </summary>
        /// <param name="variantRequestId">variant Id</param>
        /// <returns>True if Variant exists false otherwise.</returns>
        public async Task<bool> DoesVariantRequestExistAsync(string variantRequestId)
        {
            try
            {
                IHttpResult<VariantRequest> requestResponse = await this.PdmsClient.VariantRequests
                    .ReadAsync(variantRequestId, this.RequestContext).ConfigureAwait(false);

                return requestResponse.Response?.Id == variantRequestId;
            }
            catch (Exception e)
            {
                if (e.Message.Contains("not found"))
                {
                    return false;
                }

                throw e;
            }
        }

        /// <summary>
        /// Verify that a request was deleted.
        /// </summary>
        /// <param name="variantRequestId">The id of the variant request that should have been deleted.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task VerifyVariantRequestDoesNotExistAsync(string variantRequestId)
        {
            IHttpResult<VariantRequest> requestResponse = null;
            try
            {
                requestResponse = await this.PdmsClient.VariantRequests
                    .ReadAsync(variantRequestId, this.RequestContext).ConfigureAwait(false);
            }
            catch (NotFoundError)
            {
                // expected
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("NotFound"), $"Exception was {ex.Message}");
            }

            Assert.IsNull(requestResponse, $"Unexpected response: {requestResponse?.HttpStatusCode}");
        }

        /// <summary>
        /// Create a new data owner without a Service Tree entry.
        /// </summary>
        /// <returns>The new data owner.</returns>
        protected async Task<DataOwner> CreateDataOwnerWithoutServiceTreeAsync()
        {
            var dataOwner = new DataOwner()
            {
                Name = Guid.NewGuid().ToString(),
                Description = "FunctionalTests Created DataOwner",
                WriteSecurityGroups = new List<string>()
                {
                    Guid.NewGuid().ToString()
                },
                AlertContacts = new List<string>()
                {
                    "ngpcieng@microsoft.com"
                }
            };

            var agResponse = await this.PdmsClient
                .DataOwners.CreateAsync(dataOwner, this.RequestContext)
                .ConfigureAwait(false);

            Assert.IsTrue(agResponse.HttpStatusCode == HttpStatusCode.Created);

            return agResponse.Response;
        }

        /// <summary>
        /// Create a new asset group.
        /// </summary>
        /// <param name="memberName">Name of calling method</param>
        /// <returns>The new asset group.</returns>
        protected async Task<AssetGroup> CreateNewAssetGroupAsync([CallerMemberName] string memberName = "")
        {
            // Use the name of the caller as part of the stream name so we know which test created the asset group.
            // This will be useful in figuring out which tests are not cleaning up.
            string stream = memberName + '/' + Guid.NewGuid().ToString();

            var owner = await this.CreateDataOwnerWithoutServiceTreeAsync().ConfigureAwait(false);

            var assetQualifier = AssetQualifier.CreateForCosmosStructuredStream("cosmos17", "cosmosadmin", $"{AssetGroupPathPrefix}/{stream}");
            var assetGroup = new AssetGroup
            {
                Qualifier = assetQualifier,
                OwnerId = owner.Id
            };

            var client = this.PdmsClient;
            var agResponse = await client.AssetGroups.CreateAsync(assetGroup, this.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(agResponse.HttpStatusCode == HttpStatusCode.Created);

            return agResponse.Response;
        }

        /// <summary>
        /// Create a new variant definition with a specific owner id.
        /// </summary>
        /// <param name="ownerId">The owner of the variant definition.</param>
        /// <returns>The new variant definition.</returns>
        protected async Task<VariantDefinition> CreateNewVariantDefinitionAsync(
            string ownerId)
        {
            string egrcId = Guid.NewGuid().ToString();
            string egrcName = "EgrcName - " + egrcId;
            IEnumerable<DataTypeId> dataTypes = new List<DataTypeId>() { Policies.Current.DataTypes.Ids.Account };
            IEnumerable<SubjectTypeId> subjectTypes = new List<SubjectTypeId>() { Policies.Current.SubjectTypes.Ids.DemographicUser };
            IEnumerable<CapabilityId> capabilities = new List<CapabilityId>() { Policies.Current.Capabilities.Ids.Delete };

            var variant = new VariantDefinition
            {
                Name = "VariantDefinition" + Guid.NewGuid().ToString(),
                Description = "FunctionalTests Created Variant",
                OwnerId = ownerId,
                EgrcId = egrcId,
                EgrcName = egrcName,
                DataTypes = dataTypes,
                Capabilities = capabilities,
                SubjectTypes = subjectTypes
            };
            var client = this.PdmsClient;
            var response = await client.VariantDefinitions.CreateAsync(variant, this.RequestContext)
                .ConfigureAwait(false);
            Assert.IsTrue(response.HttpStatusCode == HttpStatusCode.Created);
            return response.Response;
        }

        /// <summary>
        /// Delete the specified asset group. Unlink variant requests and transfer requests first, if needed
        /// </summary>
        /// <param name="assetGroupId">The Id of the asset group to delete.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>.
        protected async Task DeleteAssetGroupAsync(string assetGroupId)
        {
            var client = this.PdmsClient;

            try
            {
                var agResponse = await client.AssetGroups.ReadAsync(assetGroupId, this.RequestContext).ConfigureAwait(false);
                var assetGroup = agResponse.Response;

                bool agUpdated = false;
                if (assetGroup.HasPendingTransferRequest)
                {
                    // Remove transfer request
                    var txfRequestFilter = new TransferRequestFilterCriteria()
                    {
                        SourceOwnerId = assetGroup.OwnerId,
                        TargetOwnerId = assetGroup.PendingTransferRequestTargetOwnerId,
                        Count = 1,
                        Index = 0
                    };
                    var txfResponse = await client.TransferRequests.ReadByFiltersAsync(this.RequestContext, TransferRequestExpandOptions.None, txfRequestFilter).ConfigureAwait(false);
                    var txfRequest = txfResponse.Response.Value.FirstOrDefault();
                    if (txfRequest != null)
                    {
                        await client.TransferRequests.DeleteAsync(txfRequest.Id, txfRequest.ETag, this.RequestContext).ConfigureAwait(false);
                    }

                    agUpdated = true;
                }

                if (assetGroup.HasPendingVariantRequests)
                {
                    // Remove variant requests
                    var variantRequestFilter = new VariantRequestFilterCriteria()
                    {
                        OwnerId = assetGroup.OwnerId,
                        Count = 1,
                        Index = 0
                    };
                    var variantRequestResponse = await client.VariantRequests.ReadByFiltersAsync(this.RequestContext, VariantRequestExpandOptions.None, variantRequestFilter).ConfigureAwait(false);
                    var variantRequests = variantRequestResponse.Response.Value;

                    foreach (var variantRequest in variantRequests)
                    {
                        await client.VariantRequests.DeleteAsync(variantRequest.Id, variantRequest.ETag, this.RequestContext).ConfigureAwait(false);
                    }

                    agUpdated = true;
                }

                if (assetGroup.Owner != null)
                {
                    await this.CleanupDataOwnerAsync(assetGroup.Owner).ConfigureAwait(false);
                }

                // Get asset group new ETag if asset group was updated
                if (agUpdated)
                {
                    agResponse = await client.AssetGroups.ReadAsync(assetGroupId, this.RequestContext).ConfigureAwait(false);
                    assetGroup = agResponse.Response;
                }

                await this.PdmsClient.AssetGroups.DeleteAsync(assetGroup.Id, assetGroup.ETag, this.RequestContext).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // We don't care about the result of the delete, but really shouldn't be getting an error..
                this.logger.Warning(ComponentName, ex, $"Unable to delete AssetGroup Id: {assetGroupId}");
            }
        }

        /// <summary>
        /// Delete a variant definition.
        /// </summary>
        /// <param name="variantId">The Id of the variant request to delete.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>.
        protected async Task DeleteVariantDefinitionAsync(string variantId)
        {
            try
            {
                var variantResponse = await this.PdmsClient.VariantDefinitions
                    .ReadAsync(variantId, this.RequestContext).ConfigureAwait(false);

                VariantDefinition variant = variantResponse.Response;

                // We must close the VariantDefinition before we can delete it
                variant.State = VariantDefinitionState.Closed;
                variant.Reason = VariantDefinitionReason.Intentional;

                var variantUpdateResponse = await this.PdmsClient.VariantDefinitions
                    .UpdateAsync(variant, this.RequestContext).ConfigureAwait(false);

                // Delete the definition
                var variantDeleteResponse = await this.PdmsClient.VariantDefinitions
                    .DeleteAsync(variantUpdateResponse.Response.Id, variantUpdateResponse.Response.ETag, this.RequestContext, force: true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // We don't care about the result of the delete, but really shouldn't be getting an error..
                this.logger.Warning(ComponentName, ex, $"Unable to delete Variant Definition Id: {variantId}");
            }
        }

        // Remove newly created data owner.
        protected async Task CleanupDataOwnerAsync(DataOwner owner)
        {
            try
            {
                await this.PdmsClient.DataOwners
                    .DeleteAsync(owner.Id, owner.ETag, this.RequestContext).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // We don't care about the result of the delete, but really shouldn't be getting an error..
                this.logger.Warning(ComponentName, ex, $"Unable to delete Owner Id: {owner.Id}");
            }
        }
    }
}
