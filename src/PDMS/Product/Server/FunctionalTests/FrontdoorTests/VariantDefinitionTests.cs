namespace Microsoft.PrivacyServices.DataManagement.FunctionalTests.FrontdoorTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;
    using Microsoft.PrivacyServices.DataManagement.FunctionalTests.Setup;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;

    [TestClass]
    public class VariantDefinitionTests : TestBase
    {
        [TestMethod]
        public async Task CanCreateVariantDefinitionUsingClient()
        {
            var variant = await CreateNewVariantDefinitionAsync().ConfigureAwait(false);

            await VerifyVariantExists(variant.Id).ConfigureAwait(false);

            await CleanupVariantDefinition(variant);
        }


        [TestMethod]
        public async Task WhenICallCreateVariantDefinitionsWithNullBodyItFailsWithBadArumentError()
        {
            await Assert.ThrowsExceptionAsync<BadArgumentError.InvalidArgument>(() => TestSetup.PdmsClientInstance.VariantDefinitions
                .CreateAsync(null, TestSetup.RequestContext)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CanReadVariantDefinitionsUsingClient()
        {
            var variant = await CreateNewVariantDefinitionAsync().ConfigureAwait(false);

            var variantsResponse = await TestSetup.PdmsClientInstance.VariantDefinitions.ReadAllByFiltersAsync(
                    TestSetup.RequestContext,
                    VariantDefinitionExpandOptions.None)
                .ConfigureAwait(false);

            Assert.IsTrue(variantsResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {variantsResponse.HttpStatusCode}");

            Assert.IsTrue(variantsResponse.Response.Any());

            if (variantsResponse.Response.Any(a => a.Id.Equals(variant.Id, StringComparison.OrdinalIgnoreCase)))
            {
                await VerifyVariantExists(variant.Id).ConfigureAwait(false);
            }
            else
            {
                Assert.Fail("Newly created Variant not retrieved");
            }

            await CleanupVariantDefinition(variant).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CanUpdateVariantDefinitionUsingClient()
        {
            var variant = await CreateNewVariantDefinitionAsync().ConfigureAwait(false);
            var capabilities = new List<CapabilityId>
            {
                Policies.Current.Capabilities.Ids.Delete,
                Policies.Current.Capabilities.Ids.AccountClose
            };
            variant.Capabilities = capabilities;

            var variantResponse = await TestSetup.PdmsClientInstance.VariantDefinitions
                .UpdateAsync(variant, TestSetup.RequestContext).ConfigureAwait(false);

            var updatedVariant = variantResponse.Response;

            Assert.IsTrue(variantResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {variantResponse.HttpStatusCode}");
            Assert.IsTrue(variantResponse.Response.Capabilities.Count() == 2);

            await CleanupVariantDefinition(updatedVariant);
        }

        [TestMethod]
        public async Task CanUpdateVariantDefinitionStateAndReason()
        {
            var variant = await CreateNewVariantDefinitionAsync().ConfigureAwait(false);
            variant.State = VariantDefinitionState.Closed;
            variant.Reason = VariantDefinitionReason.Intentional;

            var variantResponse = await TestSetup.PdmsClientInstance.VariantDefinitions
                .UpdateAsync(variant, TestSetup.RequestContext).ConfigureAwait(false);

            var updatedVariant = variantResponse.Response;

            Assert.IsTrue(variantResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {variantResponse.HttpStatusCode}");
            Assert.AreEqual(variant.State, updatedVariant.State);
            Assert.AreEqual(variant.Reason, updatedVariant.Reason);

            await CleanupVariantDefinition(updatedVariant);
        }

        [TestMethod]
        public async Task CanUpdateVariantDefinitionEgrcIdAndName()
        {
            string egrcId = Guid.NewGuid().ToString();
            string egrcName = $"Egrc Name - {egrcId}";
            var variant = await CreateNewVariantDefinitionAsync().ConfigureAwait(false);
            variant.EgrcId = egrcId;
            variant.EgrcName = egrcName;

            var variantResponse = await TestSetup.PdmsClientInstance.VariantDefinitions
                .UpdateAsync(variant, TestSetup.RequestContext).ConfigureAwait(false);

            var updatedVariant = variantResponse.Response;

            Assert.IsTrue(variantResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {variantResponse.HttpStatusCode}");
            Assert.AreEqual(egrcId, updatedVariant.EgrcId);
            Assert.AreEqual(egrcName, updatedVariant.EgrcName);

            await CleanupVariantDefinition(updatedVariant);
        }

        [TestMethod]
        public async Task CanUpdateVariantDefinitionClosedStateReason()
        {
            // Create a new variant
            var variant = await CreateNewVariantDefinitionAsync().ConfigureAwait(false);
            variant.State = VariantDefinitionState.Closed;
            variant.Reason = VariantDefinitionReason.Intentional;

            // Change the State to Closed with reason Intentional
            var variantResponse = await TestSetup.PdmsClientInstance.VariantDefinitions
                .UpdateAsync(variant, TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(variantResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {variantResponse.HttpStatusCode}");

            // Modify the updated variant to change the Reason to Expired
            var updatedVariant = variantResponse.Response;
            updatedVariant.State = VariantDefinitionState.Closed;
            updatedVariant.Reason = VariantDefinitionReason.Expired;

            var variantResponse2 = await TestSetup.PdmsClientInstance.VariantDefinitions
                .UpdateAsync(updatedVariant, TestSetup.RequestContext).ConfigureAwait(false);
            updatedVariant = variantResponse2.Response;

            Assert.AreEqual(updatedVariant.State, updatedVariant.State);
            Assert.AreEqual(updatedVariant.Reason, updatedVariant.Reason);

            await CleanupVariantDefinition(updatedVariant);
        }

        [TestMethod]
        public async Task CantSetVariantDefinitionStateActiveAndReasonNotNone()
        {
            var variant = await CreateNewVariantDefinitionAsync().ConfigureAwait(false);
            variant.State = VariantDefinitionState.Active;
            variant.Reason = VariantDefinitionReason.Intentional;

            await Assert.ThrowsExceptionAsync<ConflictError.InvalidValue.StateTransition>(() => TestSetup.PdmsClientInstance.VariantDefinitions
                .UpdateAsync(variant, TestSetup.RequestContext)).ConfigureAwait(false);

            await CleanupVariantDefinition(variant);
        }

        [TestMethod]
        public async Task CantSetVariantDefinitionStateClosedAndReasonNone()
        {
            var variant = await CreateNewVariantDefinitionAsync().ConfigureAwait(false);
            variant.State = VariantDefinitionState.Closed;
            variant.Reason = VariantDefinitionReason.None;

            await Assert.ThrowsExceptionAsync<ConflictError.InvalidValue.StateTransition>(() => TestSetup.PdmsClientInstance.VariantDefinitions
                .UpdateAsync(variant, TestSetup.RequestContext)).ConfigureAwait(false);

            await CleanupVariantDefinition(variant);
        }

        [TestMethod]
        public async Task CanReadAllVariantDefinitionsCallingApi()
        {
            var variant = await CreateNewVariantDefinitionAsync().ConfigureAwait(false);

            var content = await GetApiCallResponseAsStringAsync(
                    "api/v2/VariantDefinitions?$select=id,eTag,name,description,egrcId,egrcName,dataTypes,capabilities,subjectTypes,approver,ownerId,state,reason")
                .ConfigureAwait(false);
            Assert.IsNotNull(content);

            var variants = JsonConvert.DeserializeObject<ODataResponse<List<VariantDefinition>>>(content);
            Assert.IsTrue(variants.Value.Count > 0);

            // Query with trackingDetails
            content = await GetApiCallResponseAsStringAsync(
                    $"api/v2/VariantDefinitions?$select=id,eTag,name,description,egrcId,egrcName,dataTypes,capabilities,subjectTypes,approver,ownerId,state,reason,trackingDetails")
                .ConfigureAwait(false);
            Assert.IsNotNull(content);

            variants = JsonConvert.DeserializeObject<ODataResponse<List<VariantDefinition>>>(content);
            Assert.IsTrue(variants.Value.Count > 0);

            await CleanupVariantDefinition(variant);
        }

        [TestMethod]
        public async Task ReadAllWithNoFiltersReturnsOnlyActiveStateDefinitions()
        {
            // Create new variant
            var variant = await CreateNewVariantDefinitionAsync().ConfigureAwait(false);
            variant.State = VariantDefinitionState.Closed;
            variant.Reason = VariantDefinitionReason.Intentional;

            // Change the variant state to 'Closed'
            var variantUpdateResponse = await TestSetup.PdmsClientInstance.VariantDefinitions
                .UpdateAsync(variant, TestSetup.RequestContext).ConfigureAwait(false);

            var updatedVariant = variantUpdateResponse.Response;

            Assert.IsTrue(variantUpdateResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {variantUpdateResponse.HttpStatusCode}");

            // Get all variants
            var variantResponse = await TestSetup.PdmsClientInstance.VariantDefinitions
                .ReadAllByFiltersAsync(TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(variantResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {variantResponse.HttpStatusCode}");

            // Make sure none of the variants are in the Closed state
            var variantDefinitions = variantResponse.Response.ToList();
            if (variantDefinitions.Count > 0)
            {
                var closedVariants = variantDefinitions.Where(v => v.State == VariantDefinitionState.Closed);

                Assert.IsTrue(condition: !closedVariants.Any());
            }

            // Clean up the variant we created
            await CleanupVariantDefinition(updatedVariant);
        }

        [TestMethod]
        public async Task ReadAllWithNoFiltersReturnsAllActiveStateDefinitions()
        {
            // Get all variants via client
            var variantResponse = await TestSetup.PdmsClientInstance.VariantDefinitions
                .ReadAllByFiltersAsync(TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(variantResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {variantResponse.HttpStatusCode}");

            // Make sure none of the variants are in the Closed state
            var variantDefinitions = variantResponse.Response.ToList();
            if (variantDefinitions.Count > 0)
            {
                var closedVariants = variantDefinitions.Where(v => v.State == VariantDefinitionState.Closed);

                Assert.IsTrue(condition: !closedVariants.Any());
            }
            var variantDefinitionsCount1 = variantDefinitions.Count;

            // Query with variants via the API
            int countReturned;
            int maxQueryResult = 1000;
            int skip = 0;
            var variantDefinitionsCount2 = 0;
            List<VariantDefinition> allActiveVariants = new List<VariantDefinition>();
            do
            {
                countReturned = 0;
                var content = await GetApiCallResponseAsStringAsync(
                        $"api/v2/VariantDefinitions?$select=id,eTag,name,description,egrcId,egrcName,dataTypes,capabilities,subjectTypes,approver,ownerId,state,reason&$top={maxQueryResult}&$skip={skip}")
                    .ConfigureAwait(false);

                if (content != null)
                {
                    var response = JsonConvert.DeserializeObject<ODataResponse<List<VariantDefinition>>>(content);
                    var variantDefs = response.Value;
                    if (variantDefs.Count > 0)
                    {
                        var closedVariants = variantDefs.Where(v => v.State == VariantDefinitionState.Closed);

                        Assert.IsTrue(condition: !closedVariants.Any());
                    }
                    countReturned = variantDefs.Count;
                    skip += maxQueryResult;
                }

                variantDefinitionsCount2 += countReturned;

            } while (countReturned > 0);

            Assert.IsTrue(variantDefinitionsCount1 == variantDefinitionsCount2);
        }

        [TestMethod]
        public async Task ReadByFiltersWithBadStateReturnsException()
        {
            var filterCriteria = new VariantDefinitionFilterCriteria()
            {
                State = "InvalidStateValue"
            };

            await Assert.ThrowsExceptionAsync<BadArgumentError.InvalidArgument>(() => TestSetup.PdmsClientInstance.VariantDefinitions
                .ReadByFiltersAsync(TestSetup.RequestContext, VariantDefinitionExpandOptions.None, filterCriteria)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CanQueryVariantDefinitionsCallingApi()
        {
            var variant = await CreateNewVariantDefinitionAsync().ConfigureAwait(false);

            var content = await GetApiCallResponseAsStringAsync(
                    $"api/v2/VariantDefinitions?$select=id,eTag,name,description,egrcId,egrcName,dataTypes,capabilities,subjectTypes,approver,ownerId,state,reason&$filter=name eq '{variant.Name}'")
                .ConfigureAwait(false);
            Assert.IsNotNull(content);

            var variants = JsonConvert.DeserializeObject<ODataResponse<List<VariantDefinition>>>(content);
            Assert.IsTrue(variants.Value.Count == 1);


            var result = variants.Value.First();
            Assert.IsNotNull(result.EgrcId);
            Assert.IsNotNull(result.EgrcName);

            await CleanupVariantDefinition(variant).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CanQueryVariantDefinitionsByClosedStateCallingApi()
        {
            var variant = await CreateNewVariantDefinitionAsync().ConfigureAwait(false);

            // Change the variant state to 'Closed'
            variant.State = VariantDefinitionState.Closed;
            variant.Reason = VariantDefinitionReason.Intentional;
            var variantUpdateResponse = await TestSetup.PdmsClientInstance.VariantDefinitions
                .UpdateAsync(variant, TestSetup.RequestContext).ConfigureAwait(false);

            var updatedVariant = variantUpdateResponse.Response;

            var variants = await FindAllVariantDefinitionsByFilter($"state eq 'Closed'").ConfigureAwait(false);
            Assert.IsNotNull(variants);

            Assert.IsTrue(variants.Count >= 1);

            if (variants.Any(a => a.Id.Equals(variant.Id, StringComparison.OrdinalIgnoreCase)))
            {
                await VerifyVariantExists(variant.Id).ConfigureAwait(false);
            }
            else
            {
                Assert.Fail("Newly closed Variant not retrieved");
            }

            await CleanupVariantDefinition(updatedVariant);
        }

        [TestMethod]
        public async Task CanQueryVariantDefinitionsByNameOrActiveStateCallingApi()
        {
            var variant1 = await CreateNewVariantDefinitionAsync().ConfigureAwait(false);
            var variant2 = await CreateNewVariantDefinitionAsync().ConfigureAwait(false);
            variant2.State = VariantDefinitionState.Closed;
            variant2.Reason = VariantDefinitionReason.Intentional;

            // Change the variant2 state to 'Closed'
            var variantUpdateResponse = await TestSetup.PdmsClientInstance.VariantDefinitions
                .UpdateAsync(variant2, TestSetup.RequestContext).ConfigureAwait(false);
            variant2 = variantUpdateResponse.Response;

            // Search for variant1 by name and variant 2 by state
            var variantDefs = await FindAllVariantDefinitionsByFilter($"name eq '{variant2.Name}' or state eq 'Active'").ConfigureAwait(false);

            // Should find at least 2 variant definitions
            Assert.IsTrue(variantDefs.Count >= 2);

            // check for variant def 1
            if (variantDefs.Any(a => a.Id.Equals(variant1.Id, StringComparison.OrdinalIgnoreCase)))
            {
                await VerifyVariantExists(variant1.Id).ConfigureAwait(false);
            }
            else
            {
                Assert.Fail("Variant1 not retrieved by state");
            }

            // check for variant def 2
            if (variantDefs.Any(a => a.Id.Equals(variant2.Id, StringComparison.OrdinalIgnoreCase)))
            {
                await VerifyVariantExists(variant2.Id).ConfigureAwait(false);
            }
            else
            {
                Assert.Fail("Variant2 not retrieved by name");
            }

            await CleanupVariantDefinition(variant1);
            await CleanupVariantDefinition(variant2);
        }

        [TestMethod]
        public async Task CanQueryVariantDefinitionsByNameOrClosedStateCallingApi()
        {
            var variant1 = await CreateNewVariantDefinitionAsync().ConfigureAwait(false);
            var variant2 = await CreateNewVariantDefinitionAsync().ConfigureAwait(false);
            variant2.State = VariantDefinitionState.Closed;
            variant2.Reason = VariantDefinitionReason.Intentional;

            // Change the variant2 state to 'Closed'
            var variantUpdateResponse = await TestSetup.PdmsClientInstance.VariantDefinitions
                .UpdateAsync(variant2, TestSetup.RequestContext).ConfigureAwait(false);
            variant2 = variantUpdateResponse.Response;

            // Search for variant1 by name and variant 2 by state
            var variantDefs = await FindAllVariantDefinitionsByFilter($"name eq '{variant1.Name}' or state eq 'Closed'").ConfigureAwait(false);

            // Should find at least 2 variant definitions
            Assert.IsTrue(variantDefs.Count >= 2);

            // check for variant def 1
            if (variantDefs.Any(a => a.Id.Equals(variant1.Id, StringComparison.OrdinalIgnoreCase)))
            {
                await VerifyVariantExists(variant1.Id).ConfigureAwait(false);
            }
            else
            {
                Assert.Fail("Variant1 not retrieved by name");
            }

            // check for variant def 2
            if (variantDefs.Any(a => a.Id.Equals(variant2.Id, StringComparison.OrdinalIgnoreCase)))
            {
                await VerifyVariantExists(variant2.Id).ConfigureAwait(false);
            }
            else
            {
                Assert.Fail("Variant2 not retrieved by state");
            }

            await CleanupVariantDefinition(variant1);
            await CleanupVariantDefinition(variant2);
        }

        [TestMethod]
        public async Task CanDeleteVariantDefinitionUsingClient()
        {
            var variant = await CreateNewVariantDefinitionAsync().ConfigureAwait(false);

            // Close the VariantDefinition
            variant.State = VariantDefinitionState.Closed;
            variant.Reason = VariantDefinitionReason.Intentional;

            var variantUpdateResponse = await TestSetup.PdmsClientInstance.VariantDefinitions
                .UpdateAsync(variant, TestSetup.RequestContext).ConfigureAwait(false);

            var variantDeleteResponse = await TestSetup.PdmsClientInstance.VariantDefinitions
                .DeleteAsync(variantUpdateResponse.Response.Id, variantUpdateResponse.Response.ETag, TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(variantDeleteResponse.HttpStatusCode == HttpStatusCode.NoContent,
                $"StatusCode was {variantDeleteResponse.HttpStatusCode}");

            await GetApiCallResponseAsStringAsync(
                    $"api/v2/VariantDefinitions('{variant.Id}')",
                    HttpStatusCode.NotFound)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CantDeleteVariantDefinitionInActiveState()
        {
            var variant = await CreateNewVariantDefinitionAsync().ConfigureAwait(false);

            await Assert.ThrowsExceptionAsync<ConflictError.InvalidValue.StateTransition>(() => TestSetup.PdmsClientInstance.VariantDefinitions
                .DeleteAsync(variant.Id, variant.ETag, TestSetup.RequestContext)).ConfigureAwait(false);

            await CleanupVariantDefinition(variant).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CantDeleteVariantDefinitionWithLinkedAsset()
        {
            var variant = await CreateNewVariantDefinitionAsync().ConfigureAwait(false);

            // Create an AssetGroup and link it to the VariantDefinition
            var assetGroup = await CreateNewAssetGroupAsync().ConfigureAwait(false);
            var variants = new List<AssetGroupVariant>
            {
                new AssetGroupVariant { VariantId = variant.Id},
            };

            assetGroup.Variants = variants;

            var agResponse = await TestSetup.PdmsClientInstance.AssetGroups.UpdateAsync(assetGroup, TestSetup.RequestContext).ConfigureAwait(false);
            Assert.IsTrue(agResponse.HttpStatusCode == HttpStatusCode.OK);

            // Close the VariantDefinition
            variant.State = VariantDefinitionState.Closed;
            variant.Reason = VariantDefinitionReason.Intentional;

            var variantUpdateResponse = await TestSetup.PdmsClientInstance.VariantDefinitions
                .UpdateAsync(variant, TestSetup.RequestContext).ConfigureAwait(false);

            var updatedVariant = variantUpdateResponse.Response;

            await Assert.ThrowsExceptionAsync<ConflictError.LinkedEntityExists>(() => TestSetup.PdmsClientInstance.VariantDefinitions
                .DeleteAsync(variant.Id, updatedVariant.ETag, TestSetup.RequestContext)).ConfigureAwait(false);

            await RemoveAssetGroupAsync(assetGroup.Id);
            await CleanupVariantDefinition(updatedVariant);
        }

        [TestMethod]
        public async Task CanForceDeleteVariantDefinitionWithLinkedAsset()
        {
            var variant = await CreateNewVariantDefinitionAsync().ConfigureAwait(false);

            // Create an AssetGroup and link it to the VariantDefinition
            var assetGroup = await CreateNewAssetGroupAsync().ConfigureAwait(false);
            var variants = new List<AssetGroupVariant>
            {
                new AssetGroupVariant { VariantId = variant.Id},
            };

            assetGroup.Variants = variants;
            var agResponse = await TestSetup.PdmsClientInstance.AssetGroups.UpdateAsync(assetGroup, TestSetup.RequestContext).ConfigureAwait(false);
            Assert.IsTrue(agResponse.HttpStatusCode == HttpStatusCode.OK);

            // Close the VariantDefinition
            variant.State = VariantDefinitionState.Closed;
            variant.Reason = VariantDefinitionReason.Intentional;

            var variantUpdateResponse = await TestSetup.PdmsClientInstance.VariantDefinitions
                .UpdateAsync(variant, TestSetup.RequestContext).ConfigureAwait(false);

            // Forcibly delete the VariantDefinition
            await TestSetup.PdmsClientInstance.VariantDefinitions
                .DeleteAsync(variant.Id, variantUpdateResponse.Response.ETag, TestSetup.RequestContext, force: true).ConfigureAwait(false);

            // The VariantDefinition should no longer exist
            await GetApiCallResponseAsStringAsync(
                    $"api/v2/VariantDefinitions('{variant.Id}')",
                    HttpStatusCode.NotFound)
                .ConfigureAwait(false);

            // The AssetGroup should no longer have the VariantDefinition
            agResponse = await TestSetup.PdmsClientInstance.AssetGroups.ReadAsync(assetGroup.Id, TestSetup.RequestContext).ConfigureAwait(false);
            Assert.IsFalse(agResponse.Response.Variants.Any());

            await RemoveAssetGroupAsync(assetGroup.Id);
        }

        [TestMethod]
        public async Task WhenICallApiToReadVariantDefinitionUsingHeadMethodItFailsAsync()
        {
            var content = await GetApiCallResponseAsStringAsync($"api/v2/VariantDefinitions",
                            HttpStatusCode.BadRequest,
                            HttpMethod.Head)
                .ConfigureAwait(false);
            Assert.IsTrue(string.IsNullOrEmpty(content));
        }

        private static async Task VerifyVariantExists(string variantId)
        {
            var variantResponse = await TestSetup.PdmsClientInstance.VariantDefinitions
                .ReadAsync(variantId, TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(variantResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {variantResponse.HttpStatusCode}");
            Assert.AreEqual(variantId, variantResponse.Response.Id);
        }

        private static async Task<List<VariantDefinition>> FindAllVariantDefinitionsByFilter(string filter)
        {
            // Query all variants via the API
            int countReturned;
            int maxQueryResult = 1000;
            int skip = 0;
            var totalCount = 0;
            List<VariantDefinition> allVariantDefs = new List<VariantDefinition>();
            do
            {
                countReturned = 0;
                var content = await GetApiCallResponseAsStringAsync(
                        $"api/v2/VariantDefinitions?$select=id,eTag,name,description,egrcId,egrcName,dataTypes,capabilities,subjectTypes,approver,ownerId,state,reason&$filter={filter}&$top={maxQueryResult}&$skip={skip}")
                   .ConfigureAwait(false);

                if (content != null)
                {
                    var response = JsonConvert.DeserializeObject<ODataResponse<List<VariantDefinition>>>(content);
                    var variantDefs = response.Value;

                    if (variantDefs.Count > 0)
                    {
                        allVariantDefs.AddRange(variantDefs);
                    };

                    countReturned = variantDefs.Count;
                    skip += maxQueryResult;
                }

                totalCount += countReturned;

            } while (countReturned > 0);

            return allVariantDefs;
        }
    }
}
