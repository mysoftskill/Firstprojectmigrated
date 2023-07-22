namespace Microsoft.PrivacyServices.CommandFeed.Client.Test.PrivacyCommandValidator
{
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class KeyDiscoveryFactoryTests
    {
        private readonly EnvironmentConfiguration aadPreproductionConfig = new EnvironmentConfiguration(
            PcvEnvironment.Preproduction,
            Issuer.Aad,
            KeyDiscoveryConfigurationCollection.PublicPpe.KeyDiscoveryEndPoint,
            null,
            KeyDiscoveryConfigurationCollection.PublicPpe.IsCertificateChainValidationEnabled);

        [TestMethod]
        public void KeyDiscoveryFactory_GetKeyDiscoveryService_MsaSubject_ReturnsKeyDiscoveryService()
        {
            var factory = new KeyDiscoveryServiceFactory();
            IKeyDiscoveryService service = factory.GetKeyDiscoveryService(new MsaSubject(), EnvironmentConfiguration.MsaPreproduction, null, default(LoggableInformation));
            Assert.IsInstanceOfType(service, typeof(KeyDiscoveryService));
        }

        [TestMethod]
        public void KeyDiscoveryFactory_GetKeyDiscoveryService_AadSubject_ReturnsKeyDiscoveryService()
        {
            var factory = new KeyDiscoveryServiceFactory();
            
            IKeyDiscoveryService service = factory.GetKeyDiscoveryService(new AadSubject(), this.aadPreproductionConfig, Policies.Current.CloudInstances.Ids.Public.Value, default(LoggableInformation));
            Assert.IsInstanceOfType(service, typeof(KeyDiscoveryService));
        }

        [TestMethod]
        public void KeyDiscoveryFactory_GetKeyDiscoveryService_MsaSubjectThenAadSubject_InitializesDistinctServices()
        {
            var factory = new KeyDiscoveryServiceFactory();

            IKeyDiscoveryService msaService = factory.GetKeyDiscoveryService(new MsaSubject(), EnvironmentConfiguration.MsaPreproduction, null, default(LoggableInformation));
            IKeyDiscoveryService aadService = factory.GetKeyDiscoveryService(new AadSubject(), this.aadPreproductionConfig, Policies.Current.CloudInstances.Ids.Public.Value, default(LoggableInformation));

            Assert.AreNotSame(aadService, msaService);
        } 

        [TestMethod]
        public void KeyDiscoveryFactory_GetKeyDiscoveryService_AadSubjectThenMsaSubject_InitializesDistinctServices()
        {
            var factory = new KeyDiscoveryServiceFactory();

            IKeyDiscoveryService aadService = factory.GetKeyDiscoveryService(new AadSubject(), this.aadPreproductionConfig, Policies.Current.CloudInstances.Ids.Public.Value, default(LoggableInformation));
            IKeyDiscoveryService msaService = factory.GetKeyDiscoveryService(new MsaSubject(), EnvironmentConfiguration.MsaPreproduction, null, default(LoggableInformation));

            Assert.AreNotSame(aadService, msaService);
        }

        [TestMethod]
        public void KeyDiscoveryFactory_GetKeyDiscoveryService_AadSubjectWithDifferentCloudInstances_InitializesDistinctServices()
        {
            var ffConfig = new EnvironmentConfiguration(
                PcvEnvironment.Preproduction,
                Issuer.Aad,
                KeyDiscoveryConfigurationCollection.Fairfax.KeyDiscoveryEndPoint,
                null,
                KeyDiscoveryConfigurationCollection.Fairfax.IsCertificateChainValidationEnabled);
            var factory = new KeyDiscoveryServiceFactory();

            IKeyDiscoveryService aadServicePublic = factory.GetKeyDiscoveryService(new AadSubject(), this.aadPreproductionConfig, Policies.Current.CloudInstances.Ids.Public.Value, default(LoggableInformation));
            IKeyDiscoveryService aadServiceFairfax = factory.GetKeyDiscoveryService(new AadSubject(), ffConfig, Policies.Current.CloudInstances.Ids.US_Azure_Fairfax.Value, default(LoggableInformation));

            Assert.AreNotSame(aadServicePublic, aadServiceFairfax);
        }

        [TestMethod]
        public void KeyDiscoveryFactory_GetsSameServiceWhenCalledSecondTime()
        {
            var ffConfig = new EnvironmentConfiguration(
                PcvEnvironment.Preproduction,
                Issuer.Aad,
                KeyDiscoveryConfigurationCollection.Fairfax.KeyDiscoveryEndPoint,
                null,
                KeyDiscoveryConfigurationCollection.Fairfax.IsCertificateChainValidationEnabled);
            var factory = new KeyDiscoveryServiceFactory();

            IKeyDiscoveryService aadServiceFairfax1 = factory.GetKeyDiscoveryService(new AadSubject(), ffConfig, Policies.Current.CloudInstances.Ids.US_Azure_Fairfax.Value, default(LoggableInformation));
            IKeyDiscoveryService aadServiceFairfax2 = factory.GetKeyDiscoveryService(new AadSubject(), ffConfig, Policies.Current.CloudInstances.Ids.US_Azure_Fairfax.Value, default(LoggableInformation));

            Assert.AreSame(aadServiceFairfax1, aadServiceFairfax2);
        }

        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public void KeyDiscoveryFactory_GetKeyDiscoveryService_AlternateSubject_ThrowsInvalidPrivacyCommandException()
        {
            var factory = new KeyDiscoveryServiceFactory();
            IKeyDiscoveryService service = factory.GetKeyDiscoveryService(new DemographicSubject(), EnvironmentConfiguration.MsaPreproduction, null, default(LoggableInformation));
            Assert.IsInstanceOfType(service, typeof(KeyDiscoveryService));
        }
    }
}
