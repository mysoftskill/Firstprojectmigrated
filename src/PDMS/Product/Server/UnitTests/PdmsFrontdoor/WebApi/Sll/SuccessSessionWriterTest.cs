namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.UnitTest
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Linq.Expressions;

    using global::Autofac;

    using Microsoft.PrivacyServices.DataManagement.Common.Autofac;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Owin;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi;
    using Microsoft.PrivacyServices.Testing;
    using Microsoft.Telemetry;

    using Moq;
    using Xunit;

    using static Microsoft.AzureAd.Icm.Types.IcmConstants;

    public class SuccessSessionWriterTest
    {
        [Theory(DisplayName = "When a session writer is resolved for OperationMetadata, then return SuccessEventWriter"), AutoMoqData]
        public void VerifyAutofacRegistration(ISllConfig sllConfig)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(new WebApiModule<OwinRequestContextFactory>());
            containerBuilder.RegisterModule(new InstrumentationModule());
            containerBuilder.RegisterInstance(sllConfig);
            var container = containerBuilder.Build();

            var successEventWriter = container.Resolve<ISessionWriter<OperationMetadata>>();
            Assert.Equal(typeof(SuccessSessionWriter), successEventWriter.GetType());
        }

        [Theory(DisplayName = "When success event is written, then log api properties."), AutoMoqData]
        public void VerifySuccessSessionFields(Mock<ILogger<Base>> log, SessionProperties properties, string name, long duration, OperationMetadata metadata, string cv)
        {
            var writer = new SuccessSessionWriter(log.Object, properties);
            writer.WriteDone(SessionStatus.Success, name, duration, cv, metadata);

            Func<IncomingApiSuccessEvent, bool> verifyInternal = d =>
            {
                Assert.Equal(metadata.CallerIpAddress, d.baseData.callerIpAddress);
                Assert.Equal(metadata.ProtocolStatusCode, d.baseData.serviceErrorCode);
                Assert.Equal(metadata.Protocol, d.baseData.protocol);
                Assert.Equal(metadata.RequestMethod, d.baseData.requestMethod);
                Assert.Equal(metadata.RequestSizeBytes, d.baseData.requestSizeBytes);
                Assert.Equal(metadata.ResponseContentType, d.baseData.responseContentType);
                Assert.Equal(metadata.ProtocolStatusCode.ToString(), d.baseData.protocolStatusCode);
                Assert.Equal(metadata.TargetUri, d.baseData.targetUri);
                Assert.Equal(name, d.baseData.operationName);
                Assert.Equal(duration, d.baseData.latencyMs);
                Assert.Equal(Ms.Qos.ServiceRequestStatus.Success, d.baseData.requestStatus);
                Assert.True(d.baseData.succeeded);
                return true;
            };
            Expression<Func<IncomingApiSuccessEvent, bool>> verify = d => verifyInternal(d);

            log.Verify(m => m.Write(properties, It.Is(verify), EventLevel.Informational, EventOptions.None, cv));
        }
    }
}