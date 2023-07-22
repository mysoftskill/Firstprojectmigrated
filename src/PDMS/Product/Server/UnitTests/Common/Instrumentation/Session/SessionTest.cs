namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation.UnitTest
{
    using System;

    using Microsoft.PrivacyServices.Testing;

    using Moq;

    using Ploeh.AutoFixture.Xunit2;

    using Xunit;

    public class SessionTest
    {
        [Theory(DisplayName = "When session.Done is called, then use NullEvent to load a session writer."), AutoMoqData]
        public void VerifyWriteDone(Mock<ISessionWriterFactory> sessionWriterFactory, [Frozen] ISessionWriter<Exception> writer, SessionType sessionType, SessionProperties sessionProperties)
        {
            var session = new Session(sessionWriterFactory.Object, string.Empty, sessionType, sessionProperties);
            session.Done(SessionStatus.Error);

            var outValue = writer;
            sessionWriterFactory.Verify(m => m.TryCreate(sessionType, out outValue), Times.Once());
        }
    }
}
