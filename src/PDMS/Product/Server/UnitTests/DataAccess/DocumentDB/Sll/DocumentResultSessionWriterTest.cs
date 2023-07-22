namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.UnitTest
{
    using System;
    using System.Diagnostics.Tracing;

    using global::Autofac;

    using Microsoft.PrivacyServices.DataManagement.Common.Autofac;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac;
    using Microsoft.PrivacyServices.Testing;
    using Microsoft.Telemetry;

    using Moq;

    using Xunit;

    public class DocumentResultSessionWriterTest
    {
        [Theory(DisplayName = "Verify DocumentResult sll event."), AutoMoqData]
        public void VerifyConversion(Mock<ILogger<Base>> log, string activityId, double requestCharge, string uri)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new DataAccessModule());
            builder.RegisterModule(new InstrumentationModule());
            builder.RegisterInstance(log.Object);

            var sessionWriterFactory = builder.Build().Resolve<ISessionWriterFactory>();
            
            var data = new Autofac.Sll.DocumentResult();
            data.RequestCharge = requestCharge;
            data.ActivityId = activityId;
            data.RequestUri = uri;
            
            sessionWriterFactory.WriteDone(SessionType.Outgoing, SessionStatus.Success, string.Empty, 0, string.Empty, data);

            Action<DocumentClientSuccessEvent> verify = v =>
            {
                Assert.Equal(uri, v.baseData.targetUri);
                Assert.Equal(activityId, v.activityId);
                Assert.Equal(requestCharge, v.requestCharge);
            };

            log.Verify(m => m.Write(It.IsAny<SessionProperties>(), Is.Value(verify), EventLevel.Informational, EventOptions.None, It.IsAny<string>()), Times.Once());
        }
    }
}