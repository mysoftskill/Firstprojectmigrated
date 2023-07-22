using Microsoft.AspNetCore.Http;
using Microsoft.PrivacyServices.UX.I9n.Cookers;
using Microsoft.PrivacyServices.UX.I9n.Scenario;
using System;

namespace Microsoft.PrivacyServices.UX
{
    public class MockPdmsScenarioHelper
    {
        private readonly MockCookerAccessor mockCookerAccessor;
        private readonly ScenarioConfigurator scenarioConfigurator;

        public MockPdmsScenarioHelper(IHttpContextAccessor httpContextAccessor)
        {
            mockCookerAccessor = new MockCookerAccessor();
            scenarioConfigurator = new ScenarioConfigurator(httpContextAccessor);

            ConfigureAllScenarios();
        }

        public Func<object> GetMethodMock(string methodName)
        {
            return scenarioConfigurator.GetMethodMock(methodName);
        }

        private void ConfigureAllScenarios()
        {
            CreateDataOwnerMocks();
            CreateDataAssetMocks();
            CreateDataAgentMocks();
        }

        private void CreateDataOwnerMocks()
        {
            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "DataOwners.FindAllByAuthenticatedUserAsync",
                                mockCookerAccessor.DataOwnerMockCooker.CookListOfDataOwners());

            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "DataOwner.ReadAsync.Owner.Team1",
                                mockCookerAccessor.DataOwnerMockCooker.CookDataOwnerFor("Team1"));
            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "DataOwner.UpdateAsync.Owner.Team1",
                                mockCookerAccessor.DataOwnerMockCooker.CookDataOwnerFor("Team1"));
            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "DataOwner.DeleteAsync.Owner.Team1",
                                mockCookerAccessor.DataOwnerMockCooker.CookEmptyHttpResult());

            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "DataOwner.CreateAsync.Owner.Team3",
                                mockCookerAccessor.DataOwnerMockCooker.CookDataOwnerFor("Team3"));
        }

        private void CreateDataAgentMocks()
        {
            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "DataAgents.ReadAllByFiltersAsync.Team1",
                                mockCookerAccessor.DataAgentMockCooker.CookListOfDeleteAgentsFor("Team1"));
            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "DataAgents.ReadAllByFiltersAsync.Team2",
                                mockCookerAccessor.DataAgentMockCooker.CookListOfDeleteAgentsFor("Team2"));

            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "DataAgents.ReadAsync.Agent1",
                                mockCookerAccessor.DataAgentMockCooker.CookDataAgentFor("Team1"));

            scenarioConfigurator.ConfigureMethodMock(
                                Scenario.ManageDataAgents.RemoveAgent,
                                "DataAgents.ReadAllByFiltersAsync.Team1",
                                mockCookerAccessor.DataAgentMockCooker.CookListOfDeleteAgentsWithoutProdConnectionFor("Team1"));
            scenarioConfigurator.ConfigureMethodMock(
                                Scenario.ManageDataAgents.RemoveAgent,
                                "DataAgents.ReadAllByFiltersAsync.Team2",
                                mockCookerAccessor.DataAgentMockCooker.CookListOfDeleteAgentsWithoutProdConnectionFor("Team2"));

            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "DataAgents.ReadAllByFiltersAsync.Count.Team1",
                                mockCookerAccessor.DataAgentMockCooker.CookListOfDataAgentsWithCountFor("Team1"));
            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "DataAgents.ReadAllByFiltersAsync.Count.Team2",
                                mockCookerAccessor.DataAgentMockCooker.CookListOfDataAgentsWithCountFor("Team2"));

            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "DataAgents.GetOperationalReadinessBooleanArray.Agent1",
                                mockCookerAccessor.DataAgentMockCooker.CookOperationalReadinessFor("Agent1"));
            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "DataAgents.GetOperationalReadinessBooleanArray.Agent2",
                                mockCookerAccessor.DataAgentMockCooker.CookOperationalReadinessFor("Agent2"));

            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "DataAgents.DeleteAsync.Agent1",
                                mockCookerAccessor.DataAgentMockCooker.CookEmptyHttpResult());
        }

        private void CreateDataAssetMocks()
        {
            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "AssetGroups.ReadAsync.AssetGroup1",
                                mockCookerAccessor.DataAssetMockCooker.CookAssetGroupFor("Team1"));

            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "AssetGroups.ReadAllByFiltersAsync.Team1",
                                mockCookerAccessor.DataAssetMockCooker.CookListOfAssetGroupsFor("Team1"));
            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "AssetGroups.ReadAllByFiltersAsync.Team2",
                                mockCookerAccessor.DataAssetMockCooker.CookListOfAssetGroupsFor("Team2"));

            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "AssetGroups.ReadAllByFiltersAsync.Count.Team1",
                                mockCookerAccessor.DataAssetMockCooker.CookListOfAssetGroupsWithCountFor("Team1"));
            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "AssetGroups.ReadAllByFiltersAsync.Count.Team2",
                                mockCookerAccessor.DataAssetMockCooker.CookListOfAssetGroupsWithCountFor("Team2"));

            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "AssetGroups.DeleteAsync.AssetGroup1",
                                mockCookerAccessor.DataAssetMockCooker.CookEmptyHttpResult());
        }
    }
}
