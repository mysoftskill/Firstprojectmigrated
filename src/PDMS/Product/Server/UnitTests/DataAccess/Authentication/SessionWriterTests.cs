namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication.UnitTest
{
    using System;
    using System.Diagnostics.Tracing;

    using global::Autofac;

    using Microsoft.Graph;
    using Microsoft.Identity.Client;
    using Microsoft.PrivacyServices.DataManagement.Common.Autofac;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.Testing;
    using Microsoft.Telemetry;

    using Moq;
    using Xunit;

    public class SessionWriterTests
    {
        [Theory(DisplayName = "When an MsalException is returned, log it as an exception."), AutoMoqData]
        public void VerifyConversionForAdalException(Mock<ILogger<Base>> log, string errorCode, string message)
        {
            var sessionWriterFactory = this.CreateSessionFactory(log);

            var data = new MsalException(errorCode, message);
            
            sessionWriterFactory.WriteDone(SessionType.Outgoing, SessionStatus.Fault, string.Empty, 0, string.Empty, data);

            Action<MsalExceptionEvent> verify = v =>
            {
                Assert.Equal(message, v.message);
                Assert.Equal(data.StackTrace, v.stackTrace);
                Assert.Equal(data.ErrorCode, v.baseData.protocolStatusCode);
                Assert.Equal(Ms.Qos.ServiceRequestStatus.ServiceError, v.baseData.requestStatus);
            };

            log.Verify(m => m.Write(It.IsAny<SessionProperties>(), Is.Value(verify), EventLevel.Error, EventOptions.None, string.Empty), Times.Once());
        }

        [Theory(DisplayName = "When a IUserTransitiveMemberOfCollectionWithReferencesPagee is returned, log it as a success."), AutoMoqData]
        public void VerifyConversionForIUserTransitiveMemberOfCollectionWithReferencesPage(Mock<ILogger<Base>> log, IUserTransitiveMemberOfCollectionWithReferencesPage data)
        {
            var sessionWriterFactory = this.CreateSessionFactory(log);
            
            sessionWriterFactory.WriteDone(SessionType.Outgoing, SessionStatus.Success, string.Empty, 0, string.Empty, data);

            Action<MicrosoftGraphSuccessEvent> verify = v =>
            {
                Assert.Equal(data.Count, v.count);
                Assert.Equal(data.NextPageRequest.RequestUrl, v.nextPageRequestUrl);
                Assert.Equal("Success", v.baseData.protocolStatusCode);
            };

            log.Verify(m => m.Write(It.IsAny<SessionProperties>(), Is.Value(verify), EventLevel.Informational, EventOptions.None, string.Empty), Times.Once());
        }

        [Theory(DisplayName = "When a ServiceException is returned, log it as a success."), AutoMoqData(true)]
        public void VerifyConversionForServiceException(Mock<ILogger<Base>> log, ServiceException data)
        {
            var sessionWriterFactory = this.CreateSessionFactory(log);

            sessionWriterFactory.WriteDone(SessionType.Outgoing, SessionStatus.Fault, string.Empty, 0, string.Empty, data);

            Action<MicrosoftGraphExceptionEvent> verify = v =>
            {
                Assert.Equal(data.Error.Code, v.error.code);
                Assert.Equal(data.Error.Message, v.error.message);
                Assert.Equal(data.Error.Code, v.baseData.protocolStatusCode);
            };

            log.Verify(m => m.Write(It.IsAny<SessionProperties>(), Is.Value(verify), EventLevel.Error, EventOptions.None, string.Empty), Times.Once());
        }

        private ISessionWriterFactory CreateSessionFactory(Mock<ILogger<Base>> log)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new AuthenticationModule());
            builder.RegisterModule(new InstrumentationModule());
            builder.RegisterInstance(log.Object);

            return builder.Build().Resolve<ISessionWriterFactory>();
        }
    }
}