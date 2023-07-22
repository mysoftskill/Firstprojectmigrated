namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.UnitTest
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Linq.Expressions;

    using global::Autofac;

    using Microsoft.PrivacyServices.DataManagement.Common.Autofac;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Owin;
    using Microsoft.PrivacyServices.Testing;
    using Microsoft.Telemetry;

    using Moq;
    using Xunit;

    public class ServiceErrorSessionWriterTest
    {
        [Theory(DisplayName = "When a session writer is resolved for Tuple<OperationMetadata, ServiceError>, then return ServiceErrorSessionWriter"), AutoMoqData]
        public void VerifyAutofacRegistration(ISllConfig sllConfig)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(new WebApiModule<OwinRequestContextFactory>());
            containerBuilder.RegisterModule(new InstrumentationModule());
            containerBuilder.RegisterInstance(sllConfig);
            var container = containerBuilder.Build();

            var eventWriter = container.Resolve<ISessionWriter<Tuple<OperationMetadata, ServiceError>>>();
            Assert.Equal(typeof(ServiceErrorSessionWriter), eventWriter.GetType());
        }

        [Theory(DisplayName = "When service error event with an inner error is written, then log api properties."), AutoMoqData]
        public void VerifyServiceErrorFieldsForInnerError(Mock<ILogger<Base>> log, SessionProperties properties, string name, long duration, OperationMetadata metadata, string cv)
        {
            var error = new InvalidArgumentError("param", "someValue").ServiceError;
            var writer = new ServiceErrorSessionWriter(log.Object, properties);
            writer.WriteDone(SessionStatus.Error, name, duration, cv, Tuple.Create(metadata, error));

            Func<IncomingApiServiceErrorEvent, bool> verifyInternal = d =>
            {
                Assert.Equal(error.ToString(), d.baseData.protocolStatusCode);
                Assert.Equal(error.Message, d.message);
                Assert.Equal(error.Target, d.target);
                Assert.True(d.innerError.Contains("someValue"), "d.innerError.Contains(\"someValue\")");
                Assert.Equal(metadata.CallerIpAddress, d.baseData.callerIpAddress);
                Assert.Equal(metadata.ProtocolStatusCode, d.baseData.serviceErrorCode);
                Assert.Equal(metadata.Protocol, d.baseData.protocol);
                Assert.Equal(metadata.RequestMethod, d.baseData.requestMethod);
                Assert.Equal(metadata.RequestSizeBytes, d.baseData.requestSizeBytes);
                Assert.Equal(metadata.ResponseContentType, d.baseData.responseContentType);
                Assert.Equal(metadata.TargetUri, d.baseData.targetUri);
                Assert.Equal(name, d.baseData.operationName);
                Assert.Equal(duration, d.baseData.latencyMs);
                Assert.Equal(Ms.Qos.ServiceRequestStatus.CallerError, d.baseData.requestStatus);
                Assert.False(d.baseData.succeeded);
                return true;
            };
            Expression<Func<IncomingApiServiceErrorEvent, bool>> verify = d => verifyInternal(d);

            log.Verify(m => m.Write(properties, It.Is(verify), EventLevel.Warning, EventOptions.None, cv));
        }

        [Theory(DisplayName = "When service error event with details is written, then log api properties."), AutoMoqData]
        public void VerifyServiceErrorFieldsForDetails(Mock<ILogger<Base>> log, SessionProperties properties, string name, long duration, OperationMetadata metadata, string cv)
        {
            var innerError = new NullArgumentError("param");
            var error = new MultiBadArgumentError(new[] { innerError }).ServiceError;
            var writer = new ServiceErrorSessionWriter(log.Object, properties);
            writer.WriteDone(SessionStatus.Error, name, duration, cv, Tuple.Create(metadata, error));

            Func<IncomingApiServiceErrorEvent, bool> verifyInternal = d =>
            {
                Assert.Equal(error.ToString(), d.baseData.protocolStatusCode);
                Assert.Equal(error.Message, d.message);
                Assert.Equal(error.Target, d.target);
                Assert.Single(d.details);
                Assert.Equal(innerError.ToDetail().Code, d.details[0].code);
                Assert.Equal(innerError.ToDetail().Message, d.details[0].message);
                Assert.Equal(innerError.ToDetail().Target, d.details[0].target);
                return true;
            };
            Expression<Func<IncomingApiServiceErrorEvent, bool>> verify = d => verifyInternal(d);

            log.Verify(m => m.Write(properties, It.Is(verify), EventLevel.Warning, EventOptions.None, cv));
        }

        [Theory(DisplayName = "When service fault event is written, then log api properties."), AutoMoqData]
        public void VerifyServiceFaultFields(Mock<ILogger<Base>> log, SessionProperties properties, string name, long duration, OperationMetadata metadata, string cv)
        {
            var exn = new NullReferenceException("message", new Exception());
            var error = new ServiceFault(exn).ServiceError;
            var exnError = error.InnerError as ServiceFault.ExceptionError;
            var writer = new ServiceErrorSessionWriter(log.Object, properties);
            writer.WriteDone(SessionStatus.Fault, name, duration, cv, Tuple.Create(metadata, error));

            Func<IncomingApiServiceFaultEvent, bool> verifyInternal = d =>
            {
                Assert.Equal(error.ToString(), d.baseData.protocolStatusCode);
                Assert.Equal(error.Message, d.message);
                Assert.Equal(error.Target, d.target);
                Assert.Equal(exnError.StackTrace, d.stackTrace);
                Assert.Equal(exnError.Code, d.innerException.code);
                Assert.Equal(exnError.Message, d.innerException.message);
                Assert.Equal(exnError.InnerException.Code, d.innerException.innerException.code);
                Assert.Equal(exnError.InnerException.Message, d.innerException.innerException.message);

                // Standard properties.
                Assert.Equal(metadata.CallerIpAddress, d.baseData.callerIpAddress);
                Assert.Equal(metadata.ProtocolStatusCode, d.baseData.serviceErrorCode);
                Assert.Equal(metadata.Protocol, d.baseData.protocol);
                Assert.Equal(metadata.RequestMethod, d.baseData.requestMethod);
                Assert.Equal(metadata.RequestSizeBytes, d.baseData.requestSizeBytes);
                Assert.Equal(metadata.ResponseContentType, d.baseData.responseContentType);
                Assert.Equal(metadata.TargetUri, d.baseData.targetUri);
                Assert.Equal(name, d.baseData.operationName);
                Assert.Equal(duration, d.baseData.latencyMs);
                Assert.Equal(Ms.Qos.ServiceRequestStatus.ServiceError, d.baseData.requestStatus);
                Assert.False(d.baseData.succeeded);
                return true;
            };
            Expression<Func<IncomingApiServiceFaultEvent, bool>> verify = d => verifyInternal(d);

            log.Verify(m => m.Write(properties, It.Is(verify), EventLevel.Error, EventOptions.None, cv));
        }
    }
}