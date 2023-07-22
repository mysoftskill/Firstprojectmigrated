namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation.UnitTest
{
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Moq.Protected;

    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;

    using Xunit;

    public class BaseSessionTest
    {
        [Theory(DisplayName = "When done is called then ensure duration is calculated."), AutoMoqData]
        public void VerifyElapsedTime([Frozen] SessionStatus status, [Frozen] string apiName, Mock<BaseSession> session)
        {
            this.Delay();
            session.Object.Done(status);
            session.Protected().Verify("Done", Times.Once(), status, apiName, ItExpr.Is<long>(v => v > 0));
        }

        [Theory(DisplayName = "When name is changed, then ensure new value is used."), AutoMoqData]
        public void VerifySetName([Frozen] SessionStatus status, [Frozen] string originalName, Mock<BaseSession> session)
        {
            this.VerifyElapsedTime(status, originalName, session); // Ensures original name is logged first.

            session.Object.SetName("newName");
            session.Object.Done(status);
            session.Protected().Verify("Done", Times.Once(), status, "newName", ItExpr.Is<long>(v => v > 0));
        }

        [Theory(DisplayName = "When stop is called, then ensure done uses the same elapsed value."), AutoMoqData]
        public void VerifyStop([Frozen] SessionStatus status, [Frozen] string originalName, Mock<BaseSession> session)
        {
            this.Delay();
            var elapsed = session.Object.Stop();

            session.Object.Done(status);
            session.Protected().Verify("Done", Times.Once(), status, originalName, elapsed);
        }

        [Theory(DisplayName = "When done is called with a result and no sessionWriter matches then fallback to base method."), AutoMoqData]
        public void VerifyFallbackIfNoSessionWriter([Frozen] SessionStatus status, [Frozen] string apiName, Mock<BaseSession> session)
        {
            this.Delay();
            session.Object.Done(status, string.Empty);
            session.Protected().Verify("Done", Times.Once(), status, apiName, ItExpr.Is<long>(v => v > 0));
        }

        [Theory(DisplayName = "When done is called with a result and a sessionWriter matches then call the writer method."), AutoMoqData]
        public void VerifySessionWriterCalled([Frozen] SessionStatus status, [Frozen] string apiName, Fixture fixture)
        {
            // Arrange.
            // Due to generics, must register this manually.
            var sessionWriter = fixture.Freeze<Mock<ISessionWriter<int>>>();
            var sessionCreation = sessionWriter.Object;

            var sessionWriterFactory = new Mock<ISessionWriterFactory>();
            sessionWriterFactory.Setup(m => m.TryCreate(It.IsAny<SessionType>(), out sessionCreation)).Returns(true);
            fixture.Inject(sessionWriterFactory.Object);

            var session = fixture.Freeze<Mock<BaseSession>>();
            var value = fixture.Create<int>();

            // Act.
            this.Delay();
            session.Object.Done(status, value);

            // Assert.
            session.Protected().Verify("Done", Times.Never(), ItExpr.IsAny<SessionStatus>(), ItExpr.IsAny<string>(), ItExpr.IsAny<long>());
            sessionWriter.Verify(m => m.WriteDone(SessionStatus.Success, apiName, It.Is<long>(v => v > 0), session.Object.CorrelationVector, value), Times.Once());
        }

        [Theory(DisplayName = "When outgoing session is logged, then increment cv."), AutoMoqData]
        public void VerifyOutgoingCv([Frozen] SessionStatus status, [Frozen] Mock<ICorrelationVector> cv, Fixture fixture)
        {
            var counter = 0;
            cv.Setup(m => m.Increment()).Returns((counter++).ToString());
            cv.Setup(m => m.Get()).Returns(counter.ToString());

            // Arrange.
            fixture.Inject(SessionType.Outgoing);
            var session = fixture.Freeze<Mock<BaseSession>>();
            var value = fixture.Create<int>();

            // Act.
            session.Object.Done(status, value);

            // Assert.
            session.Protected().Verify("Done", Times.Never(), ItExpr.IsAny<SessionStatus>(), "1", ItExpr.IsAny<long>());
        }

        [Theory(DisplayName = "When incoming session is logged, then do not increment cv."), AutoMoqData]
        public void VerifyIncomingCv([Frozen] SessionStatus status, [Frozen] Mock<ICorrelationVector> cv, Fixture fixture)
        {
            var counter = 0;
            cv.Setup(m => m.Increment()).Returns((counter++).ToString());
            cv.Setup(m => m.Get()).Returns(counter.ToString());

            // Arrange.
            fixture.Inject(SessionType.Outgoing);
            var session = fixture.Freeze<Mock<BaseSession>>();
            var value = fixture.Create<int>();

            // Act.
            session.Object.Done(status, value);

            // Assert.
            session.Protected().Verify("Done", Times.Never(), ItExpr.IsAny<SessionStatus>(), "0", ItExpr.IsAny<long>());
        }

        private void Delay()
        {
            System.Threading.Thread.Sleep(1); // Ensure we have some delay in the call.
        }
    }
}