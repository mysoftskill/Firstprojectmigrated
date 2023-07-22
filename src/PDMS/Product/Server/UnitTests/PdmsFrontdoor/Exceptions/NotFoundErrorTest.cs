namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions.UnitTest
{
    using System.Net;

    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Xunit;

    public class NotFoundErrorTest
    {
        [Theory(DisplayName = "Verify NotFoundError fields."), AutoMoqData]
        public void VerifyNotFoundError(Fixture fixture)
        {
            fixture.DisableRecursionCheck(); // InnerError is recursive, so must disable this check.

            var message = fixture.Create<string>();
            var innerError = fixture.Create<InnerError>();
            var error = new Mock<NotFoundError>(MockBehavior.Loose, message, innerError).Object;

            Assert.Equal("NotFound", error.ServiceError.Code);
            Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
            Assert.Equal(message, error.ServiceError.Message);
            Assert.Equal(innerError.Code, error.ServiceError.InnerError.Code);
        }
    }
}
