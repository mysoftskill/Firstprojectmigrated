namespace Microsoft.PrivacyServices.DataManagement.FunctionalTests.FrontdoorTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;
    using Microsoft.PrivacyServices.DataManagement.FunctionalTests.Setup;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;

    [TestClass]
    public class VariantRequestTests : TestBase
    {
        [TestMethod]
        public async Task CanCreateVariantRequestUsingClient()
        {
            var variantRequest = await CreateNewVariantRequestAsync().ConfigureAwait(false);

            await VerifyVariantRequest(variantRequest).ConfigureAwait(false);
            // Get the asset group associated with the variant request, and verify that 
            // the HasPendingVariantRequests flag is set
            var assetGroupId = variantRequest.VariantRelationships.FirstOrDefault().AssetGroupId;
            var client = TestSetup.PdmsClientInstance;
            var response = await client.AssetGroups.ReadAsync(assetGroupId, TestSetup.RequestContext)
                .ConfigureAwait(false);
            var assetGroup = response.Response;

            Assert.IsTrue(assetGroup.HasPendingVariantRequests);

            await CleanupVariantRequest(variantRequest);
        }

        [TestMethod]
        public async Task CanCreateVariantRequestForExcludedDefinition()
        {
            var variantDefinitionExclusionList = new List<string>
            {
                "2c8ac7e3-3aaa-41ee-a2b2-3febe74b1bb7",
                "0f0ab672-68a6-476c-bc33-75b33ef56ae2"
            };
            var index = new Random().Next(variantDefinitionExclusionList.Count);

            var variantRequest = await CreateNewVariantRequestAsync(null, variantDefinitionExclusionList.ElementAt(index)).ConfigureAwait(false);

            await VerifyVariantRequest(variantRequest).ConfigureAwait(false);

            // Get the asset group associated with the variant request, and verify that
            // the HasPendingVariantRequests flag is set
            var assetGroupId = variantRequest.VariantRelationships.FirstOrDefault().AssetGroupId;
            var client = TestSetup.PdmsClientInstance;
            var response = await client.AssetGroups.ReadAsync(assetGroupId, TestSetup.RequestContext)
                .ConfigureAwait(false);
            var assetGroup = response.Response;

            Assert.IsTrue(assetGroup.HasPendingVariantRequests);

            await CleanupVariantRequest(variantRequest);
        }

        [TestMethod]
        public async Task WhenICreateAVariantRequestForAnAssetGroupThatHasPendingVariantRequestDontUpdate()
        {
            var variantRequest1 = await CreateNewVariantRequestAsync().ConfigureAwait(false);

            await VerifyVariantRequest(variantRequest1).ConfigureAwait(false);

            // Get the asset group associated with the variant request
            var assetGroupId = variantRequest1.VariantRelationships.FirstOrDefault().AssetGroupId;
            var client = TestSetup.PdmsClientInstance;
            var response = await client.AssetGroups.ReadAsync(assetGroupId, TestSetup.RequestContext)
                .ConfigureAwait(false);
            var assetGroup = response.Response;

            // Create a new VariantDefinition and link it to the same asset group
            var variantDefinition = await CreateNewVariantDefinitionAsync(assetGroup.OwnerId).ConfigureAwait(false);

            var requestedVariants = new List<AssetGroupVariant>
            {
                new AssetGroupVariant { VariantId = variantDefinition.Id},
            };

            VariantRequest variantRequest2 = new VariantRequest
            {
                OwnerId = variantRequest1.OwnerId,
                OwnerName = variantRequest1.OwnerName,
                RequestedVariants = requestedVariants,
                VariantRelationships = variantRequest1.VariantRelationships
            };

            // Create a second variant request
            var variantRequest2Response = await TestSetup.PdmsClientInstance.VariantRequests
                        .CreateAsync(variantRequest2, TestSetup.RequestContext).ConfigureAwait(false);
            variantRequest2 = variantRequest2Response.Response;
            await VerifyVariantRequest(variantRequest2).ConfigureAwait(false);

            // Get the asset group associated with the variant request again, and verify that it didn't change
            response = await client.AssetGroups.ReadAsync(assetGroupId, TestSetup.RequestContext)
                .ConfigureAwait(false);
            var assetGroup2 = response.Response;

            Assert.AreEqual(assetGroup.ETag, assetGroup2.ETag);

            // Clean up the first request
            await CleanupVariantRequest(variantRequest1);

            // Because this request targets the same asset group,
            // and the asset group cleanup removes any variant request links,
            // this variant request should already be deleted.. 
            await VerifyVariantRequestDoesNotExist(variantRequest2.Id).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task WhenICallCreateVariantRequestsWithNullBodyItFailsWithBadArumentError()
        {
            await Assert.ThrowsExceptionAsync<BadArgumentError.InvalidArgument>(() => TestSetup.PdmsClientInstance.VariantRequests
                .CreateAsync(null, TestSetup.RequestContext)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CantCreateVariantRequestUsingClientIfOwnerIdDoesNotExist()
        {
            // Create an AssetGroup and link it to the VariantDefinition
            var assetGroup = await CreateNewAssetGroupAsync().ConfigureAwait(false);

            var variantDefinition = await CreateNewVariantDefinitionAsync(assetGroup.OwnerId).ConfigureAwait(false);

            var requestedVariants = new List<AssetGroupVariant>
            {
                new AssetGroupVariant { VariantId = variantDefinition.Id},
            };
            var variantRelationships = new List<VariantRelationship>
            {
                new VariantRelationship
                {
                    AssetGroupId = assetGroup.Id,
                    AssetQualifier = assetGroup.Qualifier
                }
            };

            var ownerId = Guid.NewGuid().ToString();
            VariantRequest variantRequest = new VariantRequest
            {
                OwnerId = ownerId,      // Set non-existent owner id
                OwnerName = "Owner" + ownerId,
                RequestedVariants = requestedVariants,
                VariantRelationships = variantRelationships
            };

            await Assert.ThrowsExceptionAsync<ConflictError.DoesNotExist>(() => TestSetup.PdmsClientInstance.VariantRequests
                .CreateAsync(variantRequest, TestSetup.RequestContext)).ConfigureAwait(false);

            await CleanupVariantDefinition(variantDefinition);
            await RemoveAssetGroupAsync(assetGroup.Id).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CantCreateVariantRequestUsingClientIfOwnerIdDoesNotMatch()
        {
            // Create an AssetGroup and link it to a VariantDefinition
            var assetGroup = await CreateNewAssetGroupAsync().ConfigureAwait(false);

            var variantDefinition = await CreateNewVariantDefinitionAsync(assetGroup.OwnerId).ConfigureAwait(false);

            var requestedVariants = new List<AssetGroupVariant>
            {
                new AssetGroupVariant { VariantId = variantDefinition.Id},
            };
            var variantRelationships = new List<VariantRelationship>
            {
                new VariantRelationship
                {
                    AssetGroupId = assetGroup.Id,
                    AssetQualifier = assetGroup.Qualifier
                }
            };

            var ownerId = await GetADataOwnerIdAsync().ConfigureAwait(false);
            VariantRequest variantRequest = new VariantRequest
            {
                OwnerId = ownerId,      // Owner id of variant request is different from owner of asset group
                OwnerName = "Owner" + ownerId,
                RequestedVariants = requestedVariants,
                VariantRelationships = variantRelationships
            };

            await Assert.ThrowsExceptionAsync<ConflictError.InvalidValue>(() => TestSetup.PdmsClientInstance.VariantRequests
                .CreateAsync(variantRequest, TestSetup.RequestContext)).ConfigureAwait(false);

            await CleanupVariantDefinition(variantDefinition);
            await RemoveAssetGroupAsync(assetGroup.Id).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CantCreateVariantRequestIfThereIsDuplicatePendingRequest()
        {
            // Create a variant request
            var variantRequest = await CreateNewVariantRequestAsync().ConfigureAwait(false);
            var conflictingVariantId = variantRequest.RequestedVariants.First().VariantId.ToString();

            // Verify that it exists
            await VerifyVariantRequest(variantRequest).ConfigureAwait(false);

            // Try to create another request with the same asset group and variant
            var newVariantRequest = new VariantRequest()
            {
                OwnerId = variantRequest.OwnerId,
                OwnerName = variantRequest.OwnerName,
                VariantRelationships = variantRequest.VariantRelationships,
                RequestedVariants = variantRequest.RequestedVariants
            };
            var result = await Assert.ThrowsExceptionAsync<ConflictError.AlreadyExists>(() => TestSetup.PdmsClientInstance.VariantRequests
                .CreateAsync(newVariantRequest, TestSetup.RequestContext)).ConfigureAwait(false);
            Assert.AreEqual("Asset group has a pending variant request for one of the requested variants.", result.Message);
            Assert.AreEqual(conflictingVariantId, result.Value);

            // Cleanup
            await CleanupVariantRequest(variantRequest);
        }

        [TestMethod]
        public async Task CantCreateVariantRequestIfThereIsDuplicateApprovedRequest()
        {
            // Create a variant request
            var variantRequest = await CreateNewVariantRequestAsync().ConfigureAwait(false);
            var conflictingVariantId = variantRequest.RequestedVariants.First().VariantId.ToString();

            // Verify that it exists
            await VerifyVariantRequest(variantRequest).ConfigureAwait(false);

            // Approve the request
            var variantRequestResponse = await TestSetup.PdmsClientInstance.VariantRequests
                .ApproveAsync(variantRequest.Id, variantRequest.ETag, TestSetup.RequestContext).ConfigureAwait(false);

            // Try to create another request with the same asset group and variant
            var newVariantRequest = new VariantRequest()
            {
                OwnerId = variantRequest.OwnerId,
                OwnerName = variantRequest.OwnerName,
                VariantRelationships = variantRequest.VariantRelationships,
                RequestedVariants = variantRequest.RequestedVariants
            };
            var result = await Assert.ThrowsExceptionAsync<ConflictError.AlreadyExists>(() => TestSetup.PdmsClientInstance.VariantRequests
                .CreateAsync(newVariantRequest, TestSetup.RequestContext)).ConfigureAwait(false);
            Assert.AreEqual("Asset group has an existing asset group-variant link for one of the requested variants.", result.Message);
            Assert.AreEqual(conflictingVariantId, result.Value);

            // The variant request should already be deleted...
            await VerifyVariantRequestDoesNotExist(variantRequest.Id).ConfigureAwait(false);

            // Clean up asset groups
            foreach (var assetGroup in variantRequest.VariantRelationships)
            {
                await RemoveAssetGroupAsync(assetGroup.AssetGroupId).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task CantCreateVariantRequestUsingClientIfVariantDefinitionDoesNotExist()
        {
            var variantDefinitionId = Guid.NewGuid().ToString();

            // Create an AssetGroup and link it to a bogus VariantDefinition
            var assetGroup = await CreateNewAssetGroupAsync().ConfigureAwait(false);
            var requestedVariants = new List<AssetGroupVariant>
            {
                new AssetGroupVariant { VariantId = variantDefinitionId },
            };
            var variantRelationships = new List<VariantRelationship>
            {
                new VariantRelationship
                {
                    AssetGroupId = assetGroup.Id,
                    AssetQualifier = assetGroup.Qualifier
                }
            };

            VariantRequest variantRequest = new VariantRequest
            {
                OwnerId = assetGroup.OwnerId,
                OwnerName = "Owner" + assetGroup.OwnerId,
                RequestedVariants = requestedVariants,
                VariantRelationships = variantRelationships
            };

            await Assert.ThrowsExceptionAsync<ConflictError.DoesNotExist>(() => TestSetup.PdmsClientInstance.VariantRequests
                .CreateAsync(variantRequest, TestSetup.RequestContext)).ConfigureAwait(false);

            await RemoveAssetGroupAsync(assetGroup.Id).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CantCreateVariantRequestUsingClientIfAssetGroupsDoesNotExist()
        {
            var variantDefinition = await CreateNewVariantDefinitionAsync().ConfigureAwait(false);

            var requestedVariants = new List<AssetGroupVariant>
            {
                new AssetGroupVariant { VariantId = variantDefinition.Id},
            };
            var variantRelationships = new List<VariantRelationship>
            {
                new VariantRelationship
                {
                    AssetGroupId = Guid.NewGuid().ToString(), // Bogus asset group id
                    AssetQualifier = AssetQualifier.CreateForPlatformService("host")
                }
            };

            VariantRequest variantRequest = new VariantRequest
            {
                OwnerId = variantDefinition.OwnerId,
                OwnerName = "Owner" + variantDefinition.OwnerId,
                RequestedVariants = requestedVariants,
                VariantRelationships = variantRelationships
            };

            await Assert.ThrowsExceptionAsync<ConflictError.DoesNotExist>(() => TestSetup.PdmsClientInstance.VariantRequests
                .CreateAsync(variantRequest, TestSetup.RequestContext)).ConfigureAwait(false);

            await CleanupVariantDefinition(variantDefinition);
        }

        [TestMethod]
        public async Task CanReadVariantRequestsUsingClient()
        {
            var variantRequest = await CreateNewVariantRequestAsync().ConfigureAwait(false);

            var variantRequestsResponse = await TestSetup.PdmsClientInstance.VariantRequests.ReadAllByFiltersAsync(
                    TestSetup.RequestContext,
                    VariantRequestExpandOptions.None)
                .ConfigureAwait(false);

            Assert.IsTrue(variantRequestsResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {variantRequestsResponse.HttpStatusCode}");

            Assert.IsTrue(variantRequestsResponse.Response.Any());

            if (variantRequestsResponse.Response.Any(a => a.Id.Equals(variantRequest.Id, StringComparison.OrdinalIgnoreCase)))
            {
                await VerifyVariantRequest(variantRequest).ConfigureAwait(false);

                await CleanupVariantRequest(variantRequest);
            }
            else
            {
                Assert.Fail("Newly created Variant not retrieved");
            }
        }

        [TestMethod]
        public async Task CanUpdateVariantRequestUsingClient()
        {
            var variantRequest = await CreateNewVariantRequestAsync().ConfigureAwait(false);

            var workItemUri = new Uri("https://CanUpdateVariantRequestUsingClient");
            variantRequest.WorkItemUri = workItemUri;

            var variantRequestResponse = await TestSetup.PdmsClientInstance.VariantRequests
                .UpdateAsync(variantRequest, TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(variantRequestResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {variantRequestResponse.HttpStatusCode}");
            Assert.IsTrue(variantRequestResponse.Response.WorkItemUri.ToString() == workItemUri.ToString());
            
            // Cleanup updated variant
            await CleanupVariantRequest(variantRequestResponse.Response);
        }

        [TestMethod]
        public async Task CantUpdateVariantRequestVariantIds()
        {
            // Create a variant request with 2 variants
            var variantRequest = await CreateNewVariantRequestAsync(null, null, false, true).ConfigureAwait(false);

            // Change the id of the first variant
            variantRequest.RequestedVariants.First().VariantId = Guid.NewGuid().ToString();

            var result = await Assert.ThrowsExceptionAsync<ConflictError.InvalidValue.Immutable>(() => TestSetup.PdmsClientInstance.VariantRequests
                .UpdateAsync(variantRequest, TestSetup.RequestContext)).ConfigureAwait(false);
            Assert.AreEqual("RequestedVariants are immutable.", result.Message);

            // Cleanup updated variant
            await CleanupVariantRequest(variantRequest);
        }

        [TestMethod]
        public async Task CanUpdateVariantRequestWithEquivalentVariantIds()
        {
            // Create a variant request with 2 variants
            var variantRequest = await CreateNewVariantRequestAsync(null, null, false, true).ConfigureAwait(false);

            // Change the order of the variants
            variantRequest.RequestedVariants = variantRequest.RequestedVariants.Reverse();

            var workItemUri = new Uri("https://CanUpdateVariantRequestWithEquivalentVariantIds");
            variantRequest.WorkItemUri = workItemUri;

            var variantRequestResponse = await TestSetup.PdmsClientInstance.VariantRequests
                .UpdateAsync(variantRequest, TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(variantRequestResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {variantRequestResponse.HttpStatusCode}");
            Assert.IsTrue(variantRequestResponse.Response.WorkItemUri.ToString() == workItemUri.ToString());

            // Cleanup updated variant
            await CleanupVariantRequest(variantRequest);
        }

        [TestMethod]
        public async Task CantUpdateVariantRequestVariantRelationships()
        {
            // Create a request with 2 asset groups
            var variantRequest = await CreateNewVariantRequestAsync().ConfigureAwait(false);

            // Change the id of the first asset groups
            var originalAssetGroupId = variantRequest.VariantRelationships.First().AssetGroupId;
            variantRequest.VariantRelationships.First().AssetGroupId = Guid.NewGuid().ToString();

            var result = await Assert.ThrowsExceptionAsync<ConflictError.InvalidValue.Immutable>(() => TestSetup.PdmsClientInstance.VariantRequests
                .UpdateAsync(variantRequest, TestSetup.RequestContext)).ConfigureAwait(false);
            Assert.AreEqual("VariantRelationships are immutable.", result.Message);

            // Restore the original asset group id so that it gets cleaned up
            // when the variant request is removed
            variantRequest.VariantRelationships.First().AssetGroupId = originalAssetGroupId;
            await CleanupVariantRequest(variantRequest);
        }

        [TestMethod]
        public async Task CanUpdateVariantRequestWithEquivalentVariantRelationships()
        {
            // Create a request with 2 asset groups
            var variantRequest = await CreateNewVariantRequestAsync(null, null, true, false).ConfigureAwait(false);

            // Change the order of the asset groups
            variantRequest.VariantRelationships = variantRequest.VariantRelationships.Reverse();

            var workItemUri = new Uri("https://CanUpdateVariantRequestWithEquivalentVariantIds");
            variantRequest.WorkItemUri = workItemUri;

            var variantRequestResponse = await TestSetup.PdmsClientInstance.VariantRequests
                .UpdateAsync(variantRequest, TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(variantRequestResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {variantRequestResponse.HttpStatusCode}");
            Assert.IsTrue(variantRequestResponse.Response.WorkItemUri.ToString() == workItemUri.ToString());

            // Cleanup updated variant
            await CleanupVariantRequest(variantRequest);
        }

        [TestMethod]
        public async Task CanApproveVariantRequestUsingClient()
        {
            var variantRequest = await CreateNewVariantRequestAsync().ConfigureAwait(false);

            var workItemUri = new Uri("https://CanApproveVariantRequestUsingClient");
            variantRequest.WorkItemUri = workItemUri;

            // Update the workItemUrl so that we can check that it got copied to the
            // asset group variants
            var updateRequestResponse = await TestSetup.PdmsClientInstance.VariantRequests
                .UpdateAsync(variantRequest, TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(updateRequestResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {updateRequestResponse.HttpStatusCode}");

            variantRequest = updateRequestResponse.Response;

            var assetGroupIds = variantRequest.VariantRelationships.Select(x => x.AssetGroupId);
            var variantIds = variantRequest.RequestedVariants.Select(x => x.VariantId);
            var workItemUrl = variantRequest.WorkItemUri.ToString();

            // Approve the request; if successful, then the request will be deleted, and the asset groups updated
            var variantRequestResponse = await TestSetup.PdmsClientInstance.VariantRequests
                .ApproveAsync(variantRequest.Id, variantRequest.ETag, TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(variantRequestResponse.HttpStatusCode == HttpStatusCode.NoContent,
                $"StatusCode was {variantRequestResponse.HttpStatusCode}");

            // Verify that the request was deleted
            await GetApiCallResponseAsStringAsync($"api/v2/VariantRequests('{variantRequest.Id}')", HttpStatusCode.NotFound).ConfigureAwait(false);

            // Check that the asset groups were updated
            var client = TestSetup.PdmsClientInstance;
            foreach (var assetGroupId in assetGroupIds)
            {
                var agResponse = await client.AssetGroups.ReadAsync(assetGroupId, TestSetup.RequestContext).ConfigureAwait(false);

                Assert.IsTrue(agResponse.HttpStatusCode == HttpStatusCode.OK);
                var assetGroup = agResponse.Response;

                // Check that the flag was cleared
                Assert.IsFalse(assetGroup.HasPendingVariantRequests);

                // Check that the variants were updated
                foreach (var variantId in variantIds)
                {
                    var variants = assetGroup.Variants.Where(x => x.VariantId == variantId);
                    Assert.IsTrue(variants.Count() == 1);
                    Assert.IsTrue(variants.Select(x => x.VariantState == VariantState.Approved).Count() == 1);
                    Assert.IsTrue(variants.First().TfsTrackingUris.Contains(workItemUrl));
                    Assert.IsTrue(variants.First().TfsTrackingUris.Select(x => x.Equals(workItemUrl)).Count() == 1);
                }

                // No longer need the asset group
                await RemoveAssetGroupAsync(assetGroup.Id).ConfigureAwait(false);
            }

            // The variant should already be deleted...
            await VerifyVariantRequestDoesNotExist(variantRequest.Id);
        }

        [TestMethod]
        public async Task CanReadAllVariantRequestsCallingApi()
        {
            var variantRequest = await CreateNewVariantRequestAsync().ConfigureAwait(false);

            var content = await GetApiCallResponseAsStringAsync(
                    "api/v2/VariantRequests?$select=id,eTag,ownerId,ownerName,requesterAlias,generalContractorAlias,celaContactAlias,workItemUri,requestedVariants,variantRelationships")
                  .ConfigureAwait(false);
            Assert.IsNotNull(content);

            var variantRequests = JsonConvert.DeserializeObject<ODataResponse<List<VariantRequest>>>(content);
            Assert.IsTrue(variantRequests.Value.Count > 0);

            // Query with trackingDetails
            content = await GetApiCallResponseAsStringAsync(
                    $"api/v2/VariantRequests?$select=id,eTag,ownerId,ownerName,requesterAlias,generalContractorAlias,celaContactAlias,workItemUri,requestedVariants,variantRelationships,trackingDetails")
                .ConfigureAwait(false);
            Assert.IsNotNull(content);

            variantRequests = JsonConvert.DeserializeObject<ODataResponse<List<VariantRequest>>>(content);
            Assert.IsTrue(variantRequests.Value.Count > 0);

            await CleanupVariantRequest(variantRequest);
        }

        [TestMethod]
        public async Task ReadByFiltersWithValidOwnerIdReturnsResult()
        {
            var variantRequest = await CreateNewVariantRequestAsync().ConfigureAwait(false);

            var filterCriteria = new VariantRequestFilterCriteria()
            {
                OwnerId = variantRequest.OwnerId
            };

            var variantRequestsResponse = await TestSetup.PdmsClientInstance.VariantRequests
                .ReadByFiltersAsync(TestSetup.RequestContext, VariantRequestExpandOptions.None, filterCriteria).ConfigureAwait(false);

            Assert.IsTrue(variantRequestsResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {variantRequestsResponse.HttpStatusCode}");

            var variantRequests = JsonConvert.DeserializeObject<ODataResponse<List<VariantRequest>>>(variantRequestsResponse.ResponseContent);
            Assert.IsTrue(variantRequests.Value.Count == 1);

            var result = variantRequests.Value.ElementAt(0);
            Assert.IsTrue(variantRequest.OwnerId == result.OwnerId);

            await CleanupVariantRequest(variantRequest);
        }

        [TestMethod]
        public async Task CanQueryVariantRequestsCallingApi()
        {
            var variantRequest = await CreateNewVariantRequestAsync().ConfigureAwait(false);

            var content = await GetApiCallResponseAsStringAsync(
                    $"api/v2/VariantRequests?$select=id,eTag,ownerId,ownerName,requesterAlias,generalContractorAlias,celaContactAlias,workItemUri,requestedVariants,variantRelationships&$filter=ownerId eq '{variantRequest.OwnerId}'")
                .ConfigureAwait(false);
            Assert.IsNotNull(content);

            var variantRequests = JsonConvert.DeserializeObject<ODataResponse<List<VariantRequest>>>(content);
            Assert.IsTrue(variantRequests.Value.Count == 1);

            var result = variantRequests.Value.First();
            Assert.IsNotNull(result.CelaContactAlias);
            Assert.IsNotNull(result.GeneralContractorAlias);

            await CleanupVariantRequest(variantRequest);
        }

        [TestMethod]
        public async Task CanDeleteVariantRequestUsingClient()
        {
            var variantRequest = await CreateNewVariantRequestAsync().ConfigureAwait(false);

            var variantRequestDeleteResponse = await TestSetup.PdmsClientInstance.VariantRequests
                .DeleteAsync(variantRequest.Id, variantRequest.ETag, TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(variantRequestDeleteResponse.HttpStatusCode == HttpStatusCode.NoContent,
                $"StatusCode was {variantRequestDeleteResponse.HttpStatusCode}");

            // Verify that the variant request was deleted
            await VerifyVariantRequestDoesNotExist(variantRequest.Id).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CanQueryVariantRequestsWithFilterApi()
        {
            var variantReq1 = await CreateNewVariantRequestAsync().ConfigureAwait(false);
            var variantReq2 = await CreateNewVariantRequestAsync().ConfigureAwait(false);

            // Search for variant request 1 and variant request 2 by etag
            var variantReqs = await FindAllVariantRequestsByFilter($"eTag eq '{variantReq1.ETag}' or eTag eq '{variantReq2.ETag}'").ConfigureAwait(false);

            // Should find at least 2 variant definitions
            Assert.IsTrue(variantReqs.Count >= 2);

            // check for variant request 1
            if (variantReqs.Any(a => a.Id.Equals(variantReq1.Id, StringComparison.OrdinalIgnoreCase)))
            {
                await VerifyVariantRequest(variantReq1).ConfigureAwait(false);
            }
            else
            {
                Assert.Fail("Variant request one could not retrieved by state");
            }

            // check for variant request 2
            if (variantReqs.Any(a => a.Id.Equals(variantReq2.Id, StringComparison.OrdinalIgnoreCase)))
            {
                await VerifyVariantRequest(variantReq2).ConfigureAwait(false);
            }
            else
            {
                Assert.Fail("Variant request two could not retrieved by name");
            }

            await CleanupVariantRequest(variantReq1);
            await CleanupVariantRequest(variantReq2);
        }

        private static async Task VerifyVariantRequest(VariantRequest variantRequest)
        {
            var variantRequestResponse = await TestSetup.PdmsClientInstance.VariantRequests
                .ReadAsync(variantRequest.Id, TestSetup.RequestContext).ConfigureAwait(false);
            Assert.IsTrue(variantRequestResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {variantRequestResponse.HttpStatusCode}");
            VariantRequest storedVariantRequest = variantRequestResponse.Response;
            Assert.AreEqual(variantRequest.Id, storedVariantRequest.Id);
            Assert.AreEqual(variantRequest.OwnerId, storedVariantRequest.OwnerId);
            Assert.AreEqual(variantRequest.OwnerName, storedVariantRequest.OwnerName);
            Assert.AreEqual(variantRequest.RequesterAlias, storedVariantRequest.RequesterAlias);
            Assert.AreEqual(variantRequest.GeneralContractorAlias, storedVariantRequest.GeneralContractorAlias);
            Assert.AreEqual(variantRequest.CelaContactAlias, storedVariantRequest.CelaContactAlias);
            Assert.AreEqual(variantRequest.AdditionalInformation, storedVariantRequest.AdditionalInformation);
            Assert.AreEqual(variantRequest.RequestedVariants.Count(), storedVariantRequest.RequestedVariants.Count());
            Assert.AreEqual(variantRequest.VariantRelationships.Count(), storedVariantRequest.VariantRelationships.Count());
        }

        // Verify that the request was deleted
        private static async Task VerifyVariantRequestDoesNotExist(string variantRequestId)
        {
            await GetApiCallResponseAsStringAsync(
                    $"api/v2/VariantRequests('{variantRequestId}')",
                    HttpStatusCode.NotFound)
                .ConfigureAwait(false);
        }

        private static async Task<List<VariantRequest>> FindAllVariantRequestsByFilter(string filter)
        {
            // Query all variants via the API
            int countReturned;
            int maxQueryResult = 1000;
            int skip = 0;
            var totalCount = 0;
            List<VariantRequest> allVariantDefs = new List<VariantRequest>();
            do
            {
                countReturned = 0;
                var content = await GetApiCallResponseAsStringAsync(
                        $"api/v2/VariantRequests?$$select=id,eTag,ownerId,ownerName,requesterAlias,generalContractorAlias,celaContactAlias,workItemUri,requestedVariants,variantRelationships&$filter={filter}&$top={maxQueryResult}&$skip={skip}")
                    .ConfigureAwait(false);

                if (content != null)
                {
                    var response = JsonConvert.DeserializeObject<ODataResponse<List<VariantRequest>>>(content);
                    var variantRequests = response.Value;

                    if (variantRequests.Count > 0)
                    {
                        allVariantDefs.AddRange(variantRequests);
                    };

                    countReturned = variantRequests.Count;
                    skip += maxQueryResult;
                }

                totalCount += countReturned;

            } while (countReturned > 0);

            return allVariantDefs;
        }

        // Remove the specified variant request.
        private static async Task CleanupVariantRequest(VariantRequest variantRequest)
        {
            try
            {
                // Must get the latest ETag for the request because it may have
                // been updated by the work item creator function
                var variantRequestResponse = await TestSetup.PdmsClientInstance.VariantRequests
                    .ReadAsync(variantRequest.Id, TestSetup.RequestContext).ConfigureAwait(false);

                // If the variant request hasn't already been deleted, remove it
                if (variantRequestResponse.HttpStatusCode == HttpStatusCode.OK)
                {
                    var variantDeleteResponse = await TestSetup.PdmsClientInstance.VariantRequests
                        .DeleteAsync(variantRequest.Id, variantRequestResponse.Response.ETag, TestSetup.RequestContext).ConfigureAwait(false);
                }

                foreach (var variant in variantRequest.VariantRelationships)
                {
                    await RemoveAssetGroupAsync(variant.AssetGroupId).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                // We don't care about the result of the delete, but really shouldn't be getting an error..
                var logger = new ConsoleLogger();
                logger.Warning(nameof(VariantRequestTests), ex, $"Unable to delete Variant Request Id: {variantRequest.Id}");
            }
        }
    }
}
