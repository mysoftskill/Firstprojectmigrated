using System;
using Microsoft.AspNetCore.Http;
using Microsoft.PrivacyServices.UX.I9n.Cookers;
using Microsoft.PrivacyServices.UX.I9n.Scenario;

namespace Microsoft.PrivacyServices.UX
{
    public class MockServiceTreeScenarioHelper
    {
        private readonly MockCookerAccessor mockCookerAccessor;
        private readonly ScenarioConfigurator scenarioConfigurator;

        public MockServiceTreeScenarioHelper(IHttpContextAccessor httpContextAccessor)
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
            ConfigureMocksForFindNodesByName();
            ConfigureMocksForReadServiceWithExtendedProperties();
        }

        private void ConfigureMocksForFindNodesByName()
        {
            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "FindNodesByName.Service.Team2",
                                mockCookerAccessor.ServiceTreeMockCooker.CookListOfServicesFor("Team2"));
            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "FindNodesByName.Service.Team3",
                                mockCookerAccessor.ServiceTreeMockCooker.CookListOfServicesFor("Team3"));
        }

        private void ConfigureMocksForReadServiceWithExtendedProperties()
        {
            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "ReadServiceWithExtendedProperties.Service.Team2",
                                mockCookerAccessor.ServiceTreeMockCooker.CookServiceFor("Team2"));
            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "ReadServiceWithExtendedProperties.Service.Team3",
                                mockCookerAccessor.ServiceTreeMockCooker.CookServiceFor("Team3"));
        }
    }
}
