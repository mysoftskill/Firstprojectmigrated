using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.PrivacyServices.PrivacyOperation.Client;
using Microsoft.PrivacyServices.PrivacyOperation.Client.Models;
using Microsoft.PrivacyServices.PrivacyOperation.Contracts;
using Moq;

namespace Microsoft.PrivacyServices.UX.Core.PxsClient
{
    public class MockPxsClientProvider : IPxsClientProvider
    {
        private readonly Mock<IPrivacyOperationClient> instance;
        private readonly MockPxsScenarioHelper scenarioHelper;
        private readonly IHttpContextAccessor httpContextAccessor;

        public MockPxsClientProvider(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            instance = new Mock<IPrivacyOperationClient>(MockBehavior.Strict);
            scenarioHelper = new MockPxsScenarioHelper(httpContextAccessor);

            //  Setup method mocks.
            CreateMocksForDeleteDemographicSubjectRequest();
            CreateMocksForGetRequestStatuses();
            CreateMocksForExportDemographicSubjectRequest();

            Instance = instance.Object;
        }

        private void CreateMocksForDeleteDemographicSubjectRequest()
        {
            instance.Setup(i => i.PostDeleteRequestAsync(
                It.IsAny<DeleteOperationArgs>())
            ).Returns((Task<DeleteOperationResponse>)scenarioHelper.GetMethodMock(
                "DeleteRequest.AltSubject"
            ).DynamicInvoke());
        }

        private void CreateMocksForExportDemographicSubjectRequest()
        {
            instance.Setup(i => i.PostExportRequestAsync(
                It.IsAny<ExportOperationArgs>())
            ).Returns((Task<ExportOperationResponse>)scenarioHelper.GetMethodMock(
                "ExportRequest.AltSubject"
            ).DynamicInvoke());
        }

        private void CreateMocksForGetRequestStatuses()
        {
            instance.Setup(i => i.ListRequestsAsync(
                It.Is<ListOperationArgs>(
                    listOperationArgs => listOperationArgs.RequestTypes.Contains(PrivacyRequestType.Export)
                ))
            ).Returns((Task<IList<PrivacyRequestStatus>>)scenarioHelper.GetMethodMock(
                "RequestStatus.ExportRequest"
            ).DynamicInvoke());

            instance.Setup(i => i.ListRequestsAsync(
                It.Is<ListOperationArgs>(
                    listOperationArgs => listOperationArgs.RequestTypes.Contains(PrivacyRequestType.Delete)
                ))
            ).Returns((Task<IList<PrivacyRequestStatus>>)scenarioHelper.GetMethodMock(
                "RequestStatus.DeleteRequest"
            ).DynamicInvoke());

            instance.Setup(i => i.ListRequestsAsync(
                It.Is<ListOperationArgs>(
                    listOperationArgs => listOperationArgs.RequestTypes.Contains(PrivacyRequestType.AccountClose)
                ))
            ).Returns((Task<IList<PrivacyRequestStatus>>)scenarioHelper.GetMethodMock(
                "RequestStatus.AccountCloseRequest"
            ).DynamicInvoke());
        }

        #region IPxsClientProvider Members

        public IPrivacyOperationClient Instance { get; }

        public async Task<T> ApplyRequestContext<T>(T operationArgs) where T : BasePrivacyOperationArgs
        {
            return await Task.FromResult(operationArgs);
        }

        #endregion
    }
}
