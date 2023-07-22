using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.PrivacyServices.DataManagement.Client;
using Microsoft.PrivacyServices.DataManagement.Client.V2;
using Moq;

namespace Microsoft.PrivacyServices.UX.Core.PdmsClient
{
    public class MockPdmsClientProvider : IPdmsClientProvider
    {
        private readonly Mock<IDataManagementClient> instance;
        private readonly MockPdmsScenarioHelper scenarioHelper;
        private readonly IHttpContextAccessor httpContextAccessor;

        public MockPdmsClientProvider(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            instance = new Mock<IDataManagementClient>(MockBehavior.Strict);
            scenarioHelper = new MockPdmsScenarioHelper(httpContextAccessor);

            //  Setup method mocks.
            CreateMocksForGetOwnersByAuthenticatedUser();
            CreateMocksForGetAssetGroupsByOwnerId();
            CreateMocksForGetAgentsByOwnerId();
            CreateMocksForGetAssetGroupsCountByOwnerId();
            CreateMocksForGetDataAgentsCountByOwnerId();
            CreateMocksForCreateOwner();
            CreateMocksForGetDataOwnerWithServiceTree();
            CreateMocksForUpdateDataOwnerWithServiceTree();
            CreateMocksForDeleteDataOwnerById();
            CreateMocksForDeleteAssetGroupById();
            CreateMocksForGetOperationalReadinessBooleanArray();
            CreateMocksForDeleteAgentById();

            Instance = instance.Object;
        }

        private void CreateMocksForGetAgentsByOwnerId()
        {
            instance.Setup(i => i.DataAgents.ReadAllByFiltersAsync(
                It.IsAny<RequestContext>(),
                DataAgentExpandOptions.HasSharingRequests,
                It.Is<DeleteAgentFilterCriteria>(criteria => criteria.OwnerId.EndsWith("01")))
            ).Returns((Task<IHttpResult<IEnumerable<DeleteAgent>>>)scenarioHelper.GetMethodMock(
                "DataAgents.ReadAllByFiltersAsync.Team1"
            ).DynamicInvoke());

            instance.Setup(i => i.DataAgents.ReadAllByFiltersAsync(
                It.IsAny<RequestContext>(),
                DataAgentExpandOptions.HasSharingRequests,
                It.Is<DeleteAgentFilterCriteria>(criteria => criteria.OwnerId.EndsWith("02")))
            ).Returns((Task<IHttpResult<IEnumerable<DeleteAgent>>>)scenarioHelper.GetMethodMock(
                "DataAgents.ReadAllByFiltersAsync.Team2"
            ).DynamicInvoke());
        }

        private void CreateMocksForGetAssetGroupsByOwnerId()
        {
            instance.Setup(i => i.AssetGroups.ReadAllByFiltersAsync(
                It.IsAny<RequestContext>(),
                AssetGroupExpandOptions.None,
                It.Is<AssetGroupFilterCriteria>(criteria => criteria.OwnerId.EndsWith("01")))
            ).Returns((Task<IHttpResult<IEnumerable<AssetGroup>>>)scenarioHelper.GetMethodMock(
                "AssetGroups.ReadAllByFiltersAsync.Team1"
            ).DynamicInvoke());

            instance.Setup(i => i.AssetGroups.ReadAllByFiltersAsync(
                It.IsAny<RequestContext>(),
                AssetGroupExpandOptions.None,
                It.Is<AssetGroupFilterCriteria>(criteria => criteria.OwnerId.EndsWith("02")))
            ).Returns((Task<IHttpResult<IEnumerable<AssetGroup>>>)scenarioHelper.GetMethodMock(
                "AssetGroups.ReadAllByFiltersAsync.Team2"
            ).DynamicInvoke());
        }

