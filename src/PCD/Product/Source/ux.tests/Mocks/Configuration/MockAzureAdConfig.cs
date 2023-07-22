using Microsoft.PrivacyServices.UX.Configuration;
using Moq;

namespace Microsoft.PrivacyServices.UX.Tests.Mocks.Configuration
{
    /// <summary>
    /// Provides mock of <see cref="IAzureADConfig"/>.
    /// </summary>
    public static class MockAzureAdConfig
    {
        /// <summary>
        /// Creates a new instance of <see cref="IAzureADConfig"/> mock.
        /// </summary>
        public static Mock<IAzureADConfig> Create()
        {
            var config = new Mock<IAzureADConfig>(MockBehavior.Strict);
            config.SetupGet(c => c.AppId).Returns("MockAppID");
            config.SetupGet(c => c.Authority).Returns("MockAuthority");
            config.SetupGet(c => c.CertSubjectName).Returns("MockAadCertSubjectName");
            config.SetupGet(c => c.PostLogoutUrl).Returns("MockPostLogoutUrl");

            return config;
        }
    }
}
