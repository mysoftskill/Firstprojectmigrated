namespace Microsoft.PrivacyServices.CommandFeed.Client.Test
{
    using System;

    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests the CommandFeedEndpointConfiguration class
    /// </summary>
    [TestClass]
    public class CommandFeedEndpointConfigurationTests
    {
        [TestMethod]
        public void ConstructorBuildConfigForAzureMooncake()
        {
            var tenantHost = "mytenant.microsoftonline.cn";
            var config = new CommandFeedEndpointConfiguration(Policies.Current.CloudInstances.Ids.CN_Azure_Mooncake.Value, tenantHost);

            Assert.AreEqual(config.CommandFeedAadResourceId, CommandFeedEndpointConfiguration.Mooncake.CommandFeedAadResourceId);
            Assert.AreEqual(config.CommandFeedHostName, CommandFeedEndpointConfiguration.Mooncake.CommandFeedHostName);
            Assert.AreEqual(config.MsaAuthEndpoint, CommandFeedEndpointConfiguration.Mooncake.MsaAuthEndpoint);
            Assert.AreEqual(config.CommandFeedMsaSiteName, CommandFeedEndpointConfiguration.Mooncake.CommandFeedMsaSiteName);
            Assert.AreEqual(config.EnforceValidation, CommandFeedEndpointConfiguration.Mooncake.EnforceValidation);
            Assert.AreEqual(config.Environment, CommandFeedEndpointConfiguration.Mooncake.Environment);

            Uri bfUri = new Uri(CommandFeedEndpointConfiguration.Mooncake.AadAuthority);
            UriBuilder uriBuilder = new UriBuilder(new Uri(bfUri.GetLeftPart(UriPartial.Authority)))
            {
                Port = -1,
                Path = tenantHost
            };
            Assert.AreEqual(config.AadAuthority, uriBuilder.ToString());
        }

        [TestMethod]
        public void ConstructorBuildConfigForAzureFairfax()
        {
            var tenantHost = "mytenant.microsoftonline.us";
            var config = new CommandFeedEndpointConfiguration(Policies.Current.CloudInstances.Ids.US_Azure_Fairfax.Value, tenantHost);

            Assert.AreEqual(config.CommandFeedAadResourceId, CommandFeedEndpointConfiguration.Fairfax.CommandFeedAadResourceId);
            Assert.AreEqual(config.CommandFeedHostName, CommandFeedEndpointConfiguration.Fairfax.CommandFeedHostName);
            Assert.AreEqual(config.MsaAuthEndpoint, CommandFeedEndpointConfiguration.Fairfax.MsaAuthEndpoint);
            Assert.AreEqual(config.CommandFeedMsaSiteName, CommandFeedEndpointConfiguration.Fairfax.CommandFeedMsaSiteName);
            Assert.AreEqual(config.EnforceValidation, CommandFeedEndpointConfiguration.Fairfax.EnforceValidation);
            Assert.AreEqual(config.Environment, CommandFeedEndpointConfiguration.Fairfax.Environment);

            Uri bfUri = new Uri(CommandFeedEndpointConfiguration.Fairfax.AadAuthority);
            UriBuilder uriBuilder = new UriBuilder(new Uri(bfUri.GetLeftPart(UriPartial.Authority)))
            {
                Port = -1,
                Path = tenantHost
            };
            Assert.AreEqual(config.AadAuthority, uriBuilder.ToString());
        }

        [TestMethod]
        public void ConstructorBuildConfigForPublic()
        {
            var tenantHost = "mytenant.microsoftonline.com";
            var config = new CommandFeedEndpointConfiguration(Policies.Current.CloudInstances.Ids.Public.Value, tenantHost);

            Assert.AreEqual(config.CommandFeedAadResourceId, CommandFeedEndpointConfiguration.Production.CommandFeedAadResourceId);
            Assert.AreEqual(config.CommandFeedHostName, CommandFeedEndpointConfiguration.Production.CommandFeedHostName);
            Assert.AreEqual(config.MsaAuthEndpoint, CommandFeedEndpointConfiguration.Production.MsaAuthEndpoint);
            Assert.AreEqual(config.CommandFeedMsaSiteName, CommandFeedEndpointConfiguration.Production.CommandFeedMsaSiteName);
            Assert.AreEqual(config.EnforceValidation, CommandFeedEndpointConfiguration.Production.EnforceValidation);
            Assert.AreEqual(config.Environment, CommandFeedEndpointConfiguration.Production.Environment);

            Uri bfUri = new Uri(CommandFeedEndpointConfiguration.Production.AadAuthority);
            UriBuilder uriBuilder = new UriBuilder(new Uri(bfUri.GetLeftPart(UriPartial.Authority)))
            {
                Port = -1,
                Path = tenantHost
            };
            Assert.AreEqual(config.AadAuthority, uriBuilder.ToString());
        }

        [TestMethod]
        public void ConstructorBuildConfigForPublicPreproduction()
        {
            var tenantHost = "mytenant.microsoftonline.com";
            var config = new CommandFeedEndpointConfiguration(Policies.Current.CloudInstances.Ids.Public.Value, tenantHost, PcvEnvironment.Preproduction);

            Assert.AreEqual(config.CommandFeedAadResourceId, CommandFeedEndpointConfiguration.Preproduction.CommandFeedAadResourceId);
            Assert.AreEqual(config.CommandFeedHostName, CommandFeedEndpointConfiguration.Preproduction.CommandFeedHostName);
            Assert.AreEqual(config.MsaAuthEndpoint, CommandFeedEndpointConfiguration.Preproduction.MsaAuthEndpoint);
            Assert.AreEqual(config.CommandFeedMsaSiteName, CommandFeedEndpointConfiguration.Preproduction.CommandFeedMsaSiteName);
            Assert.AreEqual(config.EnforceValidation, CommandFeedEndpointConfiguration.Preproduction.EnforceValidation);
            Assert.AreEqual(config.Environment, CommandFeedEndpointConfiguration.Preproduction.Environment);

            Uri bfUri = new Uri(CommandFeedEndpointConfiguration.Preproduction.AadAuthority);
            UriBuilder uriBuilder = new UriBuilder(new Uri(bfUri.GetLeftPart(UriPartial.Authority)))
            {
                Port = -1,
                Path = tenantHost
            };
            Assert.AreEqual(config.AadAuthority, uriBuilder.ToString());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "config")]
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ConstructorThrowsForInvalidCloudInstance()
        {
            var tenantHost = "mytenant.microsoftonline.com";
            var config = new CommandFeedEndpointConfiguration("invalid", tenantHost);
        }
    }
}
