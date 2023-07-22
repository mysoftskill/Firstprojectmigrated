namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation.UnitTest
{
    using Microsoft.PrivacyServices.Testing;

    using Moq;

    using Ploeh.AutoFixture;

    using Xunit;

    public class ISessionWriterFactoryTest
    {
        [Theory(DisplayName = "When sessionWriterFactory.WriteDone is called and a sessionWriter is registerd, then call its write function."), AutoMoqData]
        public void VerifyWriteDone_WriterFound(Fixture fixture, SessionStatus status, string apiName, long duration, int value, SessionType sessionType, string cv)
        {
            // Arrange.
            var sessionWriter = fixture.Create<Mock<ISessionWriter<int>>>();
            var sessionCreation = sessionWriter.Object;

            var sessionWriterFactory = new Mock<ISessionWriterFactory>();
            sessionWriterFactory.Setup(m => m.TryCreate(sessionType, out sessionCreation)).Returns(true);
            fixture.Inject(sessionWriterFactory.Object);

            // Act.
            sessionWriterFactory.Object.WriteDone(sessionType, status, apiName, duration, cv, value);

            // Assert.
            sessionWriter.Verify(m => m.WriteDone(status, apiName, duration, cv, value), Times.Once());
        }

        [Theory(DisplayName = "When sessionWriterFactory.WriteDone is called and no sessionWriter is registerd, then do not fail."), AutoMoqData]
        public void VerifyWriteDone_WriterNotFound(Fixture fixture, SessionStatus status, string apiName, long duration, int value, SessionType sessionType, string cv)
        {
            // Arrange.
            var sessionWriter = fixture.Create<Mock<ISessionWriter<int>>>();
            var sessionCreation = sessionWriter.Object;

            var sessionWriterFactory = new Mock<ISessionWriterFactory>();
            sessionWriterFactory.Setup(m => m.TryCreate(sessionType, out sessionCreation)).Returns(false);
            fixture.Inject(sessionWriterFactory.Object);

            // Act.
            sessionWriterFactory.Object.WriteDone(sessionType, status, apiName, duration, cv, value);

            // Assert.
            sessionWriter.Verify(m => m.WriteDone(status, apiName, duration, cv, value), Times.Never());
        }
    }
}
