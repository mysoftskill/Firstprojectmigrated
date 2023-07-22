namespace PCF.UnitTests.Applicability
{
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Applicability;
    using Microsoft.PrivacyServices.Policy;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class PrivacyCommandTypeToCapabilityIdTest
    {
        [Theory]
        [InlineAutoData(PrivacyCommandType.AccountClose, "AccountClose")]
        [InlineAutoData(PrivacyCommandType.Delete, "Delete")]
        [InlineAutoData(PrivacyCommandType.Export, "Export")]
        public void VerifyPrivacyCommandTypeToCapabilityId(PrivacyCommandType privacyCommandType, string capabilityIdName)
        {
            var capabilityId = Policies.Current.Capabilities.CreateId(capabilityIdName);
            Assert.Equal(capabilityId, privacyCommandType.ToCapabilityId());
        }
    }
}
