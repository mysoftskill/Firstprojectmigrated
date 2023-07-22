namespace Microsoft.PrivacyServices.DataManagement.Client.V2.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.DataManagement.Client.Filters;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class DataManagementClientTest
    {
        #region Data Owners
        [Theory(DisplayName = "Verify DataOwners.CreateAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyCreateDataOwnerAsync(
            DataOwner dataOwner,
            IHttpResult<DataOwner> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/dataOwners";

            proxyMock
                .Setup(m => m.PostAsync<DataOwner, DataOwner>(url, dataOwner, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.DataOwners.CreateAsync(dataOwner, requestContext).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify DataOwners.ReadAsync calls the correct url.")]
        [InlineAutoMoqData("", DataOwnerExpandOptions.None, DisableRecursionCheck = true)]
        ////[InlineAutoMoqData(",dataAgents&$expand=dataAgents", DataOwnerExpandOptions.DataAgents, DisableRecursionCheck = true)]
        ////[InlineAutoMoqData(",assetGroups&$expand=assetGroups", DataOwnerExpandOptions.AssetGroups, DisableRecursionCheck = true)]
        [InlineAutoMoqData(",trackingDetails", DataOwnerExpandOptions.TrackingDetails, DisableRecursionCheck = true)]
        [InlineAutoMoqData(",serviceTree", DataOwnerExpandOptions.ServiceTree, DisableRecursionCheck = true)]
        [InlineAutoMoqData(",trackingDetails,serviceTree", DataOwnerExpandOptions.TrackingDetails | DataOwnerExpandOptions.ServiceTree, DisableRecursionCheck = true)]
        ////[InlineAutoMoqData(",trackingDetails,dataAgents&$expand=dataAgents", DataOwnerExpandOptions.DataAgents | DataOwnerExpandOptions.TrackingDetails, DisableRecursionCheck = true)]
        ////[InlineAutoMoqData(",trackingDetails,assetGroups&$expand=assetGroups", DataOwnerExpandOptions.AssetGroups | DataOwnerExpandOptions.TrackingDetails, DisableRecursionCheck = true)]
        ////[InlineAutoMoqData(",trackingDetails,assetGroups,dataAgents&$expand=assetGroups,dataAgents", DataOwnerExpandOptions.AssetGroups | DataOwnerExpandOptions.DataAgents | DataOwnerExpandOptions.TrackingDetails, DisableRecursionCheck = true)]
        ////[InlineAutoMoqData(",assetGroups,dataAgents&$expand=assetGroups,dataAgents", DataOwnerExpandOptions.AssetGroups | DataOwnerExpandOptions.DataAgents, DisableRecursionCheck = true)]
        public async Task VerifyReadDataOwnerAsync(
            string queryString,
            DataOwnerExpandOptions expandOptions,
            DataOwner dataOwner,
            IHttpResult<DataOwner> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/dataOwners('{dataOwner.Id}')?$select=id,eTag,name,description,alertContacts,announcementContacts,sharingRequestContacts,writeSecurityGroups,tagSecurityGroups,tagApplicationIds,icm,hasInitiatedTransferRequests,hasPendingTransferRequests" + queryString;

            proxyMock
                .Setup(m => m.GetAsync<DataOwner>(Is.Value<string>(s => Assert.Equal(url, s)), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.DataOwners.ReadAsync(dataOwner.Id, requestContext, expandOptions).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify DataOwners.ReadByFiltersAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyReadDataOwnerByFiltersAsync(
            IHttpResult<Collection<DataOwner>> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/dataOwners?$select=id,eTag,name,description,alertContacts,announcementContacts,sharingRequestContacts,writeSecurityGroups,tagSecurityGroups,tagApplicationIds,icm,hasInitiatedTransferRequests,hasPendingTransferRequests";

            proxyMock
                .Setup(m => m.GetAsync<Collection<DataOwner>>(Is.Value<string>(s => Assert.Equal(url, s)), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.DataOwners.ReadByFiltersAsync(requestContext).ConfigureAwait(false);

            Assert.True(actualResult.Response.Value.SequenceEqual(result.Response.Value));
        }

        [Theory(DisplayName = "Verify DataOwners.ReadByFiltersAsync method works correctly with filter criteria."), AutoMoqData(true)]
        public async Task VerifyReadDataOwnersAsyncMethodWithFilterCriteria(
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            DataManagementClient client,
            RequestContext request,
            HttpResult<Collection<DataOwner>> result)
        {
            DataOwnerFilterCriteria filterCriteria = new DataOwnerFilterCriteria { Name = new StringFilter("test", StringComparisonType.Contains) };

            string url = $"/api/v2/dataOwners?$select=id,eTag,name,description,alertContacts,announcementContacts,sharingRequestContacts,writeSecurityGroups,tagSecurityGroups,tagApplicationIds,icm,hasInitiatedTransferRequests,hasPendingTransferRequests&$filter=contains(name,'test')";

            proxyMock
                .Setup(m => m.GetAsync<Collection<DataOwner>>(url, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IHttpResult<Collection<DataOwner>>>(result));

            IHttpResult<Collection<DataOwner>> actualResult = await client.DataOwners.ReadByFiltersAsync(
                request,
                DataOwnerExpandOptions.None,
                filterCriteria).ConfigureAwait(false);

            Assert.True(actualResult.Response.Value.SequenceEqual((IEnumerable<DataOwner>)result.Response.Value));
        }

        [Theory(DisplayName = "Verify DataOwners.ReadAllByFiltersAsync method works correctly with filter criteria."), AutoMoqData(true)]
        public async Task VerifyReadAllDataOwnersAsyncMethodWithFilterCriteria(
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            DataManagementClient client,
            RequestContext request,
            HttpResult<Collection<DataOwner>> result)
        {
            result.Response.NextLink = null;

            DataOwnerFilterCriteria filterCriteria = new DataOwnerFilterCriteria { Name = new StringFilter("test", StringComparisonType.Equals) };

            string url = $"/api/v2/dataOwners?$select=id,eTag,name,description,alertContacts,announcementContacts,sharingRequestContacts,writeSecurityGroups,tagSecurityGroups,tagApplicationIds,icm,hasInitiatedTransferRequests,hasPendingTransferRequests&$filter=name eq 'test'";

            proxyMock
                .Setup(m => m.GetAsync<Collection<DataOwner>>(url, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IHttpResult<Collection<DataOwner>>>(result));

            IHttpResult<IEnumerable<DataOwner>> actualResult = await client.DataOwners.ReadAllByFiltersAsync(
                request,
                DataOwnerExpandOptions.None,
                filterCriteria).ConfigureAwait(false);

            Assert.True(actualResult.Response.SequenceEqual((IEnumerable<DataOwner>)result.Response.Value));
        }

        [Theory(DisplayName = "Verify DataOwners.UpdateAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyUpdateDataOwnerAsync(
            DataOwner dataOwner,
            IHttpResult<DataOwner> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/dataOwners('{dataOwner.Id}')";

            proxyMock
                .Setup(m => m.PutAsync<DataOwner, DataOwner>(url, dataOwner, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.DataOwners.UpdateAsync(dataOwner, requestContext).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify DataOwners.ReplaceServiceIdAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyReplaceServiceIdDataOwnerAsync(
            DataOwner dataOwner,
            IHttpResult<DataOwner> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/dataOwners('{dataOwner.Id}')/v2.replaceServiceId";

            Action<object> verify = o =>
            {
                var value = ((dynamic)o).value as DataOwner;
                Assert.Equal(dataOwner, value);
            };

            proxyMock
                .Setup(m => m.PostAsync<object, DataOwner>(url, Is.Value(verify), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.DataOwners.ReplaceServiceIdAsync(dataOwner, requestContext).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify DataOwners.FindAllByAuthenticatedUserAsync method works correctly."), AutoMoqData(true)]
        public async Task VerifyFindAllByAuthenticatedUserAsyncMethodWithFilterCriteria(
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            DataManagementClient client,
            RequestContext request,
            HttpResult<Collection<DataOwner>> result)
        {
            result.Response.NextLink = null;

            string url = $"/api/v2/dataOwners/v2.findByAuthenticatedUser?$select=id,eTag,name,description,alertContacts,announcementContacts,sharingRequestContacts,writeSecurityGroups,tagSecurityGroups,tagApplicationIds,icm,hasInitiatedTransferRequests,hasPendingTransferRequests";

            proxyMock
                .Setup(m => m.GetAsync<Collection<DataOwner>>(Is.Value<string>(x => Assert.Equal(url, x)), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IHttpResult<Collection<DataOwner>>>(result));

            var actualResult = await client.DataOwners.FindAllByAuthenticatedUserAsync(request, DataOwnerExpandOptions.None).ConfigureAwait(false);

            Assert.True(actualResult.Response.SequenceEqual(result.Response.Value));
        }

        [Theory(DisplayName = "Verify DataOwners.DeleteAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyDeleteDataOwnerAsync(
            DataOwner dataOwner,
            IHttpResult result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/dataOwners('{dataOwner.Id}')";

            Action<IDictionary<string, Func<Task<string>>>> verify = async x =>
            {
                Assert.True(x.ContainsKey("If-Match"));
                Assert.Equal(dataOwner.ETag, await x["If-Match"]().ConfigureAwait(false));
            };

            proxyMock
                .Setup(m => m.DeleteAsync(url, Is.Value(verify), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.DataOwners.DeleteAsync(dataOwner.Id, dataOwner.ETag, requestContext).ConfigureAwait(false);

            Assert.Equal(result.HttpStatusCode, actualResult.HttpStatusCode);
        }
        #endregion

        #region Asset Groups
        /* NOTE: This test has historically had some random failures with AzureDocumentDB asset, when those are picked by AutoMoq.*/
        [Theory(DisplayName = "Verify AssetGroups.GetComplianceStateByAssetQualifier calls the correct url."), AutoMoqData]
        public async Task VerifyAssetGroupsGetComplianceStateByAssetQualifierAsync(
            AssetQualifier assetQualifier,
            IHttpResult<ComplianceState> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/assetGroups/v2.findByAssetQualifier(qualifier=@value)/complianceState?@value='{assetQualifier.Value}'";

            proxyMock
                .Setup(m => m.GetAsync<ComplianceState>(Is.Value<string>(s => Assert.Equal(url, WebUtility.UrlDecode(s))), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.AssetGroups.GetComplianceStateByAssetQualifierAsync(assetQualifier, requestContext).ConfigureAwait(false);

            Assert.Equal(result.Response.IsCompliant, actualResult.Response.IsCompliant);
            Assert.Equal(result.Response.IncompliantReason, actualResult.Response.IncompliantReason);
        }

        [Theory(DisplayName = "Verify AssetGroups.GetComplianceStateByAssetQualifier escapes the query value.")]
        [InlineAutoMoqData("Test%Test", "Test%25Test")]
        [InlineAutoMoqData("Test#Test", "Test%23Test")]
        [InlineAutoMoqData("Test+Test", "Test%2BTest")]
        [InlineAutoMoqData("Test/Test", "Test%2FTest")]
        [InlineAutoMoqData("Test?Test", "Test%3FTest")]
        [InlineAutoMoqData("Test&Test", "Test%26Test")]
        public async Task VerifyAssetGroupsGetComplianceStateByAssetQualifierEscapeValueAsync(
            string initialValue,
            string expectedValue,
            IHttpResult<ComplianceState> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            var assetQualifier = AssetQualifier.CreateForApacheCassandra(initialValue);
            
            proxyMock
                .Setup(m => m.GetAsync<ComplianceState>(Is.Value<string>(s => Assert.EndsWith(expectedValue + "'", s)), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.AssetGroups.GetComplianceStateByAssetQualifierAsync(assetQualifier, requestContext).ConfigureAwait(false);

            Assert.Equal(result.Response.IsCompliant, actualResult.Response.IsCompliant);
            Assert.Equal(result.Response.IncompliantReason, actualResult.Response.IncompliantReason);
        }

        [Theory(DisplayName = "Verify AssetGroups.CreateAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyCreateAssetGroupAsync(
            AssetGroup assetGroup,
            IHttpResult<AssetGroup> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/assetGroups";

            proxyMock
                .Setup(m => m.PostAsync<AssetGroup, AssetGroup>(url, assetGroup, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.AssetGroups.CreateAsync(assetGroup, requestContext).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify AssetGroups.SetAgentRelationshipsAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifySetAgentRelationshipsAsync(
            SetAgentRelationshipParameters parameters,
            IHttpResult<SetAgentRelationshipResponse> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/assetGroups/v2.setAgentRelationships";

            proxyMock
                .Setup(m => m.PostAsync<SetAgentRelationshipParameters, SetAgentRelationshipResponse>(url, parameters, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.AssetGroups.SetAgentRelationshipsAsync(parameters, requestContext).ConfigureAwait(false);

            Assert.Equal(result.Response.Results.First().AssetGroupId, actualResult.Response.Results.First().AssetGroupId);
        }

        [Theory(DisplayName = "Verify AssetGroups.ReadAsync calls the correct url.")]
        [InlineAutoMoqData("", AssetGroupExpandOptions.None, DisableRecursionCheck = true)]
        [InlineAutoMoqData(",trackingDetails", AssetGroupExpandOptions.TrackingDetails, DisableRecursionCheck = true)]
        public async Task VerifyReadAssetGroupAsync(
            string queryString,
            AssetGroupExpandOptions expandOptions,
            AssetGroup assetGroup,
            IHttpResult<AssetGroup> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/assetGroups('{assetGroup.Id}')?$select=id,eTag,qualifier,variants,complianceState,deleteAgentId,exportAgentId,accountCloseAgentId,inventoryId,isRealTimeStore,isVariantsInheritanceBlocked,isDeleteAgentInheritanceBlocked,isExportAgentInheritanceBlocked,hasPendingVariantRequests,optionalFeatures,ownerId,deleteSharingRequestId,exportSharingRequestId,hasPendingTransferRequest,pendingTransferRequestTargetOwnerId,pendingTransferRequestTargetOwnerName" + queryString;

            proxyMock
                .Setup(m => m.GetAsync<AssetGroup>(Is.Value<string>(s => Assert.Equal(url, s)), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.AssetGroups.ReadAsync(assetGroup.Id, requestContext, expandOptions).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify AssetGroups.ReadByFiltersAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyReadAssetGroupByFiltersAsync(
            IHttpResult<Collection<AssetGroup>> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/assetGroups?$select=id,eTag,qualifier,variants,complianceState,deleteAgentId,exportAgentId,accountCloseAgentId,inventoryId,isRealTimeStore,isVariantsInheritanceBlocked,isDeleteAgentInheritanceBlocked,isExportAgentInheritanceBlocked,hasPendingVariantRequests,optionalFeatures,ownerId,deleteSharingRequestId,exportSharingRequestId,hasPendingTransferRequest,pendingTransferRequestTargetOwnerId,pendingTransferRequestTargetOwnerName";

            proxyMock
                .Setup(m => m.GetAsync<Collection<AssetGroup>>(Is.Value<string>(s => Assert.Equal(url, s)), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.AssetGroups.ReadByFiltersAsync(requestContext).ConfigureAwait(false);

            Assert.True(actualResult.Response.Value.SequenceEqual(result.Response.Value));
        }

        [Theory(DisplayName = "Verify AssetGroups.ReadAllByFiltersAsync method calls the correct url."), AutoMoqData(true)]
        public async Task VerifyReadAllAssetGroupsByFiltersAsync(
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            DataManagementClient client,
            RequestContext request,
            HttpResult<Collection<AssetGroup>> result)
        {
            result.Response.NextLink = null;

            string url = $"/api/v2/assetGroups?$select=id,eTag,qualifier,variants,complianceState,deleteAgentId,exportAgentId,accountCloseAgentId,inventoryId,isRealTimeStore,isVariantsInheritanceBlocked,isDeleteAgentInheritanceBlocked,isExportAgentInheritanceBlocked,hasPendingVariantRequests,optionalFeatures,ownerId,deleteSharingRequestId,exportSharingRequestId,hasPendingTransferRequest,pendingTransferRequestTargetOwnerId,pendingTransferRequestTargetOwnerName";

            proxyMock
                .Setup(m => m.GetAsync<Collection<AssetGroup>>(url, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IHttpResult<Collection<AssetGroup>>>(result));

            IHttpResult<IEnumerable<AssetGroup>> actualResult = await client.AssetGroups.ReadAllByFiltersAsync(request).ConfigureAwait(false);

            Assert.True(actualResult.Response.SequenceEqual((IEnumerable<AssetGroup>)result.Response.Value));
        }

        [Theory(DisplayName = "Verify AssetGroups.UpdateAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyUpdateAssetGroupAsync(
            AssetGroup assetGroup,
            IHttpResult<AssetGroup> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/assetGroups('{assetGroup.Id}')";

            proxyMock
                .Setup(m => m.PutAsync<AssetGroup, AssetGroup>(url, assetGroup, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.AssetGroups.UpdateAsync(assetGroup, requestContext).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify AssetGroups.DeleteAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyDeleteAssetGroupAsync(
            AssetGroup assetGroup,
            IHttpResult result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/assetGroups('{assetGroup.Id}')";

            Action<IDictionary<string, Func<Task<string>>>> verify = async x =>
            {
                Assert.True(x.ContainsKey("If-Match"));
                Assert.Equal(assetGroup.ETag, await x["If-Match"]().ConfigureAwait(false));
            };

            proxyMock
                .Setup(m => m.DeleteAsync(url, Is.Value(verify), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.AssetGroups.DeleteAsync(assetGroup.Id, assetGroup.ETag, requestContext).ConfigureAwait(false);

            Assert.Equal(result.HttpStatusCode, actualResult.HttpStatusCode);
        }

        [Theory(DisplayName = "Verify AssetGroups.RemoveVariants calls the correct url."), AutoMoqData(true)]
        public async Task VerifyRemoveVariantsAsync(
            string id,
            string[] variantIds,
            string etag,
            IHttpResult<AssetGroup> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/assetGroups('{id}')/v2.removeVariants";

            Action<IDictionary<string, Func<Task<string>>>> verify = async x =>
            {
                Assert.True(x.ContainsKey("If-Match"));
                Assert.Equal(etag, await x["If-Match"]().ConfigureAwait(false));
            };

            proxyMock
                .Setup(m => m.PostAsync<RemoveVariantsParameters, AssetGroup>(
                    url, 
                    It.IsAny<RemoveVariantsParameters>(),
                    Is.Value(verify), 
                    It.IsAny<CancellationToken>())).ReturnsAsync(result);

            var actualResult = await client.AssetGroups.RemoveVariantsAsync(id, variantIds, etag, requestContext).ConfigureAwait(false);

            Assert.Equal(result.HttpStatusCode, actualResult.HttpStatusCode);
        }
        #endregion

        #region Variant Definitions
        [Theory(DisplayName = "Verify VariantDefinitions.CreateAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyCreateVariantDefinitionAsync(
            VariantDefinition variantDefinition,
            IHttpResult<VariantDefinition> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/variantDefinitions";

            proxyMock
                .Setup(m => m.PostAsync<VariantDefinition, VariantDefinition>(url, variantDefinition, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.VariantDefinitions.CreateAsync(variantDefinition, requestContext).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify VariantDefinitions.ReadAsync calls the correct url.")]
        [InlineAutoMoqData("", VariantDefinitionExpandOptions.None, DisableRecursionCheck = true)]
        [InlineAutoMoqData(",trackingDetails", VariantDefinitionExpandOptions.TrackingDetails, DisableRecursionCheck = true)]
        public async Task VerifyReadVariantDefinitionAsync(
            string queryString,
            VariantDefinitionExpandOptions expandOptions,
            VariantDefinition variantDefinition,
            IHttpResult<VariantDefinition> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/variantDefinitions('{variantDefinition.Id}')?$select=id,eTag,name,description,egrcId,egrcName,dataTypes,capabilities,subjectTypes,approver,ownerId,state,reason" + queryString;

            proxyMock
                .Setup(m => m.GetAsync<VariantDefinition>(Is.Value<string>(s => Assert.Equal(url, s)), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.VariantDefinitions.ReadAsync(variantDefinition.Id, requestContext, expandOptions).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify VariantDefinitions.ReadByFiltersAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyReadVariantDefinitionByFiltersAsync(
            IHttpResult<Collection<VariantDefinition>> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/variantDefinitions?$select=id,eTag,name,description,egrcId,egrcName,dataTypes,capabilities,subjectTypes,approver,ownerId,state,reason";

            proxyMock
                .Setup(m => m.GetAsync<Collection<VariantDefinition>>(Is.Value<string>(s => Assert.Equal(url, s)), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.VariantDefinitions.ReadByFiltersAsync(requestContext).ConfigureAwait(false);

            Assert.True(actualResult.Response.Value.SequenceEqual(result.Response.Value));
        }

        [Theory(DisplayName = "Verify VariantDefinitions.ReadByFiltersAsync method works correctly with filter criteria."), AutoMoqData(true)]
        public async Task VerifyReadVariantDefinitionsAsyncMethodWithFilterCriteria(
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            DataManagementClient client,
            RequestContext request,
            HttpResult<Collection<VariantDefinition>> result)
        {
            VariantDefinitionFilterCriteria filterCriteria = new VariantDefinitionFilterCriteria { Name = new StringFilter("test", StringComparisonType.Contains) };

            string url = $"/api/v2/variantDefinitions?$select=id,eTag,name,description,egrcId,egrcName,dataTypes,capabilities,subjectTypes,approver,ownerId,state,reason&$filter=contains(name,'test')";

            proxyMock
                .Setup(m => m.GetAsync<Collection<VariantDefinition>>(url, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IHttpResult<Collection<VariantDefinition>>>(result));

            IHttpResult<Collection<VariantDefinition>> actualResult = await client.VariantDefinitions.ReadByFiltersAsync(
                request,
                VariantDefinitionExpandOptions.None,
                filterCriteria).ConfigureAwait(false);

            Assert.True(actualResult.Response.Value.SequenceEqual((IEnumerable<VariantDefinition>)result.Response.Value));
        }

        [Theory(DisplayName = "Verify VariantDefinitions.ReadAllByFiltersAsync method works correctly with filter criteria."), AutoMoqData(true)]
        public async Task VerifyReadAllVariantDefinitionsAsyncMethodWithFilterCriteria(
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            DataManagementClient client,
            RequestContext request,
            HttpResult<Collection<VariantDefinition>> result)
        {
            result.Response.NextLink = null;

            VariantDefinitionFilterCriteria filterCriteria = new VariantDefinitionFilterCriteria { Name = new StringFilter("test", StringComparisonType.Equals) };

            string url = $"/api/v2/variantDefinitions?$select=id,eTag,name,description,egrcId,egrcName,dataTypes,capabilities,subjectTypes,approver,ownerId,state,reason&$filter=name eq 'test'";

            proxyMock
                .Setup(m => m.GetAsync<Collection<VariantDefinition>>(url, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IHttpResult<Collection<VariantDefinition>>>(result));

            IHttpResult<IEnumerable<VariantDefinition>> actualResult = await client.VariantDefinitions.ReadAllByFiltersAsync(
                request,
                VariantDefinitionExpandOptions.None,
                filterCriteria).ConfigureAwait(false);

            Assert.True(actualResult.Response.SequenceEqual((IEnumerable<VariantDefinition>)result.Response.Value));
        }

        [Theory(DisplayName = "Verify VariantDefinitions.UpdateAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyUpdateVariantDefinitionAsync(
            VariantDefinition variantDefinition,
            IHttpResult<VariantDefinition> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/variantDefinitions('{variantDefinition.Id}')";

            proxyMock
                .Setup(m => m.PutAsync<VariantDefinition, VariantDefinition>(url, variantDefinition, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.VariantDefinitions.UpdateAsync(variantDefinition, requestContext).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify VariantDefinitions.DeleteAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyDeleteVariantDefinitionAsync(
            VariantDefinition variantDefinition,
            IHttpResult<VariantDefinition> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/variantDefinitions('{variantDefinition.Id}')";

            Action<IDictionary<string, Func<Task<string>>>> verify = async x =>
            {
                Assert.True(x.ContainsKey("If-Match"));
                Assert.Equal(variantDefinition.ETag, await x["If-Match"]().ConfigureAwait(false));
            };

            proxyMock
                .Setup(m => m.DeleteAsync(url, Is.Value(verify), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.VariantDefinitions.DeleteAsync(variantDefinition.Id, variantDefinition.ETag, requestContext).ConfigureAwait(false);

            Assert.Equal(result.HttpStatusCode, actualResult.HttpStatusCode);
        }

        [Theory(DisplayName = "Verify VariantDefinitions.DeleteAsync with force flag calls the correct url."), AutoMoqData(true)]
        public async Task VerifyForceDeleteVariantDefinitionAsync(
            VariantDefinition variantDefinition,
            IHttpResult<VariantDefinition> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/variantDefinitions('{variantDefinition.Id}')/force";

            Action<IDictionary<string, Func<Task<string>>>> verify = async x =>
            {
                Assert.True(x.ContainsKey("If-Match"));
                Assert.Equal(variantDefinition.ETag, await x["If-Match"]().ConfigureAwait(false));
            };

            proxyMock
                .Setup(m => m.DeleteAsync(url, Is.Value(verify), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.VariantDefinitions.DeleteAsync(variantDefinition.Id, variantDefinition.ETag, requestContext, true).ConfigureAwait(false);

            Assert.Equal(result.HttpStatusCode, actualResult.HttpStatusCode);
        }

        [Theory(DisplayName = "Verify VariantDefinitions.DeleteAsync throws if etag is missing."), AutoMoqData(true)]
        public async Task VerifyDeleteVariantDefinitionWithoutETagAsync(
            VariantDefinition variantDefinition,
            RequestContext requestContext,
            DataManagementClient client)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => client.VariantDefinitions.DeleteAsync(variantDefinition.Id, null, requestContext, false)).ConfigureAwait(false);
        }
        #endregion

        #region Data Agents
        [Theory(DisplayName = "Verify DataAgents.CreateAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyCreateDataAgentAsync(
            DataAgent dataAgent,
            IHttpResult<DataAgent> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/dataAgents";

            proxyMock
                .Setup(m => m.PostAsync<DataAgent, DataAgent>(url, dataAgent, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.DataAgents.CreateAsync(dataAgent, requestContext).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify DataAgents.ReadAsync calls the correct url.")]
        [InlineAutoMoqData("", DataAgentExpandOptions.None, DisableRecursionCheck = true)]
        [InlineAutoMoqData("", DataAgentExpandOptions.TrackingDetails, DisableRecursionCheck = true)]
        public async Task VerifyReadDataAgentAsync(
            string queryString,
            DataAgentExpandOptions expandOptions,
            DataAgent dataAgent,
            IHttpResult<DataAgent> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/dataAgents('{dataAgent.Id}')?$select=*" + queryString;

            proxyMock
                .Setup(m => m.GetAsync<DataAgent>(Is.Value<string>(s => Assert.Equal(url, s)), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.DataAgents.ReadAsync<DataAgent>(dataAgent.Id, requestContext, expandOptions).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify DeleteAgents.ReadAsync calls the correct url.")]
        [InlineAutoMoqData("", DataAgentExpandOptions.None, DisableRecursionCheck = true)]
        [InlineAutoMoqData(",trackingDetails", DataAgentExpandOptions.TrackingDetails, DisableRecursionCheck = true)]
        [InlineAutoMoqData(",hasSharingRequests", DataAgentExpandOptions.HasSharingRequests, DisableRecursionCheck = true)]
        [InlineAutoMoqData(",trackingDetails,hasSharingRequests", DataAgentExpandOptions.TrackingDetails | DataAgentExpandOptions.HasSharingRequests, DisableRecursionCheck = true)]
        public async Task VerifyReadDeleteAgentAsync(
            string queryString,
            DataAgentExpandOptions expandOptions,
            DeleteAgent deleteAgent,
            IHttpResult<DeleteAgent> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/dataAgents('{deleteAgent.Id}')/v2.DeleteAgent?$select=id,eTag,name,description,connectionDetails,migratingConnectionDetails,ownerId,capabilities,operationalReadinessLow,operationalReadinessHigh,icm,inProdDate,sharingEnabled,isThirdPartyAgent,deploymentLocation,supportedClouds,dataResidencyBoundary" + queryString;

            proxyMock
                .Setup(m => m.GetAsync<DeleteAgent>(Is.Value<string>(s => Assert.Equal(url, s)), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.DataAgents.ReadAsync<DeleteAgent>(deleteAgent.Id, requestContext, expandOptions).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify DeleteAgents.CalculateDeleteAgentRegistrationStatus calls the correct url."), AutoMoqData(true)]
        public async Task VerifyCalculateDeleteAgentRegistrationStatusAsync(
            DeleteAgent deleteAgent,
            IHttpResult<AgentRegistrationStatus> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/dataAgents('{deleteAgent.Id}')/v2.DeleteAgent/v2.calculateRegistrationStatus";

            proxyMock
                .Setup(m => m.GetAsync<AgentRegistrationStatus>(Is.Value<string>(s => Assert.Equal(url, s)), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.DataAgents.CalculateDeleteAgentRegistrationStatus(deleteAgent.Id, requestContext).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify DataAgents.ReadByFiltersAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyReadDataAgentsByFiltersAsync(
            IHttpResult<Collection<DataAgent>> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/dataAgents?$select=*";

            proxyMock
                .Setup(m => m.GetAsync<Collection<DataAgent>>(Is.Value<string>(s => Assert.Equal(url, s)), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.DataAgents.ReadByFiltersAsync<DataAgent>(requestContext).ConfigureAwait(false);

            this.CompareDataAgents(actualResult.Response.Value, result.Response.Value);
        }

        [Theory(DisplayName = "Verify DataAgents.ReadByFiltersAsync<DeleteAgent> calls the correct url."), AutoMoqData(true)]
        public async Task VerifyReadDataAgentsByFiltersAsync_AsDeleteAgentType(
            IHttpResult<Collection<DeleteAgent>> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/dataAgents/v2.DeleteAgent?$select=id,eTag,name,description,connectionDetails,migratingConnectionDetails,ownerId,capabilities,operationalReadinessLow,operationalReadinessHigh,icm,inProdDate,sharingEnabled,isThirdPartyAgent,deploymentLocation,supportedClouds,dataResidencyBoundary";

            proxyMock
                .Setup(m => m.GetAsync<Collection<DeleteAgent>>(Is.Value<string>(s => Assert.Equal(url, s)), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.DataAgents.ReadByFiltersAsync<DeleteAgent>(requestContext).ConfigureAwait(false);

            Assert.True(actualResult.Response.Value.SequenceEqual((IEnumerable<DeleteAgent>)result.Response.Value));
        }

        [Theory(DisplayName = "Verify DataAgents.ReadByFiltersAsync method works correctly with filter criteria."), AutoMoqData(true)]
        public async Task VerifyReadDataAgentsAsyncMethodWithFilterCriteria(
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            DataManagementClient client,
            RequestContext request,
            DataAgentFilterCriteria filterCriteria,
            HttpResult<Collection<DataAgent>> result)
        {
            string url = $"/api/v2/dataAgents?$select=*&{filterCriteria.BuildRequestString()}";

            proxyMock
                .Setup(m => m.GetAsync<Collection<DataAgent>>(url, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IHttpResult<Collection<DataAgent>>>(result));

            IHttpResult<Collection<DataAgent>> actualResult = await client.DataAgents.ReadByFiltersAsync(
                request,
                DataAgentExpandOptions.None,
                filterCriteria).ConfigureAwait(false);

            this.CompareDataAgents(actualResult.Response.Value, result.Response.Value);
        }

        [Theory(DisplayName = "Verify DataAgents.ReadAllByFiltersAsync method works correctly with filter criteria."), AutoMoqData(true)]
        public async Task VerifyReadAllDataAgentsAsyncMethodWithFilterCriteria(
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            DataManagementClient client,
            RequestContext request,
            DataAgentFilterCriteria filterCriteria,
            HttpResult<Collection<DataAgent>> result)
        {
            result.Response.NextLink = null;

            string url = $"/api/v2/dataAgents?$select=*&{filterCriteria.BuildRequestString()}";

            proxyMock
                .Setup(m => m.GetAsync<Collection<DataAgent>>(url, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IHttpResult<Collection<DataAgent>>>(result));

            IHttpResult<IEnumerable<DataAgent>> actualResult = await client.DataAgents.ReadAllByFiltersAsync(
                request,
                DataAgentExpandOptions.None,
                filterCriteria).ConfigureAwait(false);

            this.CompareDataAgents(actualResult.Response, result.Response.Value);
        }

        [Theory(DisplayName = "Verify DataAgents.UpdateAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyUpdateDataAgentAsync(
            DataAgent dataAgent,
            IHttpResult<DataAgent> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/dataAgents('{dataAgent.Id}')";

            proxyMock
                .Setup(m => m.PutAsync<DataAgent, DataAgent>(url, dataAgent, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.DataAgents.UpdateAsync(dataAgent, requestContext).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify DeleteAgents.UpdateAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyUpdateDeleteAgentAsync(
            DeleteAgent deleteAgent,
            IHttpResult<DeleteAgent> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/dataAgents('{deleteAgent.Id}')";

            proxyMock
                .Setup(m => m.PutAsync<DeleteAgent, DeleteAgent>(url, deleteAgent, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.DataAgents.UpdateAsync(deleteAgent, requestContext).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify DataAgents.DeleteAsync calls the correct url.")]
        [InlineAutoMoqData(true, DisableRecursionCheck = true)]
        [InlineAutoMoqData(false, DisableRecursionCheck = true)]
        public async Task VerifyDeleteDataAgentAsync(
            bool overridePendingCommandsCheck,
            DataAgent dataAgent,
            IHttpResult result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url;
            if (overridePendingCommandsCheck)
            {
                url = $"/api/v2/dataAgents('{dataAgent.Id}')/v2.override";
            }
            else
            {
                url = $"/api/v2/dataAgents('{dataAgent.Id}')";
            }

            Action<IDictionary<string, Func<Task<string>>>> verify = async x =>
            {
                Assert.True(x.ContainsKey("If-Match"));
                Assert.Equal(dataAgent.ETag, await x["If-Match"]().ConfigureAwait(false));
            };

            proxyMock
                .Setup(m => m.DeleteAsync(url, Is.Value(verify), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.DataAgents.DeleteAsync(dataAgent.Id, dataAgent.ETag, requestContext, overridePendingCommandsCheck).ConfigureAwait(false);

            Assert.Equal(result.HttpStatusCode, actualResult.HttpStatusCode);
        }

        [Theory(DisplayName = "Verify DeleteAgents.GetOperationalReadinessBooleanArray works as expected."), AutoMoqData(true)]
        public void VerifyGetOperationalReadinessBooleanArray(
            DeleteAgent deleteAgent,
            DataManagementClient client)
        {
            deleteAgent.OperationalReadinessLow = 3;
            deleteAgent.OperationalReadinessHigh = 17;
            var actualResult = client.DataAgents.GetOperationalReadinessBooleanArray(deleteAgent);

            Assert.Equal(actualResult.Count(), sizeof(long) * 8 * 2);
            Assert.True(actualResult[0]);
            actualResult[0] = false;
            Assert.True(actualResult[1]);
            actualResult[1] = false;
            Assert.True(actualResult[sizeof(long) * 8]);
            actualResult[sizeof(long) * 8] = false;
            Assert.True(actualResult[(sizeof(long) * 8) + 4]);
            actualResult[(sizeof(long) * 8) + 4] = false;
            Assert.True(actualResult.All(x => x == false));
        }

        [Theory(DisplayName = "Verify DeleteAgents.GetOperationalReadiness works as expected."), AutoMoqData(true)]
        public void VerifyGetOperationalReadiness(
            DeleteAgent deleteAgent, 
            DataManagementClient client)
        {
            var operationalReadiness = new bool[128];
            operationalReadiness[0] = true;
            operationalReadiness[1] = true;
            operationalReadiness[65] = true;
            operationalReadiness[67] = true;
            client.DataAgents.SetOperationalReadiness(deleteAgent, operationalReadiness);

            Assert.Equal(3, deleteAgent.OperationalReadinessLow);
            Assert.Equal(10, deleteAgent.OperationalReadinessHigh);
        }

        [Theory(DisplayName = "Verify DeleteAgents.GetOperationalReadiness throws exception if size is wrong."), AutoMoqData(true)]
        public void VerifyGetOperationalReadinessThrowsException(
            DeleteAgent deleteAgent, 
            DataManagementClient client)
        {
            var operationalReadiness = new bool[60];
            operationalReadiness[0] = true;
            operationalReadiness[1] = true;
            Assert.Throws<ArgumentException>(() => client.DataAgents.SetOperationalReadiness(deleteAgent, operationalReadiness));
        }

        [Theory(DisplayName = "Verify the scenario of OperationalReadiness being converted to boolean array, then being converted back to OperationalReadiness object."), AutoMoqData(true)]
        public void VerifyTwoWayConversionForOperationalReadiness(
            DeleteAgent deleteAgent,
            DataManagementClient client)
        {
            var originalOperationalReadinessLow = deleteAgent.OperationalReadinessLow;
            var originalOperationalReadinessHigh = deleteAgent.OperationalReadinessHigh;
            var booleanResult = client.DataAgents.GetOperationalReadinessBooleanArray(deleteAgent);
            client.DataAgents.SetOperationalReadiness(deleteAgent, booleanResult);

            Assert.Equal(originalOperationalReadinessLow, deleteAgent.OperationalReadinessLow);
            Assert.Equal(originalOperationalReadinessHigh, deleteAgent.OperationalReadinessHigh);
        }
        #endregion

        #region History Items
        [Theory(DisplayName = "Verify HistoryItems.ReadByFiltersAsync method works correctly with filter criteria."), AutoMoqData(true)]
        public async Task VerifyReadHistoryItemByFiltersAsync(
            IHttpResult<Collection<HistoryItem>> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client,
            Guid entityId)
        {
            HistoryItemFilterCriteria filterCriteria = new HistoryItemFilterCriteria { EntityId = entityId.ToString() };

            string url = $"/api/v2/historyItems?$select=id,eTag,entity,writeAction,transactionId&$expand=entity&$filter=entity/id eq '{entityId}'";

            proxyMock
                .Setup(m => m.GetAsync<Collection<HistoryItem>>(Is.Value<string>(s => Assert.Equal(url, s)), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.HistoryItems.ReadByFiltersAsync(requestContext, filterCriteria).ConfigureAwait(false);

            Assert.True(actualResult.Response.Value.SequenceEqual(result.Response.Value));
        }

        [Theory(DisplayName = "Verify HistoryItems.ReadAllByFiltersAsync method works correctly with filter criteria."), AutoMoqData(true)]
        public async Task VerifyReadAllHistoryItemsAsyncMethodWithFilterCriteria(
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            DataManagementClient client,
            RequestContext request,
            HttpResult<Collection<HistoryItem>> result,
            Guid entityId)
        {
            result.Response.NextLink = null;

            HistoryItemFilterCriteria filterCriteria = new HistoryItemFilterCriteria { EntityId = entityId.ToString() };

            string url = $"/api/v2/historyItems?$select=id,eTag,entity,writeAction,transactionId&$expand=entity&$filter=entity/id eq '{entityId}'";

            proxyMock
                .Setup(m => m.GetAsync<Collection<HistoryItem>>(url, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IHttpResult<Collection<HistoryItem>>>(result));

            IHttpResult<IEnumerable<HistoryItem>> actualResult = await client.HistoryItems.ReadAllByFiltersAsync(request, filterCriteria).ConfigureAwait(false);

            Assert.True(actualResult.Response.SequenceEqual((IEnumerable<HistoryItem>)result.Response.Value));
        }
        #endregion

        #region Data Assets
        [Theory(DisplayName = "Verify DataAssets.FindByQualifier calls the correct url."), AutoMoqData]
        public async Task VerifyGetByQualifierAsync(
            AssetQualifier assetQualifier,
            IHttpResult<Collection<DataAsset>> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            var searchValue = assetQualifier.GetValueForSearch(string.Empty);
            string url = $"/api/v2/dataAssets/v2.findByQualifier(qualifier=@value)?@value='{SerializerSettings.EscapeForODataQuery(searchValue)}'";

            proxyMock
                .Setup(m => m.GetAsync<Collection<DataAsset>>(url, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.DataAssets.FindByQualifierAsync(assetQualifier, requestContext).ConfigureAwait(false);

            proxyMock.Verify(m => m.GetAsync<Collection<DataAsset>>(url, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()), Times.Once);

            Assert.True(actualResult.Response.Value.SequenceEqual(result.Response.Value));
        }

        [Theory(DisplayName = "Verify DataAssets.FindByQualifier method works correctly with filter criteria."), AutoMoqData(true)]
        public async Task VerifyFindByQualifierAsyncMethodWithFilterCriteria(
            AssetQualifier assetQualifier,
            IHttpResult<Collection<DataAsset>> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            DataAssetFilterCriteria filterCriteria = new DataAssetFilterCriteria { Index = 0, Count = 10 };

            var searchValue = assetQualifier.GetValueForSearch(string.Empty);
            string url = $"/api/v2/dataAssets/v2.findByQualifier(qualifier=@value)?@value='{SerializerSettings.EscapeForODataQuery(searchValue)}'&$top=10&$skip=0";

            proxyMock
                .Setup(m => m.GetAsync<Collection<DataAsset>>(url, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.DataAssets.FindByQualifierAsync(assetQualifier, requestContext, filterCriteria).ConfigureAwait(false);

            proxyMock.Verify(m => m.GetAsync<Collection<DataAsset>>(url, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()), Times.Once);

            Assert.True(actualResult.Response.Value.SequenceEqual(result.Response.Value));
        }
        #endregion

        #region Users
        [Theory(DisplayName = "Verify Users.ReadAsync calls the correct url.")]
        [InlineAutoMoqData("", DisableRecursionCheck = true)]
        public async Task VerifyReadUserAsync(
            string queryString,
            IHttpResult<User> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            UserClient client)
        {
            string url = $"/api/v2/users('me')?$select=id,securityGroups" + queryString;

            proxyMock
                .Setup(m => m.GetAsync<User>(
                    Is.Value<string>(s => Assert.Equal(url, s)),
                    It.IsAny<IDictionary<string, Func<Task<string>>>>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.ReadAsync(requestContext).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }
        #endregion

        #region Sharing Requests
        [Theory(DisplayName = "Verify SharingRequests.ReadAsync calls the correct url.")]
        [InlineAutoMoqData("", SharingRequestExpandOptions.None, DisableRecursionCheck = true)]
        [InlineAutoMoqData(",trackingDetails", SharingRequestExpandOptions.TrackingDetails, DisableRecursionCheck = true)]        
        public async Task VerifyReadSharingRequestAsync(
            string queryString,
            SharingRequestExpandOptions expandOptions,
            SharingRequest sharingRequest,
            IHttpResult<SharingRequest> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/sharingRequests('{sharingRequest.Id}')?$select=id,eTag,ownerId,deleteAgentId,ownerName,relationships" + queryString;

            proxyMock
                .Setup(m => m.GetAsync<SharingRequest>(Is.Value<string>(s => Assert.Equal(url, s)), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.SharingRequests.ReadAsync(sharingRequest.Id, requestContext, expandOptions).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify SharingRequests.ReadByFiltersAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyReadSharingRequestByFiltersAsync(
            IHttpResult<Collection<SharingRequest>> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/sharingRequests?$select=id,eTag,ownerId,deleteAgentId,ownerName,relationships";

            proxyMock
                .Setup(m => m.GetAsync<Collection<SharingRequest>>(Is.Value<string>(s => Assert.Equal(url, s)), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.SharingRequests.ReadByFiltersAsync(requestContext).ConfigureAwait(false);

            Assert.True(actualResult.Response.Value.SequenceEqual(result.Response.Value));
        }

        [Theory(DisplayName = "Verify SharingRequests.ReadAllByFiltersAsync method calls the correct url."), AutoMoqData(true)]
        public async Task VerifyReadAllSharingRequestsByFiltersAsync(
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            DataManagementClient client,
            RequestContext request,
            HttpResult<Collection<SharingRequest>> result)
        {
            result.Response.NextLink = null;

            string url = $"/api/v2/sharingRequests?$select=id,eTag,ownerId,deleteAgentId,ownerName,relationships";

            proxyMock
                .Setup(m => m.GetAsync<Collection<SharingRequest>>(url, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IHttpResult<Collection<SharingRequest>>>(result));

            IHttpResult<IEnumerable<SharingRequest>> actualResult = await client.SharingRequests.ReadAllByFiltersAsync(request).ConfigureAwait(false);

            Assert.True(actualResult.Response.SequenceEqual((IEnumerable<SharingRequest>)result.Response.Value));
        }

        [Theory(DisplayName = "Verify SharingRequests.ApproveAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyApproveSharingRequestAsync(
            string id,
            string etag,
            IHttpResult<object> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/sharingRequests('{id}')/v2.approve";

            proxyMock
                .Setup(m => m.PostAsync<object, object>(
                    Is.Value<string>(s => Assert.Equal(url, s)), 
                    null,
                    Is.Value<IDictionary<string, Func<Task<string>>>>(s => Assert.Equal(etag, s["If-Match"]().Result)),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.SharingRequests.ApproveAsync(id, etag, requestContext).ConfigureAwait(false);

            Assert.Equal(result.ResponseContent, actualResult.ResponseContent);
        }

        [Theory(DisplayName = "Verify SharingRequests.DeleteAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyDeleteSharingRequestAsync(
            string id,
            string etag,
            IHttpResult<object> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/sharingRequests('{id}')";

            proxyMock
                .Setup(m => m.DeleteAsync(
                    Is.Value<string>(s => Assert.Equal(url, s)),
                    Is.Value<IDictionary<string, Func<Task<string>>>>(s => Assert.Equal(etag, s["If-Match"]().Result)),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.SharingRequests.DeleteAsync(id, etag, requestContext).ConfigureAwait(false);

            Assert.Equal(result.ResponseContent, actualResult.ResponseContent);
        }
        #endregion

        #region Variant Requests
        [Theory(DisplayName = "Verify VariantRequests.CreateAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyCreateVariantRequestAsync(
            VariantRequest variantRequest,
            IHttpResult<VariantRequest> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/variantRequests";

            proxyMock
                .Setup(m => m.PostAsync<VariantRequest, VariantRequest>(url, variantRequest, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.VariantRequests.CreateAsync(variantRequest, requestContext).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify VariantRequests.ReadAsync calls the correct url.")]
        [InlineAutoMoqData("", VariantRequestExpandOptions.None, DisableRecursionCheck = true)]
        [InlineAutoMoqData(",trackingDetails", VariantRequestExpandOptions.TrackingDetails, DisableRecursionCheck = true)]
        public async Task VerifyReadVariantRequestAsync(
            string queryString,
            VariantRequestExpandOptions expandOptions,
            VariantRequest variantRequest,
            IHttpResult<VariantRequest> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/variantRequests('{variantRequest.Id}')?$select=id,eTag,ownerId,ownerName,requesterAlias,generalContractorAlias,celaContactAlias,workItemUri,requestedVariants,variantRelationships,additionalInformation" + queryString;

            proxyMock
                .Setup(m => m.GetAsync<VariantRequest>(Is.Value<string>(s => Assert.Equal(url, s)), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.VariantRequests.ReadAsync(variantRequest.Id, requestContext, expandOptions).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify VariantRequests.ReadByFiltersAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyReadVariantRequestByFiltersAsync(
            IHttpResult<Collection<VariantRequest>> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/variantRequests?$select=id,eTag,ownerId,ownerName,requesterAlias,generalContractorAlias,celaContactAlias,workItemUri,requestedVariants,variantRelationships,additionalInformation";

            proxyMock
                .Setup(m => m.GetAsync<Collection<VariantRequest>>(Is.Value<string>(s => Assert.Equal(url, s)), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.VariantRequests.ReadByFiltersAsync(requestContext).ConfigureAwait(false);

            Assert.True(actualResult.Response.Value.SequenceEqual(result.Response.Value));
        }

        [Theory(DisplayName = "Verify VariantRequests.ReadAllByFiltersAsync method calls the correct url."), AutoMoqData(true)]
        public async Task VerifyReadAllVariantRequestsByFiltersAsync(
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            DataManagementClient client,
            RequestContext request,
            HttpResult<Collection<VariantRequest>> result)
        {
            result.Response.NextLink = null;

            string url = $"/api/v2/variantRequests?$select=id,eTag,ownerId,ownerName,requesterAlias,generalContractorAlias,celaContactAlias,workItemUri,requestedVariants,variantRelationships,additionalInformation";

            proxyMock
                .Setup(m => m.GetAsync<Collection<VariantRequest>>(url, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IHttpResult<Collection<VariantRequest>>>(result));

            IHttpResult<IEnumerable<VariantRequest>> actualResult = await client.VariantRequests.ReadAllByFiltersAsync(request).ConfigureAwait(false);

            Assert.True(actualResult.Response.SequenceEqual((IEnumerable<VariantRequest>)result.Response.Value));
        }

        [Theory(DisplayName = "Verify VariantRequests.UpdateAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyUpdateVariantRequestAsync(
            VariantRequest variantRequest,
            IHttpResult<VariantRequest> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/variantRequests('{variantRequest.Id}')";

            proxyMock
                .Setup(m => m.PutAsync<VariantRequest, VariantRequest>(url, variantRequest, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.VariantRequests.UpdateAsync(variantRequest, requestContext).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify VariantRequests.ApproveAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyApproveVariantRequestAsync(
            string id,
            string etag,
            IHttpResult<object> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/variantRequests('{id}')/v2.approve";

            proxyMock
                .Setup(m => m.PostAsync<object, object>(
                    Is.Value<string>(s => Assert.Equal(url, s)),
                    null,
                    Is.Value<IDictionary<string, Func<Task<string>>>>(s => Assert.Equal(etag, s["If-Match"]().Result)),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.VariantRequests.ApproveAsync(id, etag, requestContext).ConfigureAwait(false);

            Assert.Equal(result.ResponseContent, actualResult.ResponseContent);
        }

        [Theory(DisplayName = "Verify VariantRequests.DeleteAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyDeleteVariantRequestAsync(
            string id,
            string etag,
            IHttpResult<object> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/variantRequests('{id}')";

            proxyMock
                .Setup(m => m.DeleteAsync(
                    Is.Value<string>(s => Assert.Equal(url, s)),
                    Is.Value<IDictionary<string, Func<Task<string>>>>(s => Assert.Equal(etag, s["If-Match"]().Result)),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.VariantRequests.DeleteAsync(id, etag, requestContext).ConfigureAwait(false);

            Assert.Equal(result.ResponseContent, actualResult.ResponseContent);
        }
        #endregion

        #region Transfer Requests
        [Theory(DisplayName = "Verify TransferRequests.CreateAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyCreateTransferRequestAsync(
            TransferRequest transferRequest,
            IHttpResult<TransferRequest> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/transferRequests";

            proxyMock
                .Setup(m => m.PostAsync<TransferRequest, TransferRequest>(url, transferRequest, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.TransferRequests.CreateAsync(transferRequest, requestContext).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify TransferRequests.ReadAsync calls the correct url.")]
        [InlineAutoMoqData("", TransferRequestExpandOptions.None, DisableRecursionCheck = true)]
        [InlineAutoMoqData(",trackingDetails", TransferRequestExpandOptions.TrackingDetails, DisableRecursionCheck = true)]
        public async Task VerifyReadTransferRequestAsync(
            string queryString,
            TransferRequestExpandOptions expandOptions,
            TransferRequest transferRequest,
            IHttpResult<TransferRequest> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/transferRequests('{transferRequest.Id}')?$select=id,eTag,sourceOwnerId,targetOwnerId,requestState,assetGroups" + queryString;

            proxyMock
                .Setup(m => m.GetAsync<TransferRequest>(Is.Value<string>(s => Assert.Equal(url, s)), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.TransferRequests.ReadAsync(transferRequest.Id, requestContext, expandOptions).ConfigureAwait(false);

            Assert.Equal(result.Response.Id, actualResult.Response.Id);
        }

        [Theory(DisplayName = "Verify TransferRequests.ReadByFiltersAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyReadTransferRequestByFiltersAsync(
            IHttpResult<Collection<TransferRequest>> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/transferRequests?$select=id,eTag,sourceOwnerId,targetOwnerId,requestState,assetGroups";

            proxyMock
                .Setup(m => m.GetAsync<Collection<TransferRequest>>(Is.Value<string>(s => Assert.Equal(url, s)), It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.TransferRequests.ReadByFiltersAsync(requestContext).ConfigureAwait(false);

            Assert.True(actualResult.Response.Value.SequenceEqual(result.Response.Value));
        }

        [Theory(DisplayName = "Verify TransferRequests.ReadAllByFiltersAsync method calls the correct url."), AutoMoqData(true)]
        public async Task VerifyReadAllTransferRequestsByFiltersAsync(
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            DataManagementClient client,
            RequestContext request,
            HttpResult<Collection<TransferRequest>> result)
        {
            result.Response.NextLink = null;

            string url = $"/api/v2/transferRequests?$select=id,eTag,sourceOwnerId,targetOwnerId,requestState,assetGroups";

            proxyMock
                .Setup(m => m.GetAsync<Collection<TransferRequest>>(url, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IHttpResult<Collection<TransferRequest>>>(result));

            IHttpResult<IEnumerable<TransferRequest>> actualResult = await client.TransferRequests.ReadAllByFiltersAsync(request).ConfigureAwait(false);

            Assert.True(actualResult.Response.SequenceEqual((IEnumerable<TransferRequest>)result.Response.Value));
        }

        [Theory(DisplayName = "Verify TransferRequests.ApproveAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyApproveTransferRequestAsync(
            string id,
            string etag,
            IHttpResult<object> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/transferRequests('{id}')/v2.approve";

            proxyMock
                .Setup(m => m.PostAsync<object, object>(
                    Is.Value<string>(s => Assert.Equal(url, s)),
                    null,
                    Is.Value<IDictionary<string, Func<Task<string>>>>(s => Assert.Equal(etag, s["If-Match"]().Result)),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.TransferRequests.ApproveAsync(id, etag, requestContext).ConfigureAwait(false);

            Assert.Equal(result.ResponseContent, actualResult.ResponseContent);
        }

        [Theory(DisplayName = "Verify TransferRequests.DeleteAsync calls the correct url."), AutoMoqData(true)]
        public async Task VerifyDeleteTransferRequestAsync(
            string id,
            string etag,
            IHttpResult<object> result,
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            RequestContext requestContext,
            DataManagementClient client)
        {
            string url = $"/api/v2/transferRequests('{id}')";

            proxyMock
                .Setup(m => m.DeleteAsync(
                    Is.Value<string>(s => Assert.Equal(url, s)),
                    Is.Value<IDictionary<string, Func<Task<string>>>>(s => Assert.Equal(etag, s["If-Match"]().Result)),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            var actualResult = await client.TransferRequests.DeleteAsync(id, etag, requestContext).ConfigureAwait(false);

            Assert.Equal(result.ResponseContent, actualResult.ResponseContent);
        }
        #endregion

        #region Common Logic
        [Theory(DisplayName = "Verify ReadMany method works correctly."), AutoMoqData(DisableRecursionCheck = true)]
        public async Task VerifyReadManyMethod(
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            DataManagementClient client,
            RequestContext request,
            HttpResult<Collection<DataOwner>> firstResult,
            HttpResult<Collection<DataOwner>> secondResult)
        {
            string initialUrl = $"/api/v2/dataOwners?$select=id,eTag,name,description,alertContacts,announcementContacts,sharingRequestContacts,writeSecurityGroups,tagSecurityGroups,tagApplicationIds,icm,hasInitiatedTransferRequests,hasPendingTransferRequests";
            Uri nextLinkUri = new Uri("https://management.privacy.microsoft.com/api/v2/dataOwners?$select=id,eTag,name,description,alertContacts,announcementContacts,writeSecurityGroups&$top=1&$skip=1");

            firstResult.Response.NextLink = nextLinkUri;
            secondResult.Response.NextLink = null;

            proxyMock
                .Setup(m => m.GetAsync<Collection<DataOwner>>(initialUrl, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IHttpResult<Collection<DataOwner>>>(firstResult));
            proxyMock
                .Setup(m => m.GetAsync<Collection<DataOwner>>(nextLinkUri.PathAndQuery, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IHttpResult<Collection<DataOwner>>>(secondResult));

            IHttpResult<IEnumerable<DataOwner>> actualResult = await client.DataOwners.ReadAllByFiltersAsync(request).ConfigureAwait(false);

            Assert.True(actualResult.Response.SequenceEqual(firstResult.Response.Value.Concat(secondResult.Response.Value)));

            proxyMock.Verify(m => m.GetAsync<Collection<DataOwner>>(initialUrl, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()), Times.Once);
            proxyMock.Verify(m => m.GetAsync<Collection<DataOwner>>(nextLinkUri.PathAndQuery, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()), Times.Once);
            proxyMock.Verify(m => m.GetAsync<Collection<DataOwner>>(null, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory(DisplayName = "Verify ReadMany method calls the Get() function for each result batch."), AutoMoqData(DisableRecursionCheck = true)]
        public async Task VerifyReadManyMethodCallsGet(
            [Frozen] Mock<IHttpServiceProxy> proxyMock,
            DataManagementClient client,
            RequestContext request,
            Mock<IHttpResult<Collection<DataOwner>>> resultMock,
            Collection<DataOwner> response)
        {
            string initialUrl = $"/api/v2/dataOwners?$select=id,eTag,name,description,alertContacts,announcementContacts,sharingRequestContacts,writeSecurityGroups,tagSecurityGroups,tagApplicationIds,icm,hasInitiatedTransferRequests,hasPendingTransferRequests";

            response.NextLink = null;
            resultMock.Setup(m => m.Response).Returns(response);

            proxyMock
                .Setup(m => m.GetAsync<Collection<DataOwner>>(initialUrl, It.IsAny<IDictionary<string, Func<Task<string>>>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IHttpResult<Collection<DataOwner>>>(resultMock.Object));

            IHttpResult<IEnumerable<DataOwner>> actualResult = await client.DataOwners.ReadAllByFiltersAsync(request).ConfigureAwait(false);

            resultMock.Verify(m => m.HttpStatusCode, Times.Exactly(2));
        }
        #endregion

        private void CompareDataAgents(IEnumerable<DataAgent> actual, IEnumerable<DataAgent> expected)
        {
            Assert.Equal(expected.Count(), actual.Count());

            for (int index = 0; index < actual.Count(); index++)
            {
                Assert.Equal(expected.ElementAt(index), actual.ElementAt(index));
            }
        }
    }
}