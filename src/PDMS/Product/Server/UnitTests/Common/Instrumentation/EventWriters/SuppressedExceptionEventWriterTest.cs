namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation.UnitTest
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Threading.Tasks;

    using Configuration;

    using global::Autofac;

    using Microsoft.PrivacyServices.Testing;
    using Microsoft.Telemetry;

    using Moq;

    using Ploeh.AutoFixture.Xunit2;

    using Xunit;

    public class SuppressedExceptionEventWriterTest
    {
        [Theory(DisplayName = "Verify SuppressedExceptionEvent data fields."), AutoMoqData]
        public void VerifyEventFields(
            [Frozen] Mock<ILogger<Base>> log,
            [Frozen] SessionProperties properties,
            SuppressedExceptionEventWriter eventWriter,
            SuppressedException data)
        {
            eventWriter.WriteEvent(nameof(SuppressedExceptionEventWriterTest), data);

            Action<SuppressedExceptionEvent> verify = e =>
            {
                Assert.Equal(data.Name, e.action);
                Assert.Equal(data.Value.GetName(), e.code);
                Assert.Equal(data.Value.Message, e.message);
                Assert.Equal(data.Value.StackTrace, e.stackTrace);
            };

            log.Verify(m => m.Write(properties, Is.Value(verify), EventLevel.Informational, EventOptions.None, null));
        }

        [Theory(DisplayName = "Verify SuppressException() logs a SuppressedExceptionEvent as a warning data."), AutoMoqData]
        public async Task VerifyTraceRegistration(Mock<ILogger<Base>> log, Mock<PrivacyServices.Common.Azure.ILogger> logger, Exception data)
        {
            // Arrange
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(new Autofac.InstrumentationModule());
            containerBuilder.RegisterInstance(log.Object).As<ILogger<Base>>();
            containerBuilder.RegisterInstance(logger.Object).As<PrivacyServices.Common.Azure.ILogger>();

            var container = containerBuilder.Build();
            var eventWriterFactory = container.Resolve<IEventWriterFactory>();

            // Act
            await eventWriterFactory.SuppressExceptionAsync(nameof(SuppressedExceptionEventWriterTest), "action", () => Task.FromException(data)).ConfigureAwait(false);

            // Assert
            Action<SuppressedExceptionEvent> verify = e => Assert.Equal("action", e.action);
            log.Verify(m => m.Write(It.IsAny<SessionProperties>(), Is.Value(verify), EventLevel.Warning, EventOptions.None, null));
        }
    }
}
