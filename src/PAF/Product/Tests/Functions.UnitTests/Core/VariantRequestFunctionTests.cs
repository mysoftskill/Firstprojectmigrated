namespace Functions.UnitTests.Core
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common.IfxMetric;
    using Microsoft.Cloud.InstrumentationFramework.Metrics.Extensions;
    using Microsoft.PrivacyServices.AzureFunctions.Common;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Configuration;
    using Microsoft.PrivacyServices.AzureFunctions.Core;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class VariantRequestFunctionTests
    {
        [TestMethod]
        public async Task CreateVariantRequestWorkItemAsync()
        {
            var message = "test";
            var configuration = new Mock<IFunctionConfiguration>();
            var metricContainer = new Mock<IMetricContainer>();
            metricContainer.Setup(mock => mock.IncomingMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues4D>())).Returns(true);
            metricContainer.Setup(mock => mock.IncomingApiErrorCountMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues4D>())).Returns(true);
            metricContainer.Setup(mock => mock.IncomingSuccessLatencyMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues3D>())).Returns(true);

            var authenticationProvider = new Mock<IAuthenticationProvider>();
            var logger = new Mock<ILogger>();

            var processor = new Mock<IVariantRequestProcessor>();
            processor.Setup(p => p.CreateVariantRequestWorkItemAsync(message, null))
                .Returns(Task.CompletedTask).Verifiable();

            var processorFactory = new Mock<IVariantRequestProcessorFactory>();
            processorFactory.Setup(f => f.Create(
                configuration.Object,
                authenticationProvider.Object,
                It.IsAny<IMetricContainer>(),
                logger.Object))
                .Returns(processor.Object)
                .Verifiable();

            var functions = new VariantRequestFunction(
                configuration.Object,
                metricContainer.Object,
                authenticationProvider.Object,
                processorFactory.Object,
                logger.Object);

            await functions.CreateVariantRequestWorkItemAsync(message, null).ConfigureAwait(false);

            processorFactory.VerifyAll();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public Task CreateVariantRequestWorkItemErrorAsync()
        {
            var message = "test";
            var configuration = new Mock<IFunctionConfiguration>();
            var metricContainer = new Mock<IMetricContainer>();
            metricContainer.Setup(mock => mock.IncomingMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues4D>())).Returns(true);
            metricContainer.Setup(mock => mock.IncomingApiErrorCountMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues4D>())).Returns(true);
            metricContainer.Setup(mock => mock.IncomingSuccessLatencyMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues3D>())).Returns(true);

            var authenticationProvider = new Mock<IAuthenticationProvider>();
            var logger = new Mock<ILogger>();

            var processor = new Mock<IVariantRequestProcessor>();
            processor.Setup(p => p.CreateVariantRequestWorkItemAsync(message, null))
                .Callback(() => throw new InvalidOperationException());

            var processorFactory = new Mock<IVariantRequestProcessorFactory>();
            processorFactory.Setup(f => f.Create(
                configuration.Object,
                authenticationProvider.Object,
                It.IsAny<IMetricContainer>(),
                logger.Object))
                .Returns(processor.Object)
                .Verifiable();

            var functions = new VariantRequestFunction(
                configuration.Object,
                metricContainer.Object,
                authenticationProvider.Object,
                processorFactory.Object,
                logger.Object);

            return functions.CreateVariantRequestWorkItemAsync(message, null);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public Task UpdateApprovedVariantRequestErrorAsync()
        {
            var configuration = new Mock<IFunctionConfiguration>();
            var metricContainer = new Mock<IMetricContainer>();
            metricContainer.Setup(mock => mock.IncomingMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues4D>())).Returns(true);
            metricContainer.Setup(mock => mock.IncomingApiErrorCountMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues4D>())).Returns(true);
            metricContainer.Setup(mock => mock.IncomingSuccessLatencyMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues3D>())).Returns(true);

            var authenticationProvider = new Mock<IAuthenticationProvider>();
            var logger = new Mock<ILogger>();

            var processor = new Mock<IVariantRequestProcessor>();
            processor.Setup(p => p.UpdateApprovedVariantRequestWorkItemsAsync())
                .Callback(() => throw new InvalidOperationException());

            var processorFactory = new Mock<IVariantRequestProcessorFactory>();
            processorFactory.Setup(f => f.Create(
                configuration.Object,
                authenticationProvider.Object,
                It.IsAny<IMetricContainer>(),
                logger.Object))
                .Returns(processor.Object)
                .Verifiable();

            var functions = new VariantRequestFunction(
                configuration.Object,
                metricContainer.Object,
                authenticationProvider.Object,
                processorFactory.Object,
                logger.Object);

            return functions.UpdateApprovedVariantRequestAsync(null);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public Task RemoveRejectedVariantRequestErrorAsync()
        {
            var configuration = new Mock<IFunctionConfiguration>();
            var metricContainer = new Mock<IMetricContainer>();
            metricContainer.Setup(mock => mock.IncomingMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues4D>())).Returns(true);
            metricContainer.Setup(mock => mock.IncomingApiErrorCountMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues4D>())).Returns(true);
            metricContainer.Setup(mock => mock.IncomingSuccessLatencyMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues3D>())).Returns(true);

            var authenticationProvider = new Mock<IAuthenticationProvider>();
            var logger = new Mock<ILogger>();

            var processor = new Mock<IVariantRequestProcessor>();
            processor.Setup(p => p.RemoveRejectedVariantRequestWorkItemsAsync())
                .Callback(() => throw new InvalidOperationException());

            var processorFactory = new Mock<IVariantRequestProcessorFactory>();
            processorFactory.Setup(f => f.Create(
                configuration.Object,
                authenticationProvider.Object,
                It.IsAny<IMetricContainer>(),
                logger.Object))
                .Returns(processor.Object)
                .Verifiable();

            var functions = new VariantRequestFunction(
                configuration.Object,
                metricContainer.Object,
                authenticationProvider.Object,
                processorFactory.Object,
                logger.Object);

            return functions.RemoveRejectedVariantRequestAsync(null);
        }
    }
}
