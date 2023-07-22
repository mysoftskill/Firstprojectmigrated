namespace Microsoft.PrivacyServices.DataManagement.FunctionalTests.FrontdoorTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.DataManagement.Client.Filters;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;
    using Microsoft.PrivacyServices.DataManagement.FunctionalTests.Setup;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;

    [TestClass]
    public class AssetGroupsTests : TestBase
    {
        [ClassInitialize()]
        public static async Task CleanupTestAssetGroupsAsync(TestContext context)
        {
            // Clean up test asset groups, if needed
            await TestBase.CleanupTestAssetGroupsAsync();
        }

        [TestMethod]
        public async Task WhenICallApiToReadAllAssetGroupsIGetNonZeroResultsAsync()
        {
            var content = await GetApiCallResponseAsStringAsync("api/v2/AssetGroups").ConfigureAwait(false);
            Assert.IsNotNull(content);

            var assetGroups = JsonConvert.DeserializeObject<ODataResponse<ICollection<AssetGroup>>>(content);

            Assert.IsNotNull(assetGroups);
            Assert.IsTrue(assetGroups.Value.Count > 0);
        }

        [TestMethod]
        [ExpectedException(typeof(BadArgumentError.InvalidArgument))]
        public async Task WhenICallCreateAssetGroupsWithNullAssetGroupItFailsAsync()
        {
            AssetGroup assetGroup = null;
            await TestSetup.PdmsClientInstance.AssetGroups.CreateAsync(assetGroup, TestSetup.RequestContext)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task WhenICreateAnAssetGroupAndReadItSucceedsAsync()
        {
            var assetGroup = await CreateNewAssetGroupAsync().ConfigureAwait(false);
            Assert.IsNotNull(assetGroup);

            try
            {
                var client = TestSetup.PdmsClientInstance;
                var agResponse = await client.AssetGroups.ReadAsync(assetGroup.Id, TestSetup.RequestContext).ConfigureAwait(false);

                Assert.IsTrue(agResponse.HttpStatusCode == HttpStatusCode.OK);
                Assert.AreEqual(agResponse.Response.Id, assetGroup.Id);
            }
            finally
            {
                // Remove the asset group we created.
                await RemoveAssetGroupAsync(assetGroup.Id);
            }
        }

        [TestMethod]
        public async Task WhenICreateAnAssetGroupAndReadByOwnerIdItSucceedsAsync()
        {
            var assetGroup = await CreateNewAssetGroupAsync().ConfigureAwait(false);
            Assert.IsNotNull(assetGroup);

            try
            {
                var client = TestSetup.PdmsClientInstance;
                var agResponse = await client.AssetGroups.ReadByFiltersAsync(TestSetup.RequestContext,
                        AssetGroupExpandOptions.None,
                        new AssetGroupFilterCriteria() { OwnerId = assetGroup.OwnerId, Count = 2, Index = 0 })
                    .ConfigureAwait(false);

                Assert.IsTrue(agResponse.HttpStatusCode == HttpStatusCode.OK);
                Assert.AreEqual(agResponse.Response?.Value?.FirstOrDefault()?.Id, assetGroup.Id);
            }
            finally
            {
                // Remove the asset group we created.
                await RemoveAssetGroupAsync(assetGroup.Id);
            }
        }

        [TestMethod]
        public async Task WhenICreateAnAssetGroupAndReadByAssetQualifierItSucceedsAsync()
        {
            var assetGroup = await CreateNewAssetGroupAsync().ConfigureAwait(false);
            Assert.IsNotNull(assetGroup);

            try
            {
                var client = TestSetup.PdmsClientInstance;
                var agResponse = await client.AssetGroups.ReadByFiltersAsync(TestSetup.RequestContext,
                        AssetGroupExpandOptions.None,
                        new AssetGroupFilterCriteria()
                        {
                            Qualifier = new StringFilter(assetGroup.Qualifier.Value, StringComparisonType.Equals),
                            Count = 2,
                            Index = 0
                        })
                    .ConfigureAwait(false);

                Assert.IsTrue(agResponse.HttpStatusCode == HttpStatusCode.OK);
                Assert.AreEqual(agResponse.Response?.Value?.FirstOrDefault()?.Id, assetGroup.Id);
            }
            finally
            {
                // Remove the asset group we created.
                await RemoveAssetGroupAsync(assetGroup.Id);
            }
        }

        [TestMethod]
        public async Task WhenICallCalculateRegistrationStatusForAnAssetGroupItSucceedsAsync()
        {
            var assetQualifier = AssetQualifier.CreateForCosmosStructuredStream("cosmos15", "PXSCosmos15.Prod", "/local/upload/PROD/DeleteSignal/CookedStream/v2");
            var owner = await CreateDataOwnerWithoutServiceTreeAsync().ConfigureAwait(false);
            var client = TestSetup.PdmsClientInstance;
            AssetGroup assetGroup;

            var agsResponse = await client.AssetGroups.ReadByFiltersAsync(TestSetup.RequestContext,
                    AssetGroupExpandOptions.None,
                    new AssetGroupFilterCriteria()
                    {
                        Qualifier = new StringFilter(assetQualifier.Value, StringComparisonType.Equals),
                        Count = 2,
                        Index = 0
                    })
                .ConfigureAwait(false);
            assetGroup = agsResponse.Response.Value.FirstOrDefault();
            bool assetGroupCreated = false;
            if (assetGroup == null)
            {
                assetGroup = new AssetGroup
                {
                    Qualifier = assetQualifier,
                    OwnerId = owner.Id
                };
                var agResponse = await client.AssetGroups.CreateAsync(assetGroup, TestSetup.RequestContext)
                    .ConfigureAwait(false);
                assetGroup = agResponse.Response;
                assetGroupCreated = true;
            }

            try
            {
                var regStatusResponse = await client.AssetGroups
                    .CalculateRegistrationStatus(assetGroup.Id, TestSetup.RequestContext)
                    .ConfigureAwait(false);

                Assert.IsTrue(regStatusResponse.HttpStatusCode == HttpStatusCode.OK);
                Assert.IsNotNull(regStatusResponse.Response.Assets);
            }
            finally
            {
                // Remove the asset group that we created.
                if (assetGroupCreated)
                {
                    await RemoveAssetGroupAsync(assetGroup.Id);
                }
                await CleanupDataOwner(owner);
            }
        }

        [TestMethod]
        public async Task WhenIUpdateAnAssetGroupItSucceedsAsync()
        {
            var assetGroup = await CreateNewAssetGroupAsync().ConfigureAwait(false);
            Assert.IsNotNull(assetGroup);
            try
            {
                assetGroup.IsExportAgentInheritanceBlocked = true;
                assetGroup.IsDeleteAgentInheritanceBlocked = true;
                assetGroup.IsVariantsInheritanceBlocked = true;

                var client = TestSetup.PdmsClientInstance;
                var agResponse = await client.AssetGroups.UpdateAsync(assetGroup, TestSetup.RequestContext).ConfigureAwait(false);
                Assert.IsTrue(agResponse.HttpStatusCode == HttpStatusCode.OK);

                string updatedETag = agResponse.Response.ETag;

                Assert.AreEqual(agResponse.Response.Id, assetGroup.Id);
                Assert.IsTrue(agResponse.Response.IsExportAgentInheritanceBlocked);
                Assert.IsTrue(agResponse.Response.IsDeleteAgentInheritanceBlocked);
                Assert.IsTrue(agResponse.Response.IsVariantsInheritanceBlocked);
            }
            finally
            {
                // Remove the asset group that we created.
                await RemoveAssetGroupAsync(assetGroup.Id);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ConflictError.InvalidValue.LinkedEntityExists))]
        public async Task CantDeleteAssetGroupWithPendingTransferRequest()
        {
            var transferRequest = await CreateNewTransferRequestAsync().ConfigureAwait(false);
            try
            {
                var assetGroupId = transferRequest.AssetGroups.FirstOrDefault();
                var assetGroupResponse = await TestSetup.PdmsClientInstance.AssetGroups.ReadAsync(
                                                    assetGroupId,
                                                    TestSetup.RequestContext,
                                                    AssetGroupExpandOptions.None)
                                                .ConfigureAwait(false);
                var assetGroup = assetGroupResponse.Response;

                var deleteResponse = await TestSetup.PdmsClientInstance.AssetGroups
                    .DeleteAsync(assetGroup.Id, assetGroup.ETag, TestSetup.RequestContext).ConfigureAwait(false);
            }
            finally
            {
                await CleanupTransferRequest(transferRequest);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ConflictError.InvalidValue.Immutable))]
        public async Task CantUpdateAssetGroupOwnerWithPendingTransferRequest()
        {
            var transferRequest = await CreateNewTransferRequestAsync().ConfigureAwait(false);
            var owner = await CreateDataOwnerWithoutServiceTreeAsync().ConfigureAwait(false);

            try
            {
                var assetGroupId = transferRequest.AssetGroups.FirstOrDefault();
                var assetGroupResponse = await TestSetup.PdmsClientInstance.AssetGroups.ReadAsync(
                                                    assetGroupId,
                                                    TestSetup.RequestContext,
                                                    AssetGroupExpandOptions.None)
                                                .ConfigureAwait(false);
                var assetGroup = assetGroupResponse.Response;


                assetGroup.OwnerId = owner.Id;

                var updateResponse = await TestSetup.PdmsClientInstance.AssetGroups
                .UpdateAsync(assetGroup, TestSetup.RequestContext).ConfigureAwait(false);

            }
            finally
            {
                await CleanupDataOwner(owner);
                await CleanupTransferRequest(transferRequest);
            }
        }

        [TestMethod]
        public async Task WhenIQueryForAComplianceStateByAssetQualifierForExistingAssetGroupItReturnsTrueAsync()
        {
            var assetGroup = await CreateNewAssetGroupAsync().ConfigureAwait(false);
            Assert.IsNotNull(assetGroup);

            try
            {
                var client = TestSetup.PdmsClientInstance;
                var agResponse = await client.AssetGroups
                    .GetComplianceStateByAssetQualifierAsync(assetGroup.Qualifier, TestSetup.RequestContext)
                    .ConfigureAwait(false);
                Assert.AreEqual(agResponse.HttpStatusCode, HttpStatusCode.OK);
                Assert.IsTrue(agResponse.Response.IsCompliant);
            }
            finally
            {
                // Remove the asset group that we created.
                await RemoveAssetGroupAsync(assetGroup.Id);
            }
        }

        [TestMethod]
        public async Task WhenIDeleteAnAssetGroupItSucceedsAsync()
        {
            var assetGroup = await CreateNewAssetGroupAsync().ConfigureAwait(false);
            Assert.IsNotNull(assetGroup);

            var client = TestSetup.PdmsClientInstance;
            var agResponse = await client.AssetGroups.DeleteAsync(assetGroup.Id, assetGroup.ETag, TestSetup.RequestContext).ConfigureAwait(false);
            Assert.IsTrue(agResponse.HttpStatusCode == HttpStatusCode.NoContent);
        }

        [TestMethod]
        public async Task WhenICallApiToReadAllAssetGroupsUsingHeadMethodItFailsAsync()
        {
            var content = await GetApiCallResponseAsStringAsync("api/v2/AssetGroups", HttpStatusCode.BadRequest, HttpMethod.Head)
                .ConfigureAwait(false);
            Assert.IsTrue(string.IsNullOrEmpty(content));
        }
    }
}
