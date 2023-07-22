namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation.UnitTest
{
    using System;
    using System.Diagnostics.Tracing;

    using global::Autofac;

    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.Testing;
    using Microsoft.Telemetry;

    using Moq;

    using Xunit;

    public class ExceptionSessionWriterTest
    {
        [Theory(DisplayName = "Verify exception data is logged for incoming calls."), AutoMoqData]
        public void VerifyExceptionFieldsIncoming(Mock<ILogger<Base>> log, SessionProperties properties, Exception innerExn, string cv)
        {
            var exn = new NullReferenceException("message", innerExn);
            var writer = new ExceptionSessionWriter(log.Object, properties, SessionType.Incoming);
            writer.WriteFault(string.Empty, 0, cv, exn);
            
            Action<BaseIncomingExceptionEvent> verify = e =>
            {
                Assert.Equal("System.NullReferenceException", e.baseData.protocolStatusCode);
                Assert.Equal(exn.Message, e.message);
                Assert.Equal(exn.StackTrace, e.stackTrace);
                Assert.Equal("System.Exception", e.innerException.code);
                Assert.Equal(innerExn.Message, e.innerException.message);
                Assert.Equal("Incoming", e.component);
            };

            log.Verify(m => m.Write(properties, Is.Value(verify), EventLevel.Error, EventOptions.None, cv), Times.Once());
        }

        [Theory(DisplayName = "Verify api names for incoming calls.")]
        [InlineAutoMoqData("Partner.Api", "Partner.Api")]
        [InlineAutoMoqData("", "Unknown")]
        [InlineAutoMoqData("  ", "Unknown")]
        [InlineAutoMoqData(null, "Unknown")]
        public void VerifyIncomingNames(
            string sessionName,
            string finalApiName,
            Mock<ILogger<Base>> log,
            SessionProperties properties, 
            string cv)
        {
            var writer = new ExceptionSessionWriter(log.Object, properties, SessionType.Incoming);
            writer.WriteSuccess(sessionName, 0, cv, null);

            Action<BaseIncomingSucessEvent> verify = e =>
                Assert.Equal(finalApiName, e.baseData.operationName);
            
            log.Verify(m => m.Write(properties, Is.Value(verify), EventLevel.Informational, EventOptions.None, cv), Times.Once());
        }

        [Theory(DisplayName = "Verify exception data is logged for internal calls."), AutoMoqData]
        public void VerifyExceptionFieldsInternal(Mock<ILogger<Base>> log, SessionProperties properties, Exception innerExn, string cv)
        {
            var exn = new NullReferenceException("message", innerExn);
            var writer = new ExceptionSessionWriter(log.Object, properties, SessionType.Internal);
            writer.WriteFault(string.Empty, 0, cv, exn);

            Action<BaseIncomingExceptionEvent> verify = e =>
            {
                Assert.Equal("System.NullReferenceException", e.baseData.protocolStatusCode);
                Assert.Equal(exn.Message, e.message);
                Assert.Equal(exn.StackTrace, e.stackTrace);
                Assert.Equal("System.Exception", e.innerException.code);
                Assert.Equal(innerExn.Message, e.innerException.message);
                Assert.Equal("Internal", e.component);
            };

            log.Verify(m => m.Write(properties, Is.Value(verify), EventLevel.Error, EventOptions.None, cv), Times.Once());
        }

        [Theory(DisplayName = "Verify api names for internal calls.")]
        [InlineAutoMoqData("Partner.Api", "_Internal.Partner.Api")]
        [InlineAutoMoqData("", "_Internal.Unknown")]
        [InlineAutoMoqData("  ", "_Internal.Unknown")]
        [InlineAutoMoqData(null, "_Internal.Unknown")]
        public void VerifyInternalNames(
            string sessionName,
            string finalApiName,
            Mock<ILogger<Base>> log,
            SessionProperties properties,
            string cv)
        {
            var writer = new ExceptionSessionWriter(log.Object, properties, SessionType.Internal);
            writer.WriteSuccess(sessionName, 0, cv, null);

            Action<BaseIncomingSucessEvent> verify = e =>
                Assert.Equal(finalApiName, e.baseData.operationName);

            log.Verify(m => m.Write(properties, Is.Value(verify), EventLevel.Informational, EventOptions.None, cv), Times.Once());
        }

        [Theory(DisplayName = "Verify exception data is logged for outgoing calls."), AutoMoqData]
        public void VerifyExceptionFieldsOutgoing(Mock<ILogger<Base>> log, SessionProperties properties, Exception innerExn, string cv)
        {
            var exn = new NullReferenceException("message", innerExn);
            var writer = new ExceptionSessionWriter(log.Object, properties, SessionType.Outgoing);
            writer.WriteFault(string.Empty, 0, cv, exn);

            Action<BaseOutgoingExceptionEvent> verify = e =>
            {
                Assert.Equal("System.NullReferenceException", e.baseData.protocolStatusCode);
                Assert.Equal(exn.Message, e.message);
                Assert.Equal(exn.StackTrace, e.stackTrace);
                Assert.Equal("System.Exception", e.innerException.code);
                Assert.Equal(innerExn.Message, e.innerException.message);
            };

            log.Verify(m => m.Write(properties, Is.Value(verify), EventLevel.Error, EventOptions.None, cv), Times.Once());
        }

        [Theory(DisplayName = "Verify partner and api names for outgoing calls.")]
        [InlineAutoMoqData("Partner.Api", "Partner", "Api")]
        [InlineAutoMoqData("Partner.Api.Value", "Partner", "Api.Value")]
        [InlineAutoMoqData(".Api", "Unknown", ".Api")]
        [InlineAutoMoqData("Partner.", "Unknown", "Partner.")]
        [InlineAutoMoqData("", "Unknown", "Unknown")]
        [InlineAutoMoqData("  ", "Unknown", "Unknown")]
        [InlineAutoMoqData(null, "Unknown", "Unknown")]
        public void VerifyOutgoingNames(
            string sessionName,
            string finalPartnerName,
            string finalApiName,
            Mock<ILogger<Base>> log,
            SessionProperties properties,
            string cv)
        {
            var writer = new ExceptionSessionWriter(log.Object, properties, SessionType.Outgoing);
            writer.WriteSuccess(sessionName, 0, cv, null);

            Action<BaseOutgoingSucessEvent> verify = e =>
            {
                Assert.Equal(finalPartnerName, e.baseData.dependencyName);
                Assert.Equal(finalApiName, e.baseData.dependencyOperationName);
            };
            
            log.Verify(m => m.Write(properties, Is.Value(verify), EventLevel.Informational, EventOptions.None, cv), Times.Once());
        }

        [Theory(DisplayName = "When the exception object is null, then log 'null' for incoming calls."), AutoMoqData]
        public void VerifyNullChecksIncoming(Mock<ILogger<Base>> log, SessionProperties properties, string cv)
        {
            var writer = new ExceptionSessionWriter(log.Object, properties, SessionType.Incoming);
            writer.WriteError(string.Empty, 0, cv, null);
            
            Action<BaseIncomingExceptionEvent> verify = e =>
                Assert.Equal("null", e.baseData.protocolStatusCode);

            log.Verify(m => m.Write(properties, Is.Value(verify), EventLevel.Warning, EventOptions.None, cv), Times.Once());
        }

        [Theory(DisplayName = "When the exception object is null, then log 'null' for outgoing calls."), AutoMoqData]
        public void VerifyNullChecksOutgoing(Mock<ILogger<Base>> log, SessionProperties properties, string cv)
        {
            var writer = new ExceptionSessionWriter(log.Object, properties, SessionType.Outgoing);
            writer.WriteError(string.Empty, 0, cv, null);

            Action<BaseOutgoingExceptionEvent> verify = e =>
                Assert.Equal("null", e.baseData.protocolStatusCode);

            log.Verify(m => m.Write(properties, Is.Value(verify), EventLevel.Warning, EventOptions.None, cv), Times.Once());
        }

        [Theory(DisplayName = "Verify AutoFac registration for ExceptionSessionWriter.")]
        [InlineAutoMoqData(SessionType.Incoming, SessionStatus.Success, 0, EventLevel.Informational)]
        [InlineAutoMoqData(SessionType.Incoming, SessionStatus.Error, 1, EventLevel.Warning)]
        [InlineAutoMoqData(SessionType.Incoming, SessionStatus.Fault, 1, EventLevel.Error)]
        [InlineAutoMoqData(SessionType.Outgoing, SessionStatus.Success, 2, EventLevel.Informational)]
        [InlineAutoMoqData(SessionType.Outgoing, SessionStatus.Error, 3, EventLevel.Warning)]
        [InlineAutoMoqData(SessionType.Outgoing, SessionStatus.Fault, 3, EventLevel.Error)]
        public void VerifyAutofacRegistration(SessionType sessionType, SessionStatus status, int verify, EventLevel level, Mock<ILogger<Base>> log)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(new Autofac.InstrumentationModule());
            containerBuilder.RegisterInstance(log.Object).As<ILogger<Base>>();

            var container = containerBuilder.Build();
            var sessionFactory = container.Resolve<ISessionFactory>();
            var session = sessionFactory.StartSession(string.Empty, sessionType);
            session.Done(status);

            // Use a mapping to identify the proper class type.
            switch (verify)
            {
                case 0:
                    log.Verify(m => m.Write(It.IsAny<SessionProperties>(), It.IsAny<BaseIncomingSucessEvent>(), level, EventOptions.None, session.CorrelationVector), Times.Once());
                    break;
                case 1:
                    log.Verify(m => m.Write(It.IsAny<SessionProperties>(), It.IsAny<BaseIncomingExceptionEvent>(), level, EventOptions.None, session.CorrelationVector), Times.Once());
                    break;
                case 2:
                    log.Verify(m => m.Write(It.IsAny<SessionProperties>(), It.IsAny<BaseOutgoingSucessEvent>(), level, EventOptions.None, session.CorrelationVector), Times.Once());
                    break;
                case 3:
                    log.Verify(m => m.Write(It.IsAny<SessionProperties>(), It.IsAny<BaseOutgoingExceptionEvent>(), level, EventOptions.None, session.CorrelationVector), Times.Once());
                    break;
            }
        }
    }
}
