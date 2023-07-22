using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.PrivacyServices.DataManagement.Client;
using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree;
using Moq;

namespace Microsoft.PrivacyServices.UX.Core.ServiceTreeClient
{
    public class MockServiceTreeClientProvider : IServiceTreeClientProvider
    {
        private readonly Mock<IServiceTreeClient> instance;
        private readonly MockServiceTreeScenarioHelper scenarioHelper;
        private readonly IHttpContextAccessor httpContextAccessor;

        public MockServiceTreeClientProvider(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            instance = new Mock<IServiceTreeClient>(MockBehavior.Strict);
            scenarioHelper = new MockServiceTreeScenarioHelper(httpContextAccessor);

            //  Setup method mocks.
            CreateMocksForGetServicesByName();
            CreateMocksForGetServiceById();

            Instance = instance.Object;
        }

        private void CreateMocksForGetServicesByName()
        {
            instance.Setup(i => i.FindServicesByName(
                It.Is<string>(nameSubstring => nameSubstring.Equals("I9n_Team2")),
                It.IsAny<RequestContext>())
            ).Returns((Task<IHttpResult<IEnumerable<Hierarchy>>>)scenarioHelper.GetMethodMock(
                "FindNodesByName.Service.Team2"
            ).DynamicInvoke());

            instance.Setup(i => i.FindServicesByName(
                It.Is<string>(nameSubstring => nameSubstring.Equals("I9n_Team3")),
                It.IsAny<RequestContext>())
            ).Returns((Task<IHttpResult<IEnumerable<Hierarchy>>>)scenarioHelper.GetMethodMock(
                "FindNodesByName.Service.Team3"
            ).DynamicInvoke());
        }

        private void CreateMocksForGetServiceById()
        {
            instance.Setup(i => i.ReadServiceWithExtendedProperties(
                It.Is<Guid>(id => id.ToString().EndsWith("02")),
                It.IsAny<RequestContext>())
            ).Returns((Task<IHttpResult<Service>>)scenarioHelper.GetMethodMock(
                "ReadServiceWithExtendedProperties.Service.Team2"
            ).DynamicInvoke());

            instance.Setup(i => i.ReadServiceWithExtendedProperties(
                It.Is<Guid>(id => id.ToString().EndsWith("03")),
                It.IsAny<RequestContext>())
            ).Returns((Task<IHttpResult<Service>>)scenarioHelper.GetMethodMock(
                "ReadServiceWithExtendedProperties.Service.Team3"
            ).DynamicInvoke());
        }

        #region IServiceTreeClientProvider Members

        public DataManagement.Client.ServiceTree.IServiceTreeClient Instance { get; }

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
