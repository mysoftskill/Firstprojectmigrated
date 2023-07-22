namespace Microsoft.PrivacyServices.CommandFeed.Client.Test.PrivacyCommandValidator.Configuration
{
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class KeyDiscoveryConfigurationCollectionTests
    {
        [TestMethod]
        public void CollectionContainsConfigurationForAllCloudInstances()
        {
            var cloudInstances = new[]
            {
                "Public",
                "CN.Azure.Mooncake",
                "US.Azure.Fairfax",
                "PublicPpe"
            };

            foreach (var cloud in cloudInstances)
            {
                var config = KeyDiscoveryConfigurationCollection.KeyDiscoveryConfigurations[cloud];
                Assert.IsNotNull(config);

                if (cloud == "PublicPpe")
                {
                    Assert.IsTrue(config.CloudInstances.Contains("Public"));
                }
                else
                {
                    Assert.IsTrue(config.CloudInstances.Contains(cloud));
                }

                Assert.IsNotNull(config.Issuer);
                Assert.IsNotNull(config.KeyDiscoveryEndPoint);
            }
        }
    }
}
