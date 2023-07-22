namespace Functions.UnitTests.Core
{
    using Microsoft.Azure.ComplianceServices.Common.IfxMetric;
    using Microsoft.PrivacyServices.AzureFunctions.Common;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Configuration;
    using Microsoft.PrivacyServices.AzureFunctions.Core;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class VariantRequestProcessorFactoryTest
    {
        [TestMethod]
        public void Create()
        {
            var configuration = new Mock<IFunctionConfiguration>();
            configuration.SetupGet(c => c.AzureDevOpsProjectUrl).Returns(@"https:\\dummyUrl").Verifiable();
            configuration.SetupGet(c => c.PdmsBaseUrl).Returns(@"https:\\pdms").Verifiable();

            var metricContainer = new Mock<IMetricContainer>();
            var authenticationProvider = new Mock<IAuthenticationProvider>();
            var logger = new Mock<ILogger>();

            var factory = new VariantRequestProcessorFactory();
            var processor = factory.Create(
                configuration.Object,
                authenticationProvider.Object,
                metricContainer.Object,
                logger.Object);

            Assert.IsNotNull(processor);
            Assert.IsTrue(processor is VariantRequestProcessor);

            configuration.VerifyAll();
        }
    }
}
