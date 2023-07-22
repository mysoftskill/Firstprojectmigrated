namespace Microsoft.PrivacyServices.DataManagement.Client.V2.UnitTest
{
    using System;

    using Client = Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;
    using Microsoft.PrivacyServices.Testing;

    using Ploeh.AutoFixture;
    using Xunit;

    public class ExpiredErrorTest
    {
        [Theory(DisplayName = "When an expired error without inner error is returned, then parse correctly."), AutoMoqData]
        public void VerifyNullInnerErrorExpiredError(Fixture fixture)
        {
            var error = new NullInnerErrorExpiredError();

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.ExpiredError e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
            }
        }

        [Theory(DisplayName = "When a ETag mismatch error is returned, then parse correctly."), AutoMoqData]
        public void VerifyETagMismatchExpiredError(Fixture fixture, Guid etag)
        {
            var error = new ETagMismatchError("ETag mismatch.", etag.ToString());

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.ExpiredError.ETagMismatch e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
                Assert.Equal(etag.ToString(), e.Value);
            }
        }

        [Theory(DisplayName = "Verify ETagMismatch.ToString contains all properties."), AutoMoqData]
        public void VerifyETagMismatchExpiredError_ToString(Fixture fixture, Guid etag)
        {
            try
            {
                var error = new ETagMismatchError("ETag mismatch.", etag.ToString());
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.ExpiredError.ETagMismatch e)
            {
                var asString = e.ToString();
                Assert.Contains($"\"code\"", asString);
                Assert.Contains($"\"message\"", asString);
                Assert.Contains($"\"source\"", asString);
                Assert.Contains($"\"stackTrace\"", asString);
                Assert.Contains($"\"value\"", asString);
            }
        }

        [Serializable]
        public class NullInnerErrorExpiredError : ExpiredError
        {
            public NullInnerErrorExpiredError()
                : base("message", null)
            {
            }

            public override Detail ToDetail()
            {
                throw new NotImplementedException();
            }
        }
    }
}