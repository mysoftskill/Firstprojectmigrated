namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation.UnitTest
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Linq.Expressions;

    using global::Autofac;

    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.Testing;
    using Microsoft.Telemetry;

    using Moq;

    using Xunit;

    public class StringEventWriterTest
    {
        [Theory(DisplayName = "Verify TraceEvent data fields."), AutoMoqData]
        public void VerifyEventFields(Mock<ILogger<Base>> log, Mock<PrivacyServices.Common.Azure.ILogger> logger, SessionProperties properties)
        {
            var eventWriter = new StringEventWriter(log.Object, logger.Object, properties);
            eventWriter.WriteEvent(nameof(StringEventWriterTest), "data");

            Expression<Func<TraceEvent, bool>> verify = e => e.message == "data";

            log.Verify(m => m.Write(properties, It.Is(verify), EventLevel.Informational, EventOptions.None, null));
        }

        [Theory(DisplayName = "Verify Trace() logs a TraceEvent as verbose data."), AutoMoqData]
        public void VerifyTraceRegistration(Mock<ILogger<Base>> log, Mock<PrivacyServices.Common.Azure.ILogger> logger, string message)
        {
            // Arrange
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(new Autofac.InstrumentationModule());
            containerBuilder.RegisterInstance(log.Object).As<ILogger<Base>>();
            containerBuilder.RegisterInstance(logger.Object).As<PrivacyServices.Common.Azure.ILogger>();

            var container = containerBuilder.Build();
            var eventWriterFactory = container.Resolve<IEventWriterFactory>();

            // Act
            eventWriterFactory.Trace(nameof(StringEventWriterTest), message);

            // Assert
            Expression<Func<TraceEvent, bool>> verify = e => e.message == message;
            log.Verify(m => m.Write(It.IsAny<SessionProperties>(), It.Is(verify), EventLevel.Informational, EventOptions.None, null));
        }
    }
}
