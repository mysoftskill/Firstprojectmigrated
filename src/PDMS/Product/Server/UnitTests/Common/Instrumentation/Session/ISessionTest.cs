namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation.UnitTest
{
    using System;

    using Microsoft.PrivacyServices.Testing;

    using Moq;

    using Xunit;

    public class ISessionTest
    {
        [Theory(DisplayName = "When session.Success is called with no value then ensure Success."), AutoMoqData]
        public void VerifySuccess(Mock<ISession> session)
        {
            session.Object.Success();
            session.Verify(m => m.Done(SessionStatus.Success));
        }

        [Theory(DisplayName = "When session.Success is called with a value then ensure Success."), AutoMoqData]
        public void VerifySuccessWithData(Mock<ISession> session, string data)
        {
            session.Object.Success(data);
            session.Verify(m => m.Done(SessionStatus.Success, data));
        }

        [Theory(DisplayName = "When session.Error is called with no value then ensure Error."), AutoMoqData]
        public void VerifyError(Mock<ISession> session)
        {
            session.Object.Error();
            session.Verify(m => m.Done(SessionStatus.Error));
        }

        [Theory(DisplayName = "When session.Error is called with a value then ensure Error."), AutoMoqData]
        public void VerifyErrorWithData(Mock<ISession> session, string data)
        {
            session.Object.Error(data);
            session.Verify(m => m.Done(SessionStatus.Error, data));
        }

        [Theory(DisplayName = "When session.Fault is called with no value then ensure Fault."), AutoMoqData]
        public void VerifyFault(Mock<ISession> session)
        {
            session.Object.Fault();
            session.Verify(m => m.Done(SessionStatus.Fault));
        }

        [Theory(DisplayName = "When session.Fault is called with a value then ensure Fault."), AutoMoqData]
        public void VerifyFaultWithData(Mock<ISession> session, string data)
        {
            session.Object.Fault(data);
            session.Verify(m => m.Done(SessionStatus.Fault, data));
        }

        [Theory(DisplayName = "When session.Fault is called with an exception then ensure Fault."), AutoMoqData]
        public void VerifyFaultWithException(Mock<ISession> session, InvalidOperationException exception)
        {
            session.Object.Fault(exception);
            session.Verify(m => m.Done<Exception>(SessionStatus.Fault, exception));
        }
    }
}
