using System;
using Microsoft.AspNetCore.Http;
using Microsoft.PrivacyServices.UX.I9n.Cookers;
using Microsoft.PrivacyServices.UX.I9n.Scenario;

namespace Microsoft.PrivacyServices.UX.Core.PxsClient
{
    public class MockPxsScenarioHelper
    {
        private readonly MockCookerAccessor mockCookerAccessor;
        private readonly ScenarioConfigurator scenarioConfigurator;

        public MockPxsScenarioHelper(IHttpContextAccessor httpContextAccessor)
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
            CreateDeleteAltSubjectMocks();
            CreateRequestStatusMocks();
            CreateExportAltSubjectMocks();
        }

        private void CreateDeleteAltSubjectMocks()
        {
            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "DeleteRequest.AltSubject",
                                mockCookerAccessor.ManualRequestMockCooker.CookDeleteOperationResponse());
        }

        private void CreateExportAltSubjectMocks()
        {
            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "ExportRequest.AltSubject",
                                mockCookerAccessor.ManualRequestMockCooker.CookExportOperationResponse());
        }

        private void CreateRequestStatusMocks()
        {
            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "RequestStatus.ExportRequest",
                                mockCookerAccessor.ManualRequestMockCooker.CookExportRequestStatusResponse());
            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "RequestStatus.DeleteRequest",
                                mockCookerAccessor.ManualRequestMockCooker.CookDeleteRequestStatusResponse());
            scenarioConfigurator.ConfigureDefaultMethodMock(
                                "RequestStatus.AccountCloseRequest",
                                mockCookerAccessor.ManualRequestMockCooker.CookAccountCloseRequestStatusResponse());
        }
    }
}
