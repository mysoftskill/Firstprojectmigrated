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

    public class EmptyEventWriterTest
    {
        [Theory(DisplayName = "When WriteEvent is called, then log the data as a warning with the data type."), AutoMoqData]
        public void VerifyEmptyEventData(
            Mock<ILogger<Base>> log,
            Mock<PrivacyServices.Common.Azure.ILogger> logger,
            SessionProperties properties,
            int data, 
            EventLevel level, 
            EventOptions options)
        {
            var emptyEventWriter = new EmptyEventWriter<int>(log.Object, logger.Object, properties);
            emptyEventWriter.WriteEvent(nameof(EmptyEventWriterTest), data, level, options);

            Expression<Func<WriterTypeNotRegisteredEvent, bool>> verify = e => e.writerType == "System.Int32";
            log.Verify(m => m.Write(properties, It.Is(verify), EventLevel.Warning, options, null));
        }

        [Theory(DisplayName = "When WriteEvent is called with null data, then do not fail."), AutoMoqData]
        public void VerifyEmptySessionNullData(
            Mock<ILogger<Base>> log, 
            Mock<PrivacyServices.Common.Azure.ILogger> logger,
            SessionProperties properties,
            SessionStatus status, 
            string name,
            long totalMilliseconds,
            string cv,
            int data)
        {
            var emptyEventWriter = new EmptySessionWriter<int>(log.Object, logger.Object, properties);
            emptyEventWriter.WriteDone(status, name, totalMilliseconds, cv, data);

            Expression<Func<WriterTypeNotRegisteredEvent, bool>> verify = e => e.writerType == "System.Int32";
            log.Verify(m => m.Write(properties, It.Is(verify), EventLevel.Warning, EventOptions.None, cv));
        }

        [Theory(DisplayName = "Verify AutoFac registration for EmptyEventWriter"), AutoMoqData]
        public void VerifyEmptyEventRegistration(Mock<ILogger<Base>> log, Mock<PrivacyServices.Common.Azure.ILogger> logger, int data)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(new Autofac.InstrumentationModule());
            containerBuilder.RegisterInstance(log.Object).As<ILogger<Base>>();
            containerBuilder.RegisterInstance(logger.Object).As<PrivacyServices.Common.Azure.ILogger>();

            var container = containerBuilder.Build();
            var eventWriterFactory = container.Resolve<IEventWriterFactory>();

            eventWriterFactory.WriteEvent(nameof(EmptyEventWriterTest), data);

            log.Verify(m => m.Write(It.IsAny<SessionProperties>(), It.IsAny<WriterTypeNotRegisteredEvent>(), EventLevel.Warning, EventOptions.None, null));
        }
    }
}
