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

    //using Sll = Microsoft.PrivacyServices.Instrumentation.Sll.Autofac;

    public class InconsistentStateEventWriterTest
    {
        [Theory(DisplayName = "Verify InconsistentState sll event."), AutoMoqData]
        public void VerifyConversion(Mock<ILogger<Base>> log, InconsistentState data)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new Autofac.InstrumentationModule());
            builder.RegisterInstance(log.Object);

            var eventWriterFactory = builder.Build().Resolve<IEventWriterFactory>();            
            eventWriterFactory.WriteEvent(nameof(InconsistentStateEventWriterTest), data, EventLevel.Warning);

            Action<InconsistentStateEvent> verify = v =>
            {
                Assert.Equal(Serialization.Serialize(data.Data), v.data);
                Assert.Equal(data.Message, v.message);
                Assert.Equal(data.Type.ToString(), v.instanceType);
            };

            log.Verify(m => m.Write(It.IsAny<SessionProperties>(), Is.Value(verify), EventLevel.Warning, EventOptions.None, It.IsAny<string>()), Times.Once());
        }
    }
}