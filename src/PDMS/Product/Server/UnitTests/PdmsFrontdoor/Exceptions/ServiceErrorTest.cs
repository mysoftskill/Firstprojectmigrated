namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions.UnitTest
{
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Xunit;

    public class ServiceErrorTest
    {
        [Theory(DisplayName = "When ServiceError.FlattenErrorCode is called, then join all error codes."), AutoMoqData]
        public void VerifyFlattenErrorCode(string rootCode, string error1, string error2)
        {            
            var innerError = new Mock<InnerError>(error1).Object;
            var innerError2 = new Mock<InnerError>(error2).Object;
            innerError.NestedError = innerError2;

            var serviceError = new ServiceError(rootCode, string.Empty);
            serviceError.InnerError = innerError;

            var finalCode = serviceError.ToString();
            Assert.Equal($"{rootCode}:{error1}:{error2}", finalCode);
        }

        [Theory(DisplayName = "When ServiceError.ToString is called, then join all error codes."), AutoMoqData]
        public void VerifyToString(string rootCode, string error1, string error2)
        {
            var innerError = new Mock<InnerError>(error1).Object;
            var innerError2 = new Mock<InnerError>(error2).Object;
            innerError.NestedError = innerError2;

            var serviceError = new ServiceError(rootCode, string.Empty);
            serviceError.InnerError = innerError;

            var finalCode = serviceError.ToString();
            Assert.Equal($"{rootCode}:{error1}:{error2}", finalCode);
        }
    }
}
