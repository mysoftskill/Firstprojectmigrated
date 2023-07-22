namespace Microsoft.PrivacyServices.CommandFeed.Client.Test.PrivacyCommandValidator
{
    using System.IdentityModel.Tokens;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using Microsoft.PrivacyServices.CommandFeed.Client.Test.PrivacyCommandValidator.Mocks;
    using Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class JwtTokenSignatureValidationTests
    {
        private static readonly Mock<ICache> CacheMock = new MockJsonKeyCache().Mock;

        private static readonly Mock<IKeyDiscoveryService> Service = new MockKeyDiscoveryService(CacheMock.Object).Mock;

        [TestMethod]
        public void MockMsaSecurityTokenProvider_ReturnsValidCertificate()
        {
            // Arrange
            string keyId = "1HX039rD8w5wYfwMqJTJV4BHrTE";

            // Act
            X509Certificate2 cert = Service.Object.GetCertificate(keyId, default(LoggableInformation), default(CancellationToken)).Result;

            // Assert
            Assert.IsNotNull(cert);
            Assert.IsNotNull(cert.PublicKey);
        }
    }
}
