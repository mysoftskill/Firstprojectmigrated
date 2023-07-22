namespace Microsoft.PrivacyServices.CommandFeed.Client.Test.PrivacyCommandValidator.Validator
{
    using System;
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CloudConfigurationTests
    {
        [TestMethod]
        public void GetEnvironmentConfigurationGetsMsaPpeForMsaSubjectInPpe()
        {
            EnvironmentConfiguration config = EnvironmentConfiguration.GetEnvironmentConfiguration(
                new MsaSubject(),
                null,
                null,
                PcvEnvironment.Preproduction,
                default(LoggableInformation));

            Assert.IsNotNull(config);
            Assert.AreEqual(Issuer.Msa, config.Issuer);
            Assert.AreEqual(PcvEnvironment.Preproduction, config.Environment);
            Assert.AreEqual(EnvironmentConfiguration.MsaPreproduction.KeyDiscoveryEndPoint, config.KeyDiscoveryEndPoint);
        }

        [TestMethod]
        public void GetEnvironmentConfigurationGetsMsaProdForMsaSubjectInProd()
        {
            EnvironmentConfiguration config = EnvironmentConfiguration.GetEnvironmentConfiguration(
                new MsaSubject(),
                null,
                null,
                PcvEnvironment.Production,
                default(LoggableInformation));

            Assert.IsNotNull(config);
            Assert.AreEqual(Issuer.Msa, config.Issuer);
            Assert.AreEqual(PcvEnvironment.Production, config.Environment);
            Assert.AreEqual(EnvironmentConfiguration.MsaProduction.KeyDiscoveryEndPoint, config.KeyDiscoveryEndPoint);
        }

        [TestMethod]
        public void GetEnvironmentConfigurationGetsMsaPpeForDeviceSubjectInPpe()
        {
            EnvironmentConfiguration config = EnvironmentConfiguration.GetEnvironmentConfiguration(
                new DeviceSubject(),
                null,
                null,
                PcvEnvironment.Preproduction,
                default(LoggableInformation));

            Assert.IsNotNull(config);
            Assert.AreEqual(Issuer.Msa, config.Issuer);
            Assert.AreEqual(PcvEnvironment.Preproduction, config.Environment);
            Assert.AreEqual(EnvironmentConfiguration.MsaPreproduction.KeyDiscoveryEndPoint, config.KeyDiscoveryEndPoint);
        }

        [TestMethod]
        public void GetEnvironmentConfigurationGetsMsaProdForDeviceSubjectInProd()
        {
            EnvironmentConfiguration config = EnvironmentConfiguration.GetEnvironmentConfiguration(
                new DeviceSubject(),
                null,
                null,
                PcvEnvironment.Preproduction,
                default(LoggableInformation));

            Assert.IsNotNull(config);
            Assert.AreEqual(Issuer.Msa, config.Issuer);
            Assert.AreEqual(PcvEnvironment.Preproduction, config.Environment);
            Assert.AreEqual(EnvironmentConfiguration.MsaPreproduction.KeyDiscoveryEndPoint, config.KeyDiscoveryEndPoint);
        }
        
        [TestMethod]
        public void GetEnvironmentConfigurationGetsPublicPpeConfigurationAsPpeDefault()
        {
            EnvironmentConfiguration config = EnvironmentConfiguration.GetEnvironmentConfiguration(
                new AadSubject(), 
                null, 
                new List<KeyDiscoveryConfiguration>(), 
                PcvEnvironment.Preproduction, 
                default(LoggableInformation));

            Assert.IsNotNull(config);
            Assert.AreEqual(Issuer.Aad, config.Issuer);
            Assert.AreEqual(PcvEnvironment.Preproduction, config.Environment);
            Assert.AreEqual(KeyDiscoveryConfigurationCollection.PublicPpe.KeyDiscoveryEndPoint, config.KeyDiscoveryEndPoint);
        }

        [TestMethod]
        public void GetEnvironmentConfigurationGetsPublicProdConfigurationAsProdDefault()
        {
            EnvironmentConfiguration config = EnvironmentConfiguration.GetEnvironmentConfiguration(
                new AadSubject(), 
                null, 
                new List<KeyDiscoveryConfiguration>(), 
                PcvEnvironment.Production, 
                default(LoggableInformation));

            Assert.IsNotNull(config);
            Assert.AreEqual(Issuer.Aad, config.Issuer);
            Assert.AreEqual(PcvEnvironment.Production, config.Environment);
            Assert.AreEqual(KeyDiscoveryConfigurationCollection.Public.KeyDiscoveryEndPoint, config.KeyDiscoveryEndPoint);
        }

        [TestMethod]
        public void GetEnvironmentConfigurationGetsConfigurationFromAgentList()
        {
            const string instance = "testAad";
            Uri issuer = new Uri("https://myissuer.com");
            Uri endPoint = new Uri("https://myendpoint.com");

            var kdconfig = new KeyDiscoveryConfiguration(new List<string> { instance }, issuer, endPoint, false);
            
            EnvironmentConfiguration config = EnvironmentConfiguration.GetEnvironmentConfiguration(
                new AadSubject(), 
                instance, 
                new List<KeyDiscoveryConfiguration>() { kdconfig }, 
                PcvEnvironment.Production, 
                default(LoggableInformation));

            Assert.IsNotNull(config);
            Assert.AreEqual(Issuer.Aad, config.Issuer);
            Assert.AreEqual(PcvEnvironment.Production, config.Environment);
            Assert.AreEqual(endPoint, config.KeyDiscoveryEndPoint);
            Assert.AreEqual(false, config.IsCertificateChainValidationEnabled);
            Assert.AreEqual(@"^https:\/\/myissuer.com\/$", config.IssuerRegexPattern);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public void GetEnvironmentConfigurationThrowsIfInstanceMissingInAgentListIfProvided()
        {
            const string instance = "testAad";
            Uri issuer = new Uri("https://myissuer.com");
            Uri endPoint = new Uri("https://myendpoint.com");

            string commandInstance = Policies.Current.CloudInstances.Ids.US_Azure_Fairfax.Value;

            var kdconfig = new KeyDiscoveryConfiguration(new List<string> { instance }, issuer, endPoint, false);
            
            EnvironmentConfiguration.GetEnvironmentConfiguration(
                new AadSubject(),
                commandInstance,
                new List<KeyDiscoveryConfiguration>() { kdconfig },
                PcvEnvironment.Production, 
                default(LoggableInformation));
        }

        [TestMethod]
        public void GetEnvironmentConfigurationGetsConfigurationBasedOnCloudInstance()
        {
            EnvironmentConfiguration config = EnvironmentConfiguration.GetEnvironmentConfiguration(
                new AadSubject(), 
                Policies.Current.CloudInstances.Ids.US_Azure_Fairfax.Value, 
                null,
                PcvEnvironment.Production, 
                default(LoggableInformation));

            Assert.IsNotNull(config);
            Assert.AreEqual(Issuer.Aad, config.Issuer);
            Assert.AreEqual(PcvEnvironment.Production, config.Environment);
            Assert.AreEqual(KeyDiscoveryConfigurationCollection.Fairfax.KeyDiscoveryEndPoint, config.KeyDiscoveryEndPoint);
            Assert.AreEqual(KeyDiscoveryConfigurationCollection.Fairfax.IsCertificateChainValidationEnabled, config.IsCertificateChainValidationEnabled);
            Assert.AreEqual($"^{KeyDiscoveryConfigurationCollection.Fairfax.Issuer.AbsoluteUri.Replace("/", @"\/")}$", config.IssuerRegexPattern);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public void GetEnvironmentConfigurationThrowsIfInvalidInstance()
        {
            EnvironmentConfiguration.GetEnvironmentConfiguration(
                new AadSubject(), 
                "MyInstance", 
                null,
                PcvEnvironment.Production,
                default(LoggableInformation));
        }
    }
}
