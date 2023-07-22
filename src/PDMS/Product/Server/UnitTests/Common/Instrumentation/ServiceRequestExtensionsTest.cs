namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation.UnitTest
{
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;

    using Ms.Qos;

    using Xunit;

    public class ServiceRequestExtensionsTest
    {
        [Theory(DisplayName = "Verify set session status.")]
        [InlineData(SessionStatus.Success, true, ServiceRequestStatus.Success)]
        [InlineData(SessionStatus.Error, false, ServiceRequestStatus.CallerError)]
        [InlineData(SessionStatus.Fault, false, ServiceRequestStatus.ServiceError)]
        public void VerifySetSessionStatus(SessionStatus status, bool succeeded, ServiceRequestStatus requestStatus)
        {
            var sllEvent = new BaseIncomingErrorEvent();
            sllEvent.baseData.SetSessionStatus(status);

            Assert.Equal(succeeded, sllEvent.baseData.succeeded);
            Assert.Equal(requestStatus, sllEvent.baseData.requestStatus);
        }
    }
}