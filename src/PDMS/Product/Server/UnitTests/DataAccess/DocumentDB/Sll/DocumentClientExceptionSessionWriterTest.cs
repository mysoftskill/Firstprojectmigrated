namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.UnitTest
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Net;

    using global::Autofac;

    using Microsoft.Azure.Documents;
    using Microsoft.PrivacyServices.DataManagement.Common.Autofac;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac;
    using Microsoft.PrivacyServices.Testing;
    using Microsoft.Telemetry;

    using Moq;

    using Xunit;

    public class DocumentClientExceptionSessionWriterTest
    {
        [Theory(DisplayName = "When a 4xx response is returned, log it as a caller error."), AutoMoqData]
        public void VerifyConversionForCallerError(Mock<ILogger<Base>> log, string uri)
        {
            var sessionWriterFactory = this.CreateSessionFactory(log);

            var response = DocumentClientExceptionModule.Create(HttpStatusCode.BadRequest);

            var data = new Tuple<string, DocumentClientException>(uri, response);

            sessionWriterFactory.WriteDone(SessionType.Outgoing, SessionStatus.Success, string.Empty, 0, string.Empty, data);

            Action<DocumentClientErrorEvent> verify = v =>
            {
                Assert.Equal(uri, v.baseData.targetUri);
                Assert.Equal(Ms.Qos.ServiceRequestStatus.CallerError, v.baseData.requestStatus);
            };

            log.Verify(m => m.Write(It.IsAny<SessionProperties>(), Is.Value(verify), EventLevel.Warning, EventOptions.None, It.IsAny<string>()), Times.Once());
        }

        [Theory(DisplayName = "When a 5xx response is returned, log it as a service error."), AutoMoqData]
        public void VerifyConversionForServiceError(Mock<ILogger<Base>> log, string uri)
        {
            var sessionWriterFactory = this.CreateSessionFactory(log);
            
            var response = DocumentClientExceptionModule.Create(HttpStatusCode.InternalServerError);

            var data = new Tuple<string, DocumentClientException>(uri, response);

            sessionWriterFactory.WriteDone(SessionType.Outgoing, SessionStatus.Success, string.Empty, 0, string.Empty, data);

            Action<DocumentClientErrorEvent> verify = v =>
            {
                Assert.Equal(uri, v.baseData.targetUri);
                Assert.Equal(Ms.Qos.ServiceRequestStatus.ServiceError, v.baseData.requestStatus);
            };

            log.Verify(m => m.Write(It.IsAny<SessionProperties>(), Is.Value(verify), EventLevel.Error, EventOptions.None, It.IsAny<string>()), Times.Once());
        }

        private ISessionWriterFactory CreateSessionFactory(Mock<ILogger<Base>> log)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new InstrumentationModule());
            builder.RegisterModule(new DataAccessModule());
            builder.RegisterInstance(log.Object);

            return builder.Build().Resolve<ISessionWriterFactory>();
        }
    }
}