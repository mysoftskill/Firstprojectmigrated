namespace Microsoft.PrivacyServices.DataManagement.Client.V2.UnitTest
{
    using Client = Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;
    using Microsoft.PrivacyServices.Testing;

    using Ploeh.AutoFixture;
    using Xunit;

    public class NotAuthenticatedErrorTest
    {
        [Theory(DisplayName = "When a not authenticated error is returned, then parse correctly."), AutoMoqData]
        public void VerifyUserNotAuthenticatedError(Fixture fixture, string message)
        {
            var error = new NotAuthenticatedError(message);

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotAuthenticatedError e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
            }
        }

        [Theory(DisplayName = "Verify NotAuthenticatedError.ToString contains all properties."), AutoMoqData]
        public void VerifyUserNotAuthenticatedError_ToString(Fixture fixture, string message)
        {
            try
            {
                var error = new NotAuthenticatedError(message);
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotAuthenticatedError e)
            {
                var asString = e.ToString();
                Assert.Contains($"\"code\"", asString);
                Assert.Contains($"\"message\"", asString);
                Assert.Contains($"\"source\"", asString);
                Assert.Contains($"\"stackTrace\"", asString);
            }
        }
    }
}