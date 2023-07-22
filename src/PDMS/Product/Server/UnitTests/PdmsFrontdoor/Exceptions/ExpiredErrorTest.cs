namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions.UnitTest
{
    using System.Net;

    using Microsoft.PrivacyServices.Testing;

    using Xunit;

    public class ExpiredErrorTest
    {
        [Theory(DisplayName = "Verify ETagMismatchError fields."), AutoMoqData]
        public void VerifyETagMismatchError(string message)
        {
            var error = new ETagMismatchError(message);
            Assert.Equal("Expired", error.ServiceError.Code);
            Assert.Null(error.ServiceError.Target);
            Assert.Equal(HttpStatusCode.PreconditionFailed, error.StatusCode);
            Assert.Equal("ETagMismatch", error.ServiceError.InnerError.Code);
        }

        [Fact(DisplayName = "Verify ETagMismatchError Detail field is null.")]
        public void VerifyETagMismatchErrorToDetail()
        {
            var detail = new ETagMismatchError().ToDetail();
            Assert.Equal("ETagMismatch", detail.Code);
            Assert.Equal("ETag mismatch with existing entity.", detail.Message);
            Assert.Null(detail.Target);
        }
    }
}