        private void CreateMocksForGetAssetGroupsCountByOwnerId()
        {
            instance.Setup(i => i.AssetGroups.ReadByFiltersAsync(
                It.IsAny<RequestContext>(),
                AssetGroupExpandOptions.None,
                It.Is<AssetGroupFilterCriteria>(criteria => criteria.OwnerId.EndsWith("01")))
            ).Returns((Task<IHttpResult<Collection<AssetGroup>>>)scenarioHelper.GetMethodMock(
                "AssetGroups.ReadAllByFiltersAsync.Count.Team1"
            ).DynamicInvoke());

            instance.Setup(i => i.AssetGroups.ReadByFiltersAsync(
                It.IsAny<RequestContext>(),
                AssetGroupExpandOptions.None,
                It.Is<AssetGroupFilterCriteria>(criteria => criteria.OwnerId.EndsWith("02")))
            ).Returns((Task<IHttpResult<Collection<AssetGroup>>>)scenarioHelper.GetMethodMock(
                "AssetGroups.ReadAllByFiltersAsync.Count.Team2"
            ).DynamicInvoke());
        }

        private void CreateMocksForGetDataAgentsCountByOwnerId()
        {
            instance.Setup(i => i.DataAgents.ReadByFiltersAsync(
                It.IsAny<RequestContext>(),
                DataAgentExpandOptions.None,
                It.Is<DeleteAgentFilterCriteria>(criteria => criteria.OwnerId.EndsWith("01")))
            ).Returns((Task<IHttpResult<Collection<DeleteAgent>>>)scenarioHelper.GetMethodMock(
                "DataAgents.ReadAllByFiltersAsync.Count.Team1"
            ).DynamicInvoke());

            instance.Setup(i => i.DataAgents.ReadByFiltersAsync(
                It.IsAny<RequestContext>(),
                DataAgentExpandOptions.None,
                It.Is<DeleteAgentFilterCriteria>(criteria => criteria.OwnerId.EndsWith("02")))
            ).Returns((Task<IHttpResult<Collection<DeleteAgent>>>)scenarioHelper.GetMethodMock(
                "DataAgents.ReadAllByFiltersAsync.Count.Team2"
            ).DynamicInvoke());
        }

        private void CreateMocksForGetOwnersByAuthenticatedUser()
        {
            instance.Setup(i => i.DataOwners.FindAllByAuthenticatedUserAsync(
                It.IsAny<RequestContext>(),
                It.IsAny<DataOwnerExpandOptions>())
            ).Returns((Task<IHttpResult<IEnumerable<DataOwner>>>)scenarioHelper.GetMethodMock(
                "DataOwners.FindAllByAuthenticatedUserAsync"
            ).DynamicInvoke());
        }

        private void CreateMocksForCreateOwner()
        {
            instance.Setup(i => i.DataOwners.CreateAsync(
                It.IsAny<DataOwner>(),
                It.IsAny<RequestContext>())
            ).Returns((Task<IHttpResult<DataOwner>>)scenarioHelper.GetMethodMock(
                "DataOwner.CreateAsync.Owner.Team3"
            ).DynamicInvoke());
        }

        private void CreateMocksForGetDataOwnerWithServiceTree()
        {
            instance.Setup(i => i.DataOwners.ReadAsync(
                It.Is<string>(id => id.EndsWith("01")),
                It.IsAny<RequestContext>(),
                It.IsAny<DataOwnerExpandOptions>())
            ).Returns((Task<IHttpResult<DataOwner>>)scenarioHelper.GetMethodMock(
                "DataOwner.ReadAsync.Owner.Team1"
            ).DynamicInvoke());

            instance.Setup(i => i.DataOwners.ReadAsync(
                It.Is<string>(id => id.EndsWith("01")),
                It.IsAny<RequestContext>(),
                It.Is<DataOwnerExpandOptions>(options => options.Equals(DataOwnerExpandOptions.ServiceTree)))
            ).Returns((Task<IHttpResult<DataOwner>>)scenarioHelper.GetMethodMock(
                "DataOwner.ReadAsync.Owner.Team1"
            ).DynamicInvoke());
        }

