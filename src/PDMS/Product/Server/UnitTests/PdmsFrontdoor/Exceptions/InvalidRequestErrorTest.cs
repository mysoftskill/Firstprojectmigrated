namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions.UnitTest
{
    using System.Net;

    using Microsoft.PrivacyServices.Testing;

    using Xunit;

    public class InvalidRequestErrorTest
    {
        [Theory(DisplayName = "Verify InvalidRequestError fields."), AutoMoqData]
        public void VerifyInvalidArgumentError(string message)
        {
            var error = new InvalidRequestError(message);
            Assert.Equal("BadArgument", error.ServiceError.Code);
            Assert.Null(error.ServiceError.Target);
            Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
            Assert.Equal("InvalidRequest", error.ServiceError.InnerError.Code);
        }

        [Theory(DisplayName = "Verify InvalidArgumentError Detail fields."), AutoMoqData]
        public void VerifyInvalidArgumentErrorToDetail(string message)
        {
            var detail = new InvalidRequestError(message).ToDetail();
            Assert.Equal("InvalidRequest", detail.Code);
            Assert.Equal(message, detail.Message);
            Assert.Null(detail.Target);
        }
    }
}