namespace Microsoft.PrivacyServices.DataManagement.FunctionalTests.FrontdoorTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;

    using Microsoft.Azure.Cosmos;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;
    using Microsoft.PrivacyServices.DataManagement.FunctionalTests.Setup;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;

    public class TestBase
    {
        public static string AssetGroupPathPrefix = "/pdmstest";

        protected static async Task<string> GetApiCallResponseAsStringAsync(string uriPath, HttpStatusCode statusCode = HttpStatusCode.OK, HttpMethod method = default, string payload = null, string etag = null)
        {
            Uri uri = new Uri(TestSetup.PdmsBaseUri, uriPath);
            var request = method == default ? new HttpRequestMessage(HttpMethod.Get, uri) : new HttpRequestMessage(method, uri);

            if ((method == HttpMethod.Post || method == HttpMethod.Put) && !string.IsNullOrWhiteSpace(payload))
            {
                request.Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            }

            if (method == HttpMethod.Delete && !string.IsNullOrWhiteSpace(etag))
            {
                request.Headers.Add("If-Match", etag);
            }

            request.Headers.Authorization = await TestSetup.AuthenticationProvider.AcquireTokenAsync(CancellationToken.None).ConfigureAwait(false);

            var response = await new HttpClient().SendAsync(request, CancellationToken.None).ConfigureAwait(false);  // lgtm [cs/httpclient-checkcertrevlist-disabled]

            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.IsTrue(response.StatusCode == statusCode,
                $"StatusCode was {response.StatusCode}-{response.ReasonPhrase?.ToString()}-{responseBody}");

            return responseBody;
        }

        protected static async Task<string> GetADataOwnerIdAsync()
        {
            var content = await GetApiCallResponseAsStringAsync("api/v2/DataOwners");
            Assert.IsNotNull(content);

            var dataOwners = JsonConvert.DeserializeObject<ODataResponse<List<DataOwner>>>(content);
            Assert.IsTrue(dataOwners.Value.Count > 0);
            return dataOwners.Value[0].Id;
        }

        protected static async Task<string> GetADataOwnerIdAsync(string ownerId)
        {
            var response = await TestSetup.PdmsClientInstance.DataOwners.ReadAsync(
                    ownerId,
                    TestSetup.RequestContext,
                    DataOwnerExpandOptions.None)
                .ConfigureAwait(false);
            return response?.Response?.Id;
        }

        protected static async Task<string> GetAnAgentIdAsync()
        {
            IDataManagementClient client = TestSetup.PdmsClientInstance;
            IHttpResult<Collection<DataAgent>> agentsResponse = await client.DataAgents.ReadByFiltersAsync<DataAgent>(
                    TestSetup.RequestContext,
                    DataAgentExpandOptions.None)
                .ConfigureAwait(false);
            return agentsResponse?.Response?.Value?.First()?.Id;
        }

        protected static async Task<DataOwner> CreateDataOwnerWithoutServiceTreeAsync()
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

            var agResponse = await TestSetup.PdmsClientInstance
                .DataOwners.CreateAsync(dataOwner, TestSetup.RequestContext)
                .ConfigureAwait(false);

            Assert.IsTrue(agResponse.HttpStatusCode == HttpStatusCode.Created);

            return agResponse.Response;
        }

        protected static async Task<DataAgent> CreateNewDataAgentAsync(string ownerId = null)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                ownerId = await GetADataOwnerIdAsync().ConfigureAwait(false);
            }
            var dataAgent = new DeleteAgent
            {
                Name = Guid.NewGuid().ToString(),
                Description = "FunctionalTests Created Agent",
                ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
                {
                    {
                        ReleaseState.PreProd,
                        new ConnectionDetail()
                        {
                            AadAppId = Guid.NewGuid(),
                            AuthenticationType = AuthenticationType.AadAppBasedAuth,
                            Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                            ReleaseState = ReleaseState.PreProd,
                            AgentReadiness = AgentReadiness.ProdReady
                        }
                    }
                },
                OwnerId = ownerId,
                DeploymentLocation = Policies.Current.CloudInstances.Ids.Public,
                DataResidencyBoundary = Policies.Current.DataResidencyInstances.Ids.EU,
                SupportedClouds = new List<CloudInstanceId> { Policies.Current.CloudInstances.Ids.All }
            };
            var client = TestSetup.PdmsClientInstance;
            var agResponse = await client.DataAgents.CreateAsync(dataAgent, TestSetup.RequestContext)
                .ConfigureAwait(false);
            Assert.IsTrue(agResponse.HttpStatusCode == HttpStatusCode.Created);
            return agResponse.Response;
        }

        protected static async Task<DataAgent> CreateNewDualAuthDataAgentAsync(string ownerId = null)
        {

            if (string.IsNullOrWhiteSpace(ownerId))
            {
                ownerId = await GetADataOwnerIdAsync().ConfigureAwait(false);
            }
            var dataAgent = new DeleteAgent
            {
                Name = Guid.NewGuid().ToString(),
                Description = "FunctionalTests Created Agent",
                ConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
                {
                    {
                        ReleaseState.PreProd,
                        new ConnectionDetail()
                        {
                            MsaSiteId = 297497,
                            AadAppIds = Enumerable.Empty<Guid>().Append(Guid.NewGuid()).Append(Guid.NewGuid()),
                            AuthenticationType = AuthenticationType.AadAppBasedAuth,
                            Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                            ReleaseState = ReleaseState.PreProd,
                            AgentReadiness = AgentReadiness.ProdReady
                        }
                    }
                },
                OwnerId = ownerId,
                DeploymentLocation = Policies.Current.CloudInstances.Ids.Public,
                DataResidencyBoundary = Policies.Current.DataResidencyInstances.Ids.EU,
                SupportedClouds = new List<CloudInstanceId> { Policies.Current.CloudInstances.Ids.All }
            };
            var client = TestSetup.PdmsClientInstance;
            var agentResponse = await client.DataAgents.CreateAsync(dataAgent, TestSetup.RequestContext)
                .ConfigureAwait(false);
            Assert.IsTrue(agentResponse.HttpStatusCode == HttpStatusCode.Created);

            agentResponse.Response.ConnectionDetails.TryGetValue(ReleaseState.PreProd, out ConnectionDetail responseConnection);
            Assert.IsTrue(responseConnection.AadAppIds.Count() == 2);

            return agentResponse.Response;
        }

        protected static async Task<AssetGroup> CreateNewAssetGroupAsync(string ownerId = null, [CallerMemberName] string memberName = "")
        {
            // Use the name of the caller as part of the stream name so we know which test created the asset group.
            // This will be useful in figuring out which tests are not cleaning up.
            string stream = memberName + '/' + Guid.NewGuid().ToString();

            if (string.IsNullOrEmpty(ownerId))
            {
                var owner = await CreateDataOwnerWithoutServiceTreeAsync().ConfigureAwait(false);
                ownerId = owner.Id;
            }

            var assetQualifier = AssetQualifier.CreateForCosmosStructuredStream("cosmos17", "cosmosadmin", $"{AssetGroupPathPrefix}/{stream}");
            var assetGroup = new AssetGroup
            {
                Qualifier = assetQualifier,
                OwnerId = ownerId
            };

            var client = TestSetup.PdmsClientInstance;
            var agResponse = await client.AssetGroups.CreateAsync(assetGroup, TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(agResponse.HttpStatusCode == HttpStatusCode.Created);

            return agResponse.Response;
        }

        protected static async Task<VariantDefinition> CreateNewVariantDefinitionAsync(
            string ownerId = null)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                ownerId = await GetADataOwnerIdAsync().ConfigureAwait(false);
            }

            string egrcId = Guid.NewGuid().ToString();
            string egrcName = "EgrcName - " + egrcId;
            var variant = new VariantDefinition
            {
                Name = "VariantDefinition" + Guid.NewGuid().ToString(),
                Description = "FunctionalTests Created Variant",
                OwnerId = ownerId,
                EgrcId = egrcId,
                EgrcName = egrcName
            };
            var client = TestSetup.PdmsClientInstance;
            var response = await client.VariantDefinitions.CreateAsync(variant, TestSetup.RequestContext)
                .ConfigureAwait(false);
            Assert.IsTrue(response.HttpStatusCode == HttpStatusCode.Created);
            return response.Response;
        }

        protected static async Task<VariantRequest> CreateNewVariantRequestAsync(
            string requesterId = null,
            string variantDefinitionId = null,
            bool createMultipleAssetGroups = false,
            bool createMultipleVariants = false)
        {
            // Create an AssetGroup and link it to the VariantDefinition
            var assetGroup = await CreateNewAssetGroupAsync().ConfigureAwait(false);

            var ownerId = requesterId ?? assetGroup.OwnerId;

            // Get the specified variant or create a new one
            var client = TestSetup.PdmsClientInstance;
            VariantDefinition variantDefinition;
            if (string.IsNullOrWhiteSpace(variantDefinitionId))
            {
                variantDefinition = await CreateNewVariantDefinitionAsync(ownerId).ConfigureAwait(false);
            }
            else
            {
                var variantDefinitionResponse = await client.VariantDefinitions
                                                            .ReadAsync(variantDefinitionId, TestSetup.RequestContext)
                                                            .ConfigureAwait(false);
                variantDefinition = variantDefinitionResponse.Response;
            }

            var requestedVariants = new List<AssetGroupVariant>
            {
                new AssetGroupVariant { VariantId = variantDefinition.Id},
            };

            if (createMultipleVariants)
            {
                var variantDefinition2 = await CreateNewVariantDefinitionAsync(ownerId).ConfigureAwait(false);

                requestedVariants.Add(new AssetGroupVariant { VariantId = variantDefinition2.Id });
            }

            var variantRelationships = new List<VariantRelationship>
            {
                new VariantRelationship
                {
                    AssetGroupId = assetGroup.Id,
                    AssetQualifier = assetGroup.Qualifier
                }
            };

            if (createMultipleAssetGroups)
            {
                var assetGroup2 = await CreateNewAssetGroupAsync(ownerId).ConfigureAwait(false);
                variantRelationships.Add(new VariantRelationship
                {
                    AssetGroupId = assetGroup2.Id,
                    AssetQualifier = assetGroup2.Qualifier
                });
            }

            VariantRequest variantRequest = new VariantRequest
            {
                OwnerId = ownerId,
                OwnerName = "Owner" + ownerId,
                CelaContactAlias = "CelaContactAlias" + ownerId,
                GeneralContractorAlias = "GeneralContractorAlias" + ownerId,
                RequestedVariants = requestedVariants,
                VariantRelationships = variantRelationships,
                AdditionalInformation = "Test Additional Information"
            };

            var response = await client.VariantRequests.CreateAsync(variantRequest, TestSetup.RequestContext)
                .ConfigureAwait(false);
            Assert.IsTrue(response.HttpStatusCode == HttpStatusCode.Created);
            return response.Response;
        }

        protected static async Task<TransferRequest> CreateNewTransferRequestAsync(string dataOwnerId = null)
        {
            if (dataOwnerId == null)
            {
                var dataOwner = await CreateDataOwnerWithoutServiceTreeAsync().ConfigureAwait(false);
                dataOwnerId = dataOwner.Id;
            }

            // Transfer a new asset group to a data owner
            var assetGroup = await CreateNewAssetGroupAsync().ConfigureAwait(false);
            var transferRequest = new TransferRequest
            {
                AssetGroups = new List<string>() { assetGroup.Id.ToString() },
                SourceOwnerId = assetGroup.OwnerId,
                TargetOwnerId = dataOwnerId
            };

            var client = TestSetup.PdmsClientInstance;
            var response = await client.TransferRequests.CreateAsync(transferRequest, TestSetup.RequestContext)
                .ConfigureAwait(false);
            Assert.IsTrue(response.HttpStatusCode == HttpStatusCode.Created);
            return response.Response;
        }

        // Helper method to remove the specified asset group; will unlink variant requests and transfer requests first, if needed
        protected static async Task RemoveAssetGroupAsync(string assetGroupId)
        {
            var client = TestSetup.PdmsClientInstance;

            try
            {
                var agResponse = await client.AssetGroups.ReadAsync(assetGroupId, TestSetup.RequestContext).ConfigureAwait(false);
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
                    var txfResponse = await client.TransferRequests.ReadByFiltersAsync(TestSetup.RequestContext, TransferRequestExpandOptions.None, txfRequestFilter).ConfigureAwait(false);
                    var txfRequest = txfResponse.Response.Value.FirstOrDefault();
                    if (txfRequest != null)
                    {
                        await client.TransferRequests.DeleteAsync(txfRequest.Id, txfRequest.ETag, TestSetup.RequestContext).ConfigureAwait(false);
                    }

                    agUpdated = true;
                }

                if (assetGroup.HasPendingVariantRequests)
                {
                    // Remove variant requests
                    var variantRequestFilter = new VariantRequestFilterCriteria()
                    {
                        OwnerId = assetGroup.OwnerId,
                        Count = 10,
                        Index = 0
                    };
                    var variantRequestResponse = await client.VariantRequests.ReadByFiltersAsync(TestSetup.RequestContext, VariantRequestExpandOptions.None, variantRequestFilter).ConfigureAwait(false);
                    var variantRequests = variantRequestResponse.Response.Value;

                    foreach (var variantRequest in variantRequests)
                    {
                        await client.VariantRequests.DeleteAsync(variantRequest.Id, variantRequest.ETag, TestSetup.RequestContext).ConfigureAwait(false);
                    }

                    agUpdated = true;
                }

                // Get new ETag if asset group was updated
                if (agUpdated)
                {
                    agResponse = await client.AssetGroups.ReadAsync(assetGroupId, TestSetup.RequestContext).ConfigureAwait(false);
                    assetGroup = agResponse.Response;
                }

                await TestSetup.PdmsClientInstance.AssetGroups.DeleteAsync(assetGroup.Id, assetGroup.ETag, TestSetup.RequestContext).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // We don't care about the result of the delete, but really shouldn't be getting an error..
                var logger = new ConsoleLogger();
                logger.Warning(nameof(TestBase), ex, $"Unable to delete AssetGroup Id: {assetGroupId}");
            }
        }

        // Remove a newly created data agent.
        protected static async Task CleanupDataAgent(DataAgent agent)
        {
            try
            {
                await TestSetup.PdmsClientInstance.DataAgents
                        .DeleteAsync(agent.Id, agent.ETag, TestSetup.RequestContext).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // We don't care about the result of the delete, but really shouldn't be getting an error..
                var logger = new ConsoleLogger();
                logger.Warning(nameof(TestBase), ex, $"Unable to delete Agent Id: {agent.Id}");
            }
        }

        // Remove newly created data owner.
        protected static async Task CleanupDataOwner(DataOwner owner)
        {
            try
            {
                await TestSetup.PdmsClientInstance.DataOwners
                    .DeleteAsync(owner.Id, owner.ETag, TestSetup.RequestContext).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // We don't care about the result of the delete, but really shouldn't be getting an error..
                var logger = new ConsoleLogger();
                logger.Warning(nameof(TestBase), ex, $"Unable to delete Owner Id: {owner.Id}");
            }
        }

        // Remove a newly created transfer request.
        protected static async Task CleanupTransferRequest(TransferRequest transferRequest)
        {
            try
            {
                await TestSetup.PdmsClientInstance.TransferRequests
                    .DeleteAsync(transferRequest.Id, transferRequest.ETag, TestSetup.RequestContext).ConfigureAwait(false);

                // Remove the asset groups that were created
                foreach (var assetGroupId in transferRequest.AssetGroups)
                {
                    await RemoveAssetGroupAsync(assetGroupId);
                }
            }
            catch (Exception ex)
            {
                // We don't care about the result of the delete, but really shouldn't be getting an error..
                var logger = new ConsoleLogger();
                logger.Warning(nameof(TestBase), ex, $"Unable to delete Transfer Request Id: {transferRequest.Id}");
            }
        }

        // Remove newly created variant definition.
        protected static async Task CleanupVariantDefinition(VariantDefinition variant)
        {
            try
            {
                // Close the VariantDefinition
                variant.State = VariantDefinitionState.Closed;
                variant.Reason = VariantDefinitionReason.Intentional;

                var variantUpdateResponse = await TestSetup.PdmsClientInstance.VariantDefinitions
                    .UpdateAsync(variant, TestSetup.RequestContext).ConfigureAwait(false);

                await TestSetup.PdmsClientInstance.VariantDefinitions
                    .DeleteAsync(variantUpdateResponse.Response.Id, variantUpdateResponse.Response.ETag, TestSetup.RequestContext, force: true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // We don't care about the result of the delete, but really shouldn't be getting an error..
                var logger = new ConsoleLogger();
                logger.Warning(nameof(TestBase), ex, $"Unable to delete Variant Definition Id: {variant.Id}");
            }
        }

        // Utility to clean up some of the test asset groups so that we ensure
        // that we never have more than a page worth for the same asset group.
        public static async Task CleanupTestAssetGroupsAsync(int countThreshold = 100)
        {
            var logger = new ConsoleLogger();
            logger.Information(nameof(TestBase), $"CleanupAssetGroupsAsync: threshold = {countThreshold}");

            // Get the total number of asset groups
            var client = TestSetup.PdmsClientInstance;
            var agResponse = await client.AssetGroups.ReadAllByFiltersAsync(TestSetup.RequestContext)
                .ConfigureAwait(false);

            if (agResponse.HttpStatusCode == HttpStatusCode.OK)
            {
                // If there are a lot, do some cleanup
                int count = agResponse.Response.Count();

                logger.Information(nameof(TestBase), $"{count} Asset Groups found");

                if (count > countThreshold)
                {
                    var testQualifier = AssetQualifier.CreateForCosmosStructuredStream("cosmos17", "cosmosadmin", AssetGroupPathPrefix);

                    // Remove the asset groups one at a time and add a delay to keep from getting throttled
                    foreach (var assetGroup in agResponse.Response)
                    {
                        if (assetGroup.Qualifier.Value.StartsWith(testQualifier.Value))
                        {
                            try
                            {
                                logger.Information(nameof(TestBase), $"Removing {assetGroup.Qualifier.Value}");
                                await RemoveAssetGroupAsync(assetGroup.Id).ConfigureAwait(false);
                                await Task.Delay(10);
                            }
                            catch (Exception ex)
                            {
                                // ignore
                                logger.Warning(nameof(TestBase), ex, $"Couldn't remove {assetGroup.Qualifier.Value}");
                            }

                            // break out when we've done enough
                            if (--count < countThreshold) break;
                        }
                    }
                }

                logger.Information(nameof(TestBase), $"{count} Asset Groups remain");
            }
        }
    }
}
