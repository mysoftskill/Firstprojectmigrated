namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation.UnitTest
{
    using Microsoft.PrivacyServices.Testing;

    using Moq;

    using Xunit;

    public class ISessionWriterTest
    {
        [Theory(DisplayName = "When sessionWriter.WriteSuccess is called then ensure Success."), AutoMoqData]
        public void VerifyWriteSuccess(Mock<ISessionWriter<string>> sessionWriter, string apiName, long duration, string cv, string value)
        {
            sessionWriter.Object.WriteSuccess(apiName, duration, cv, value);
            sessionWriter.Verify(m => m.WriteDone(SessionStatus.Success, apiName, duration, cv, value));
        }

        [Theory(DisplayName = "When sessionWriter.WriteError is called then ensure Error."), AutoMoqData]
        public void VerifyWriteError(Mock<ISessionWriter<string>> sessionWriter, string apiName, long duration, string cv, string value)
        {
            sessionWriter.Object.WriteError(apiName, duration, cv, value);
            sessionWriter.Verify(m => m.WriteDone(SessionStatus.Error, apiName, duration, cv, value));
        }

        [Theory(DisplayName = "When sessionWriter.WriteFault is called then ensure Fault."), AutoMoqData]
        public void VerifyWriteFault(Mock<ISessionWriter<string>> sessionWriter, string apiName, long duration, string cv, string value)
        {
            sessionWriter.Object.WriteFault(apiName, duration, cv, value);
            sessionWriter.Verify(m => m.WriteDone(SessionStatus.Fault, apiName, duration, cv, value));
        }
    }
}