        private void CreateMocksForUpdateDataOwnerWithServiceTree()
        {
            instance.Setup(i => i.DataOwners.UpdateAsync(
                It.Is<DataOwner>(owner => owner.Id.EndsWith("01")),
                It.IsAny<RequestContext>())
            ).Returns((Task<IHttpResult<DataOwner>>)scenarioHelper.GetMethodMock(
                "DataOwner.UpdateAsync.Owner.Team1"
            ).DynamicInvoke());
        }

        private void CreateMocksForDeleteDataOwnerById()
        {
            instance.Setup(i => i.DataOwners.DeleteAsync(
                It.Is<string>(id => id.EndsWith("01")),
                It.IsAny<string>(),
                It.IsAny<RequestContext>())
            ).Returns((Task<IHttpResult>)scenarioHelper.GetMethodMock(
                "DataOwner.DeleteAsync.Owner.Team1"
            ).DynamicInvoke());
        }

        private void CreateMocksForDeleteAssetGroupById()
        {
            instance.Setup(i => i.AssetGroups.ReadAsync(
                It.Is<string>(id => id.EndsWith("01")),
                It.IsAny<RequestContext>(),
                It.IsAny<AssetGroupExpandOptions>())
            ).Returns((Task<IHttpResult<AssetGroup>>)scenarioHelper.GetMethodMock(
                "AssetGroups.ReadAsync.AssetGroup1"
            ).DynamicInvoke());

            instance.Setup(i => i.AssetGroups.DeleteAsync(
                It.Is<string>(id => id.EndsWith("01")),
                It.IsAny<string>(),
                It.IsAny<RequestContext>())
            ).Returns((Task<IHttpResult>)scenarioHelper.GetMethodMock(
                "AssetGroups.DeleteAsync.AssetGroup1"
            ).DynamicInvoke());
        }

        private void CreateMocksForDeleteAgentById()
        {
            instance.Setup(i => i.DataAgents.ReadAsync<DeleteAgent>(
                It.Is<string>(id => id.EndsWith("01")),
                It.IsAny<RequestContext>(),
                It.IsAny<DataAgentExpandOptions>())
            ).Returns((Task<IHttpResult<DeleteAgent>>)scenarioHelper.GetMethodMock(
                "DataAgents.ReadAsync.Agent1"
            ).DynamicInvoke());

            instance.Setup(i => i.DataAgents.DeleteAsync(
                It.Is<string>(id => id.EndsWith("01")),
                It.IsAny<string>(),
                It.IsAny<RequestContext>(), false)
            ).Returns((Task<IHttpResult>)scenarioHelper.GetMethodMock(
                "DataAgents.DeleteAsync.Agent1"
            ).DynamicInvoke());
        }

        private void CreateMocksForGetOperationalReadinessBooleanArray()
        {
            instance.Setup(i => i.DataAgents.GetOperationalReadinessBooleanArray(
                It.Is<DeleteAgent>(agent => agent.Id.EndsWith("01")))
            ).Returns((bool[])scenarioHelper.GetMethodMock(
                "DataAgents.GetOperationalReadinessBooleanArray.Agent1"
            ).DynamicInvoke());

            instance.Setup(i => i.DataAgents.GetOperationalReadinessBooleanArray(
                It.Is<DeleteAgent>(agent => agent.Id.EndsWith("02")))
            ).Returns((bool[])scenarioHelper.GetMethodMock(
                "DataAgents.GetOperationalReadinessBooleanArray.Agent2"
            ).DynamicInvoke());
        }

        #region IPdmsClientProvider Members

        //  TODO: Consider extracting these functions into a common entity to reduce duplication.
        public DataManagement.Client.V2.IDataManagementClient Instance { get; }

        public Task<RequestContext> CreateNewRequestContext()
        {
            return Task.FromResult(CreateNewRequestContext("mockedJwtToken"));
        }

        public RequestContext CreateNewRequestContext(string jwtAuthToken)
        {
            return new Mock<RequestContext>().Object;
        }

        #endregion

    }
}
