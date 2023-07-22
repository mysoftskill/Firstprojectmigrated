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
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using static Microsoft.PrivacyServices.DataManagement.Client.V2.ExpiredError;

    [TestClass]
    public class TransferRequestTests : TestBase
    {
        [TestMethod]
        public async Task CanCreateTransferRequestUsingClient()
        {
            var transferRequest = await CreateNewTransferRequestAsync().ConfigureAwait(false);

            await VerifyTransferRequestExists(transferRequest.Id).ConfigureAwait(false);

            await CleanupTransferRequest(transferRequest);
        }

        [TestMethod]
        public async Task WhenICallCreateTransferRequestsWithNullBodyItFailsWithBadArumentError()
        {
            await Assert.ThrowsExceptionAsync<BadArgumentError.InvalidArgument>(() => TestSetup.PdmsClientInstance.TransferRequests
                .CreateAsync(null, TestSetup.RequestContext)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CantCreateTransferRequestWithAssetGroupThatAHasPendingTransferRequest()
        {
            var transferRequest1 = await CreateNewTransferRequestAsync().ConfigureAwait(false);
            var dataOwner2 = await CreateDataOwnerWithoutServiceTreeAsync().ConfigureAwait(false);

            var transferRequest2 = new TransferRequest
            {
                AssetGroups = transferRequest1.AssetGroups,
                SourceOwnerId = transferRequest1.SourceOwnerId,
                TargetOwnerId = dataOwner2.Id
            };

            // Attempt to transfer the same asset group to another owner
            await Assert.ThrowsExceptionAsync<ConflictError.LinkedEntityExists>(() => TestSetup.PdmsClientInstance.TransferRequests
                .CreateAsync(transferRequest2, TestSetup.RequestContext)).ConfigureAwait(false);

            await CleanupDataOwner(dataOwner2);
            await CleanupTransferRequest(transferRequest1);
        }

        [TestMethod]
        public async Task CanReadTransferRequestsUsingClient()
        {
            var transferRequest = await CreateNewTransferRequestAsync().ConfigureAwait(false);

            var transferRequestsResponse = await TestSetup.PdmsClientInstance.TransferRequests.ReadAllByFiltersAsync(
                    TestSetup.RequestContext,
                    TransferRequestExpandOptions.None)
                .ConfigureAwait(false);

            Assert.IsTrue(transferRequestsResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {transferRequestsResponse.HttpStatusCode}");

            Assert.IsTrue(transferRequestsResponse.Response.Any());

            if (transferRequestsResponse.Response.Any(a => a.Id.Equals(transferRequest.Id, StringComparison.OrdinalIgnoreCase)))
            {
                await VerifyTransferRequestExists(transferRequest.Id).ConfigureAwait(false);
            }
            else
            {
                Assert.Fail("Newly created Transfer not retrieved");
            }

            await CleanupTransferRequest(transferRequest);
        }

        [TestMethod]
        public async Task CanApproveTransferRequestsUsingClient()
        {
            var transferRequest = await CreateNewTransferRequestAsync().ConfigureAwait(false);

            var assetGroupId = transferRequest.AssetGroups.ElementAt<string>(0);
            var sourceOwnerId = transferRequest.SourceOwnerId;
            var targetOwnerId = transferRequest.TargetOwnerId;

            var transferRequestsResponse = await TestSetup.PdmsClientInstance.TransferRequests.ApproveAsync(
                    transferRequest.Id,
                    transferRequest.ETag,
                    TestSetup.RequestContext)
                .ConfigureAwait(false);

            Assert.IsTrue(transferRequestsResponse.HttpStatusCode == HttpStatusCode.NoContent,
                $"StatusCode was {transferRequestsResponse.HttpStatusCode}");

            // Read source data owner and check HasInitiatedTransferRequests
            var content = await GetApiCallResponseAsStringAsync($"api/v2/DataOwners/{sourceOwnerId}").ConfigureAwait(false);
            Assert.IsNotNull(content);
            var sourceDataOwner = JsonConvert.DeserializeObject<DataOwner>(content);
            Assert.IsFalse(sourceDataOwner.HasInitiatedTransferRequests);

            // Read tartget data owner and check HasPendingTransferRequests
            content = await GetApiCallResponseAsStringAsync($"api/v2/DataOwners/{targetOwnerId}").ConfigureAwait(false);
            Assert.IsNotNull(content);
            var targetDataOwner = JsonConvert.DeserializeObject<DataOwner>(content);
            Assert.IsFalse(targetDataOwner.HasPendingTransferRequests);

            // Get the asset group that was transferred and check for correct flags and owners
            content = await GetApiCallResponseAsStringAsync($"api/v2/AssetGroups/{assetGroupId}").ConfigureAwait(false);
            Assert.IsNotNull(content);
            var assetGroup = JsonConvert.DeserializeObject<AssetGroup>(content);
            Assert.IsFalse(assetGroup.HasPendingTransferRequest);
            Assert.IsNull(assetGroup.PendingTransferRequestTargetOwnerId);
            Assert.AreEqual(targetOwnerId, assetGroup.OwnerId);

            // Verify that the transfer request has been deleted
            await GetApiCallResponseAsStringAsync(
                    $"api/v2/TransferRequests('{transferRequest.Id}')",
                    HttpStatusCode.NotFound)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CanReadAllTransferRequestsCallingApi()
        {
            var transferRequest = await CreateNewTransferRequestAsync().ConfigureAwait(false);

            var content = await GetApiCallResponseAsStringAsync(
                    "api/v2/TransferRequests?$select=id,eTag,sourceOwnerId,targetOwnerId,requestState,assetGroups")
                .ConfigureAwait(false);
            Assert.IsNotNull(content);

            var transferRequests = JsonConvert.DeserializeObject<ODataResponse<List<TransferRequest>>>(content);
            Assert.IsTrue(transferRequests.Value.Count > 0);

            // Query with trackingDetails
            content = await GetApiCallResponseAsStringAsync(
                    $"api/v2/TransferRequests?$select=id,eTag,sourceOwnerId,targetOwnerId,requestState,assetGroups,trackingDetails")
                .ConfigureAwait(false);
            Assert.IsNotNull(content);

            transferRequests = JsonConvert.DeserializeObject<ODataResponse<List<TransferRequest>>>(content);
            Assert.IsTrue(transferRequests.Value.Count > 0);

            await CleanupTransferRequest(transferRequest);
        }

        [TestMethod]
        public async Task CanQueryTransferRequestsCallingApi()
        {
            var transferRequest = await CreateNewTransferRequestAsync().ConfigureAwait(false);

            var content = await GetApiCallResponseAsStringAsync(
                    $"api/v2/TransferRequests?$select=id,eTag,sourceOwnerId,targetOwnerId,requestState,assetGroups&$filter=sourceOwnerId eq '{transferRequest.SourceOwnerId}'")
                .ConfigureAwait(false);
            Assert.IsNotNull(content);

            var transferRequests = JsonConvert.DeserializeObject<ODataResponse<List<TransferRequest>>>(content);
            Assert.IsTrue(transferRequests.Value.Count == 1);

            await CleanupTransferRequest(transferRequest);
        }

        [TestMethod]
        public async Task CanDeleteTransferRequestUsingClient()
        {
            var transferRequest = await CreateNewTransferRequestAsync().ConfigureAwait(false);

            var transferRequestDeleteResponse = await TestSetup.PdmsClientInstance.TransferRequests
                .DeleteAsync(transferRequest.Id, transferRequest.ETag, TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(transferRequestDeleteResponse.HttpStatusCode == HttpStatusCode.NoContent,
                $"StatusCode was {transferRequestDeleteResponse.HttpStatusCode}");

            await GetApiCallResponseAsStringAsync(
                    $"api/v2/TransferRequests('{transferRequest.Id}')",
                    HttpStatusCode.NotFound)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task When_ApproveAsyncWithETagMismatch_Then_Fail()
        {
            var transferRequest = await CreateNewTransferRequestAsync().ConfigureAwait(false);
            var originalETag = transferRequest.ETag;
            transferRequest.ETag = "\"123\"";

            var exn = await Assert.ThrowsExceptionAsync<ETagMismatch>(() => TestSetup.PdmsClientInstance.TransferRequests
                .ApproveAsync(transferRequest.Id, transferRequest.ETag, TestSetup.RequestContext)).ConfigureAwait(false);
            Assert.AreEqual(transferRequest.ETag, exn.Value);

            // Restore the ETag so that we can delete the request
            transferRequest.ETag = originalETag;
            await CleanupTransferRequest(transferRequest);
        }

        [TestMethod]
        public async Task CanQueryTransferRequestsByFilter()
        {
            // Create two transfer requests for a data owner
            var dataOwner = await CreateDataOwnerWithoutServiceTreeAsync().ConfigureAwait(false);
            var transferRequest1 = await CreateNewTransferRequestAsync(dataOwner.Id).ConfigureAwait(false);
            var transferRequest2 = await CreateNewTransferRequestAsync(dataOwner.Id).ConfigureAwait(false);

            // Search for transferRequest1 and transferRequest2 by owner
            var transferRequests = await FindAllTransferRequestsByFilter($"targetOwnerId eq '{dataOwner.Id}'").ConfigureAwait(false);

            // Should find at least 2 variant definitions
            Assert.IsTrue(transferRequests.Count >= 2);

            // check for transfer request 1
            if (transferRequests.Any(a => a.Id.Equals(transferRequest1.Id, StringComparison.OrdinalIgnoreCase)))
            {
                await VerifyTransferRequestExists(transferRequest1.Id).ConfigureAwait(false);
            }
            else
            {
                Assert.Fail("Transfer request 1 was not retrieved by owner id");
            }

            // check for transfer request 2
            if (transferRequests.Any(a => a.Id.Equals(transferRequest2.Id, StringComparison.OrdinalIgnoreCase)))
            {
                await VerifyTransferRequestExists(transferRequest2.Id).ConfigureAwait(false);
            }
            else
            {
                Assert.Fail("Transfer request 2 was not retrieved by owner id");
            }

            await CleanupTransferRequest(transferRequest1);
            await CleanupTransferRequest(transferRequest2);
        }

        [TestMethod]
        public async Task WhenICallApiToReadTransferRequestUsingHeadMethodItFailsAsync()
        {
            var content = await GetApiCallResponseAsStringAsync($"api/v2/TransferRequests",
                HttpStatusCode.BadRequest,
                HttpMethod.Head)
                .ConfigureAwait(false);
            Assert.IsTrue(string.IsNullOrEmpty(content));
        }

        private static async Task VerifyTransferRequestExists(string transferRequestId)
        {
            var transferRequestResponse = await TestSetup.PdmsClientInstance.TransferRequests
                .ReadAsync(transferRequestId, TestSetup.RequestContext).ConfigureAwait(false);

            Assert.IsTrue(transferRequestResponse.HttpStatusCode == HttpStatusCode.OK,
                $"StatusCode was {transferRequestResponse.HttpStatusCode}");
            Assert.AreEqual(transferRequestId, transferRequestResponse.Response.Id);
        }

        private static async Task<List<TransferRequest>> FindAllTransferRequestsByFilter(string filter)
        {
            // Query all transfers via the API
            int countReturned;
            int maxQueryResult = 1000;
            int skip = 0;
            var totalCount = 0;
            List<TransferRequest> allTransferDefs = new List<TransferRequest>();
            do
            {
                countReturned = 0;
                var content = await GetApiCallResponseAsStringAsync(
                        $"api/v2/TransferRequests?$select=id,eTag,sourceOwnerId,targetOwnerId,requestState,assetGroups&$filter={filter}&$top={maxQueryResult}&$skip={skip}")
                    .ConfigureAwait(false);

                if (content != null)
                {
                    var response = JsonConvert.DeserializeObject<ODataResponse<List<TransferRequest>>>(content);
                    var transferRequests = response.Value;

                    if (transferRequests.Count > 0)
                    {
                        allTransferDefs.AddRange(transferRequests);
                    };

                    countReturned = transferRequests.Count;
                    skip += maxQueryResult;
                }

                totalCount += countReturned;

            } while (countReturned > 0);

            return allTransferDefs;
        }
    }
}
