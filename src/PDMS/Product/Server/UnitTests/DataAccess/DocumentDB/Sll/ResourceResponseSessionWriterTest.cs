namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.UnitTest
{
    using System;
    using System.Collections.Specialized;
    using System.Diagnostics.Tracing;

    using global::Autofac;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.PrivacyServices.DataManagement.Common.Autofac;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac;
    using Microsoft.PrivacyServices.Testing;
    using Microsoft.Telemetry;

    using Moq;

    using Xunit;

    public class ResourceResponseSessionWriterTest
    {
        [Theory(DisplayName = "Verify ResourceResponse sll event."), AutoMoqData]
        public void VerifyConversion(Mock<ILogger<Base>> log, TestObject value, string activityId, double requestCharge, string uri)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new DataAccessModule());
            builder.RegisterModule(new InstrumentationModule());
            builder.RegisterInstance(log.Object);

            var sessionWriterFactory = builder.Build().Resolve<ISessionWriterFactory>();

            var headers = new NameValueCollection();
            headers.Add("x-ms-activity-id", activityId);
            headers.Add("x-ms-request-charge", requestCharge.ToString());

            var response = ResourceResponseModule.Create<Document>(value, headers);
            
            var data = new Tuple<string, ResourceResponse<Document>>(uri, response);

            sessionWriterFactory.WriteDone(SessionType.Outgoing, SessionStatus.Success, string.Empty, 0, string.Empty, data);

            Action<DocumentClientSuccessEvent> verify = v => 
            {
                Assert.Equal(uri, v.baseData.targetUri);
                Assert.Equal(activityId, v.activityId);
                Assert.Equal(requestCharge, v.requestCharge);
            };

            log.Verify(m => m.Write(It.IsAny<SessionProperties>(), Is.Value(verify), EventLevel.Informational, EventOptions.None, It.IsAny<string>()), Times.Once());
        }

        public class TestObject
        {
            public string Value { get; set; }
        }
    }
}