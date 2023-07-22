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

    public class EmptySessionWriterTest
    {
        [Theory(DisplayName = "When WriteSession is called, then log the data as a warning with the data type."), AutoMoqData]
        public void VerifyEmptySessionData(Mock<ILogger<Base>> log, Mock<PrivacyServices.Common.Azure.ILogger> logger, SessionProperties properties, int data, SessionStatus status, string name, long totalMilliseconds, string cv)
        {
            var emptySessionWriter = new EmptySessionWriter<int>(log.Object, logger.Object, properties);
            emptySessionWriter.WriteDone(status, name, totalMilliseconds, cv, data);

            Expression<Func<WriterTypeNotRegisteredEvent, bool>> verify = e => e.writerType == "System.Int32";
            log.Verify(m => m.Write(properties, It.Is(verify), EventLevel.Warning, EventOptions.None, cv));
        }

        [Theory(DisplayName = "When WriteSession is called with null data, then do not fail."), AutoMoqData]
        public void VerifyEmptySessionNullData(Mock<ILogger<Base>> log, Mock<PrivacyServices.Common.Azure.ILogger> logger, SessionProperties properties, SessionStatus status, string name, long totalMilliseconds, string cv)
        {
            var emptySessionWriter = new EmptySessionWriter<string>(log.Object, logger.Object, properties);
            emptySessionWriter.WriteDone(status, name, totalMilliseconds, cv, null);

            Expression<Func<WriterTypeNotRegisteredEvent, bool>> verify = e => e.writerType == "System.String";
            log.Verify(m => m.Write(properties, It.Is(verify), EventLevel.Warning, EventOptions.None, cv));
        }

        [Theory(DisplayName = "Verify AutoFac registration for EmptySessionWriter"), AutoMoqData]
        public void VerifyEmptySessionRegistration(Mock<ILogger<Base>> log, Mock<PrivacyServices.Common.Azure.ILogger> logger, int data, SessionStatus status, string name, long totalMilliseconds, SessionType sessionType, string cv)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(new Autofac.InstrumentationModule());
            containerBuilder.RegisterInstance(log.Object).As<ILogger<Base>>();
            containerBuilder.RegisterInstance(logger.Object).As<PrivacyServices.Common.Azure.ILogger>();

            var container = containerBuilder.Build();
            var sessionWriterFactory = container.Resolve<ISessionWriterFactory>();

            sessionWriterFactory.WriteDone(sessionType, status, name, totalMilliseconds, cv, data);

            log.Verify(m => m.Write(It.IsAny<SessionProperties>(), It.IsAny<WriterTypeNotRegisteredEvent>(), EventLevel.Warning, EventOptions.None, cv));
        }
    }
}
