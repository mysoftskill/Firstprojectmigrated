namespace Microsoft.PrivacyServices.CommandFeed.Client.Test.PrivacyCommandValidator
{
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests environment configuration
    /// </summary>
    [TestClass]
    public class EnvironmentConfigurationTests
    {
        [TestMethod]
        public void ValidEnvironmentConfiguration()
        {
            // Verify Environment
            Assert.AreEqual(PcvEnvironment.Preproduction, EnvironmentConfiguration.MsaPreproduction.Environment);
            Assert.AreEqual(PcvEnvironment.Production, EnvironmentConfiguration.MsaProduction.Environment);

            // Verify Issuer
            Assert.AreEqual(Issuer.Msa, EnvironmentConfiguration.MsaPreproduction.Issuer);
            Assert.AreEqual(Issuer.Msa, EnvironmentConfiguration.MsaProduction.Issuer);
        }
    }
}
