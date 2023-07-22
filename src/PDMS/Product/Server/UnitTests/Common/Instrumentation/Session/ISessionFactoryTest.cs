namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation.UnitTest
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.Testing;

    using Moq;

    using Ploeh.AutoFixture.Xunit2;

    using Xunit;

    public class ISessionFactoryTest
    {
        [Theory(DisplayName = "When sessionFactory.Instrument is called and the operation succeeds then ensure Success."), AutoMoqData]
        public void VerifyInstrumentSuccess(
            ISessionFactory sessionFactory,
            string apiName,
            SessionType sessionType,
            string result,
            [Frozen] Mock<ISession> session)
        {
            sessionFactory.Instrument(apiName, sessionType, () => result);
            session.Verify(m => m.Done(SessionStatus.Success, result), Times.Once());
        }

        [Theory(DisplayName = "When sessionFactory.Instrument is called and the operation throws exception then ensure Fault."), AutoMoqData]
        public void VerifyInstrumentFault(
            ISessionFactory sessionFactory,
            string apiName,
            SessionType sessionType,
            InvalidOperationException result,
            [Frozen] Mock<ISession> session)
        {
            Func<object> action = () =>
            {
                throw result;
            };
            Assert.Throws<InvalidOperationException>(() => sessionFactory.Instrument<object>(apiName, sessionType, action));

            session.Verify(m => m.Done<Exception>(SessionStatus.Fault, result), Times.Once());
        }

        [Theory(DisplayName = "When sessionFactory.InstrumentAsync is called and the operation succeeds then ensure Success."), AutoMoqData]
        public async Task VerifyInstrumentAsyncSuccess(
            ISessionFactory sessionFactory,
            string apiName,
            SessionType sessionType,
            string result,
            [Frozen] Mock<ISession> session)
        {
            await sessionFactory.InstrumentAsync(apiName, sessionType, () => Task.FromResult(result)).ConfigureAwait(false);
            session.Verify(m => m.Done(SessionStatus.Success, result), Times.Once());
        }

        [Theory(DisplayName = "When sessionFactory.InstrumentAsync is called and the operation throws exception then ensure Fault."), AutoMoqData]
        public async Task VerifyInstrumentAsyncFault(
            ISessionFactory sessionFactory,
            string apiName,
            SessionType sessionType,
            InvalidOperationException result,
            [Frozen] Mock<ISession> session)
        {
            Func<object> action = () =>
            {
                throw result;
            };
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sessionFactory.InstrumentAsync<object>(apiName, sessionType, () => Task.Run(action))).ConfigureAwait(false);

            session.Verify(m => m.Done<Exception>(SessionStatus.Fault, result), Times.Once());
        }
    }
}
