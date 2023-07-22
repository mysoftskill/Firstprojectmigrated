namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions.UnitTest
{
    using System.Net;

    using Microsoft.PrivacyServices.Testing;

    using Xunit;

    public class BadArgumentErrorTest
    {
        [Theory(DisplayName = "Verify NullArgumentError fields."), AutoMoqData]
        public void VerifyNullArgumentError(string paramName)
        {
            var error = new NullArgumentError(paramName);
            Assert.Equal("BadArgument", error.ServiceError.Code);
            Assert.Equal(paramName, error.ServiceError.Target);
            Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
            Assert.Equal("NullValue", error.ServiceError.InnerError.Code);
        }

        [Theory(DisplayName = "Verify NullArgumentError Detail fields."), AutoMoqData]
        public void VerifyNullArgumentErrorToDetail(string paramName)
        {
            var detail = new NullArgumentError(paramName).ToDetail();
            Assert.Equal("NullValue", detail.Code);
            Assert.Equal("The given value is null.", detail.Message);
            Assert.Equal(paramName, detail.Target);
        }

        [Theory(DisplayName = "Verify InvalidArgumentError fields."), AutoMoqData]
        public void VerifyInvalidArgumentError(string paramName, string value)
        {
            var error = new InvalidArgumentError(paramName, value);
            Assert.Equal("BadArgument", error.ServiceError.Code);
            Assert.Equal(paramName, error.ServiceError.Target);
            Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
            Assert.Equal("InvalidValue", error.ServiceError.InnerError.Code);
            Assert.Equal(value, (error.ServiceError.InnerError as InvalidArgumentError.ArgumentInnerError).Value);
        }

        [Theory(DisplayName = "Verify InvalidArgumentError Detail fields."), AutoMoqData]
        public void VerifyInvalidArgumentErrorToDetail(string paramName, string value)
        {
            var detail = new InvalidArgumentError(paramName, value).ToDetail();
            Assert.Equal("InvalidValue", detail.Code);
            Assert.Equal("The given value is invalid. Value: " + value, detail.Message);
            Assert.Equal(paramName, detail.Target);
        }

        [Theory(DisplayName = "Verify NotAuthenticatedError fields."), AutoMoqData]
        public void VerifyNotAuthenticatedError(string message)
        {
            var error = new NotAuthenticatedError(message);
            Assert.Equal("NotAuthenticated", error.ServiceError.Code);
            Assert.Equal(message, error.ServiceError.Message);
            Assert.Equal(HttpStatusCode.Unauthorized, error.StatusCode);
        }

        [Theory(DisplayName = "Verify NotAuthenticatedError Detail fields."), AutoMoqData]
        public void VerifyNotAuthenticatedErrorToDetail(string message)
        {
            var detail = new NotAuthenticatedError(message).ToDetail();
            Assert.Equal("NotAuthenticated", detail.Code);
            Assert.Equal(message, detail.Message);
        }

        [Theory(DisplayName = "Verify NotAuthorizedError fields."), AutoMoqData]
        public void VerifyNotAuthorizedError(string message)
        {
            var error = new NotAuthorizedError(message);
            Assert.Equal("NotAuthorized", error.ServiceError.Code);
            Assert.Equal(message, error.ServiceError.Message);
            Assert.Equal(HttpStatusCode.Forbidden, error.StatusCode);
        }

        [Theory(DisplayName = "Verify NotAuthorizedError Detail fields."), AutoMoqData]
        public void VerifyNotAuthorizedErrorToDetail(string message)
        {
            var detail = new NotAuthorizedError(message).ToDetail();
            Assert.Equal("NotAuthorized", detail.Code);
            Assert.Equal(message, detail.Message);
        }
    }
}
