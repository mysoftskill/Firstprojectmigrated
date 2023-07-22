namespace Microsoft.PrivacyServices.AzureFunctions.UnitTests.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common.IfxMetric;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Cloud.InstrumentationFramework.Metrics.Extensions;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Configuration;
    using Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Models;
    using Microsoft.PrivacyServices.AzureFunctions.Core;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]

    public class VariantRequestProcessorTests
    {
        private readonly Mock<IVariantRequestWorkItemService> workItemServiceMock;
        private readonly Mock<ILogger> loggerMock;
        private readonly Mock<IPdmsService> pdmsServiceMock;
        private readonly Mock<IFunctionConfiguration> configurationMock;
        private readonly Mock<IMetricContainer> metricContainerMock;
        private readonly Mock<IPafMapper> mapperMock;
        private readonly PafMapper mapper;
        private readonly VariantRequestProcessor testProcessor;

        public VariantRequestProcessorTests()
        {
            this.workItemServiceMock = new Mock<IVariantRequestWorkItemService>();
            this.loggerMock = new Mock<ILogger>();
            this.pdmsServiceMock = new Mock<IPdmsService>();
            this.configurationMock = new Mock<IFunctionConfiguration>();
            this.metricContainerMock = new Mock<IMetricContainer>();
            this.metricContainerMock.Setup(mock => mock.OutgoingMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues5D>())).Returns(true);
            this.metricContainerMock.Setup(mock => mock.OutgoingApiErrorCountMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues5D>())).Returns(true);
            this.metricContainerMock.Setup(mock => mock.OutgoingSuccessLatencyMetric.Set(It.IsAny<DateTime>(), It.IsAny<ulong>(), It.IsAny<DimensionValues4D>())).Returns(true);
            MappingProfile profile = new MappingProfile();
            this.mapperMock = new Mock<IPafMapper>();
            this.mapper = new PafMapper(profile);
            this.testProcessor = new VariantRequestProcessor(
                    this.configurationMock.Object,
                    this.workItemServiceMock.Object,
                    this.pdmsServiceMock.Object,
                    this.loggerMock.Object,
                    this.metricContainerMock.Object,
                    this.mapperMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ConstructorThrowsIfConfigurationIsNull()
        {
            _ = new VariantRequestProcessor(
                null,
                this.workItemServiceMock.Object,
                this.pdmsServiceMock.Object,
                this.loggerMock.Object,
                this.metricContainerMock.Object,
                this.mapper);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ConstructorThrowsIfWorkItemServiceIsNull()
        {
            _ = new VariantRequestProcessor(
                this.configurationMock.Object,
                null,
                this.pdmsServiceMock.Object,
                this.loggerMock.Object,
                this.metricContainerMock.Object,
                this.mapper);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ConstructorThrowsIfPdmsServiceIsNull()
        {
            _ = new VariantRequestProcessor(
                this.configurationMock.Object,
                this.workItemServiceMock.Object,
                null,
                this.loggerMock.Object,
                this.metricContainerMock.Object,
                this.mapper);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ConstructorThrowsIfLoggerIsNull()
        {
            _ = new VariantRequestProcessor(
                this.configurationMock.Object,
                this.workItemServiceMock.Object,
                this.pdmsServiceMock.Object,
                null,
                this.metricContainerMock.Object,
                this.mapper);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ConstructorThrowsIfTelemetryClientIsNull()
        {
            _ = new VariantRequestProcessor(
                this.configurationMock.Object,
                this.workItemServiceMock.Object,
                this.pdmsServiceMock.Object,
                this.loggerMock.Object,
                null,
                this.mapper);
        }

        [TestMethod]
        public void ConstructorWorks()
        {
            VariantRequestProcessor processor = new VariantRequestProcessor(
                this.configurationMock.Object,
                this.workItemServiceMock.Object,
                this.pdmsServiceMock.Object,
                this.loggerMock.Object,
                this.metricContainerMock.Object,
                this.mapper);

            Assert.IsNotNull(processor);
        }

        [TestMethod]
        public async Task CreateVariantRequestAddsToProcessedQueueOnSuccessAsync()
        {
            var collectorMock = new Mock<ICollector<string>>();
            var variant = new AssetGroupVariant() { VariantId = Guid.NewGuid().ToString() };
            List<AssetGroupVariant> variants = new List<AssetGroupVariant>()
            {
                variant
            };

            List<VariantRelationship> assetGroups = new List<VariantRelationship>()
            {
                new VariantRelationship() { AssetGroupId = Guid.NewGuid().ToString() },
                new VariantRelationship() { AssetGroupId = Guid.NewGuid().ToString() }
            };

            var variantRequestId = Guid.NewGuid();
            var variantRequest = new VariantRequest()
            {
                Id = variantRequestId.ToString(),
                VariantRelationships = assetGroups,
                RequestedVariants = variants
            };

            ExtendedVariantRequest extVariantRequest = this.mapper.Map<VariantRequest, ExtendedVariantRequest>(variantRequest);

            var workItem = new WorkItem() { Id = 1234, Url = "https://someUrl" };
            var variantDefinition = new VariantDefinition()
            {
                Capabilities = new List<string>() { "Test capability" },
                DataTypes = new List<string>() { "Test datatype" },
                SubjectTypes = new List<string>() { "Test subjecttype" }
            };

            // Configure mocks
            this.mapperMock.Setup(mapMock => mapMock.Map<VariantRequest, ExtendedVariantRequest>(variantRequest)).Returns(extVariantRequest);
            this.pdmsServiceMock.Setup(psMock => psMock.GetVariantRequestAsync(variantRequestId)).Returns(Task.FromResult(variantRequest));
            this.workItemServiceMock.Setup(wsMock => wsMock.CreateVariantRequestWorkItemAsync(extVariantRequest)).Returns(Task.FromResult(workItem));
            this.pdmsServiceMock.Setup(psMock => psMock.UpdateVariantRequestAsync(variantRequest)).Returns(Task.FromResult(variantRequest));
            this.pdmsServiceMock.Setup(psMock => psMock.GetVariantDefinitionAsync(Guid.Parse(variant.VariantId))).Returns(Task.FromResult(variantDefinition));
            collectorMock.Setup(cMock => cMock.Add($"variantRequestId = {variantRequestId}, workItemId = 1234, workItemUrl = someUrl"));

            // call the processor
            string message = "{ \"variantRequestId\" : \"" + variantRequestId.ToString() + "\" }";
            await this.testProcessor.CreateVariantRequestWorkItemAsync(message, collectorMock.Object).ConfigureAwait(false);

            // validate variants
            var extVariant = extVariantRequest.RequestedVariants.First();
            Assert.AreEqual(extVariant.Capabilities.Count(), 1);
            Assert.AreEqual(extVariant.DataTypes.Count(), 1);
            Assert.AreEqual(extVariant.SubjectTypes.Count(), 1);
            Assert.AreEqual(extVariant.Capabilities.First(), variantDefinition.Capabilities.First());
            Assert.AreEqual(extVariant.DataTypes.First(), variantDefinition.DataTypes.First());
            Assert.AreEqual(extVariant.SubjectTypes.First(), variantDefinition.SubjectTypes.First());

            this.pdmsServiceMock.Verify(psMock => psMock.UpdateVariantRequestAsync(variantRequest), Times.Once);
            collectorMock.Verify(cMock => cMock.Add(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public Task CreateVariantRequestThrowsArgumentExceptionIfInvalidMessageAsync()
        {
            var collectorMock = new Mock<ICollector<string>>();

            // call the processor
            string message = "{ \"variantRequestId\" : \"invalid\" }";
            return this.testProcessor.CreateVariantRequestWorkItemAsync(message, collectorMock.Object);
        }

        [TestMethod]
        public async Task CreateVariantRequestDoesNotCreateDuplicateWorkItemAsync()
        {
            var collectorMock = new Mock<ICollector<string>>();

            // Create a VariantRequest that already has a WorkItemUri;
            // If so, then we should not create a new work item, but
            // still return success
            var variantRequestId = Guid.NewGuid();
            var variantRequest = new VariantRequest()
            {
                Id = variantRequestId.ToString(),
                WorkItemUri = new Uri("https://someUrl")
            };
            ExtendedVariantRequest extendedVariantRequest = this.mapper.Map<VariantRequest, ExtendedVariantRequest>(variantRequest);

            var workItem = new WorkItem() { Id = 1234, Url = "https://someUrl" };

            // Configure mocks
            this.pdmsServiceMock.Setup(psMock => psMock.GetVariantRequestAsync(variantRequestId)).Returns(Task.FromResult(variantRequest));
            this.workItemServiceMock.Setup(wsMock => wsMock.CreateVariantRequestWorkItemAsync(extendedVariantRequest)).Returns(Task.FromResult(workItem));
            this.pdmsServiceMock.Setup(psMock => psMock.UpdateVariantRequestAsync(variantRequest));
            collectorMock.Setup(cMock => cMock.Add($"variantRequestId = {variantRequestId}, workItemId = 1234, workItemUrl = someUrl"));

            // call the processor
            string message = "{ \"variantRequestId\" : \"" + variantRequestId.ToString() + "\" }";
            await this.testProcessor.CreateVariantRequestWorkItemAsync(message, collectorMock.Object).ConfigureAwait(false);

            this.workItemServiceMock.Verify(wsMock => wsMock.CreateVariantRequestWorkItemAsync(extendedVariantRequest), Times.Never);
            this.pdmsServiceMock.Verify(psMock => psMock.UpdateVariantRequestAsync(variantRequest), Times.Never);
            collectorMock.Verify(cMock => cMock.Add(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public Task CreateVariantRequestThrowsIfVariantRequestIsNotFoundAsync()
        {
            var collectorMock = new Mock<ICollector<string>>();
            var variantRequestId = Guid.NewGuid();
            var variantRequest = new VariantRequest()
            {
                Id = variantRequestId.ToString()
            };
            var extendedVariantRequest = this.mapper.Map<VariantRequest, ExtendedVariantRequest>(variantRequest);

            var workItem = new WorkItem() { Id = 1234, Url = "https://someUrl" };

            // Configure mocks
            this.pdmsServiceMock.Setup(psMock => psMock.GetVariantRequestAsync(variantRequestId)).Returns(Task.FromException<VariantRequest>(new ArgumentException()));
            this.workItemServiceMock.Setup(wsMock => wsMock.CreateVariantRequestWorkItemAsync(extendedVariantRequest)).Returns(Task.FromResult(workItem));
            this.pdmsServiceMock.Setup(psMock => psMock.UpdateVariantRequestAsync(variantRequest));
            collectorMock.Setup(cMock => cMock.Add($"variantRequestId = {variantRequestId}, workItemId = 1234, workItemUrl = someUrl"));

            // call the processor
            string message = "{ \"variantRequestId\" : \"" + variantRequestId.ToString() + "\" }";
            return this.testProcessor.CreateVariantRequestWorkItemAsync(message, collectorMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public Task CreateVariantRequestThrowsIfWorkItemIsNullAsync()
        {
            var collectorMock = new Mock<ICollector<string>>();
            var variantRequestId = Guid.NewGuid();
            var variantRequest = new VariantRequest()
            {
                Id = variantRequestId.ToString()
            };
            var extendedVariantRequest = this.mapper.Map<VariantRequest, ExtendedVariantRequest>(variantRequest);

            // Configure mocks
            this.mapperMock.Setup(mapMock => mapMock.Map<VariantRequest, ExtendedVariantRequest>(variantRequest)).Returns(extendedVariantRequest);
            this.pdmsServiceMock.Setup(psMock => psMock.GetVariantRequestAsync(variantRequestId)).Returns(Task.FromResult(variantRequest));
            this.workItemServiceMock.Setup(wsMock => wsMock.CreateVariantRequestWorkItemAsync(extendedVariantRequest)).Returns(Task.FromResult((WorkItem)null));
            this.pdmsServiceMock.Setup(psMock => psMock.UpdateVariantRequestAsync(variantRequest));
            collectorMock.Setup(cMock => cMock.Add($"variantRequestId = {variantRequestId}, workItemId = 1234, workItemUrl = someUrl"));

            // call the processor
            string message = "{ \"variantRequestId\" : \"" + variantRequestId.ToString() + "\" }";
            return this.testProcessor.CreateVariantRequestWorkItemAsync(message, collectorMock.Object);
        }

        [TestMethod]
        public async Task ApprovePendingVariantRequestsAsyncOnSuccessAsync()
        {
            var variantRequestId = Guid.NewGuid();
            var variantRequest1 = new WorkItem()
            {
                Id = 1234,
                Fields = new Dictionary<string, object>()
                {
                    { "/fields/System.Title", "CELA Approved" },
                    { "Custom.VariantRequestId", variantRequestId.ToString() }
                }
            };
            var listOfMultipleVariants = new List<WorkItem>()
            {
                variantRequest1
            };

            this.pdmsServiceMock.Setup(psMock => psMock.ApproveVariantRequestAsync(variantRequestId)).Returns(Task.FromResult(true));
            this.workItemServiceMock.Setup(wsMock => wsMock.GetPendingVariantRequestWorkItemsAsync()).Returns(Task.FromResult(listOfMultipleVariants));

            // Update the current list of pending variants
            await this.testProcessor.UpdateApprovedVariantRequestWorkItemsAsync().ConfigureAwait(false);

            // Only approves one variant in pdms
            this.pdmsServiceMock.Verify(psMock => psMock.ApproveVariantRequestAsync(variantRequestId), Times.Once);

            // Approves only one workitem
            this.workItemServiceMock.Verify(wsMock => wsMock.ApproveVariantRequestWorkItemAsync(1234), Times.Once);
        }

        [TestMethod]
        public async Task ApprovePendingVariantRequestsOnFailsToApproveInPdmsAsync()
        {
            var variantRequestId = Guid.NewGuid();
            var variantRequest1 = new WorkItem()
            {
                Id = 1234,
                Fields = new Dictionary<string, object>()
                {
                    { "/fields/System.Title", "CELA Approved" },
                    { "Custom.VariantRequestId", variantRequestId.ToString() }
                }
            };
            var listOfMultipleVariants = new List<WorkItem>()
            {
                variantRequest1
            };

            this.pdmsServiceMock.Setup(psMock => psMock.ApproveVariantRequestAsync(variantRequestId)).Returns(Task.FromResult(false));
            this.workItemServiceMock.Setup(wsMock => wsMock.GetPendingVariantRequestWorkItemsAsync()).Returns(Task.FromResult(listOfMultipleVariants));

            // Update the current list of pending variants
            await this.testProcessor.UpdateApprovedVariantRequestWorkItemsAsync().ConfigureAwait(false);

            // Only approves one variant in pdms
            this.pdmsServiceMock.Verify(psMock => psMock.ApproveVariantRequestAsync(variantRequestId), Times.Once);

            // Doesn't approve workitem if not approved in PDMS
            this.workItemServiceMock.Verify(wsMock => wsMock.ApproveVariantRequestWorkItemAsync(1234), Times.Never);
        }

        [TestMethod]
        public async Task ApprovePendingVariantRequestsMultipleRequestsAsync()
        {
            var variantRequestIdSuccess = Guid.NewGuid();
            var variantRequestSuccess = new WorkItem()
            {
                Id = 1234,
                Fields = new Dictionary<string, object>()
                {
                    { "/fields/System.Title", "CELA Approved" },
                    { "Custom.VariantRequestId", variantRequestIdSuccess.ToString() }
                }
            };

            var variantRequestIdFailure = Guid.NewGuid();
            var variantRequestFailure = new WorkItem()
            {
                Id = 4567,
                Fields = new Dictionary<string, object>()
                {
                    { "/fields/System.Title", "CELA Approved" },
                    { "Custom.VariantRequestId", variantRequestIdFailure.ToString() }
                }
            };
            var listOfMultipleVariants = new List<WorkItem>()
            {
                variantRequestFailure,
                variantRequestSuccess
            };

            this.pdmsServiceMock.Setup(psMock => psMock.ApproveVariantRequestAsync(variantRequestIdSuccess)).Returns(Task.FromResult(true));
            this.pdmsServiceMock.Setup(psMock => psMock.ApproveVariantRequestAsync(variantRequestIdFailure)).Returns(Task.FromResult(false));
            this.workItemServiceMock.Setup(wsMock => wsMock.GetPendingVariantRequestWorkItemsAsync()).Returns(Task.FromResult(listOfMultipleVariants));

            // Update the current list of pending variants
            await this.testProcessor.UpdateApprovedVariantRequestWorkItemsAsync().ConfigureAwait(false);

            // Only approves one variant in pdms
            this.pdmsServiceMock.Verify(psMock => psMock.ApproveVariantRequestAsync(variantRequestIdSuccess), Times.Once);
            this.pdmsServiceMock.Verify(psMock => psMock.ApproveVariantRequestAsync(variantRequestIdFailure), Times.Once);

            // Doesn't approve workitem if not approved in PDMS
            this.workItemServiceMock.Verify(wsMock => wsMock.ApproveVariantRequestWorkItemAsync(4567), Times.Never);
            this.workItemServiceMock.Verify(wsMock => wsMock.ApproveVariantRequestWorkItemAsync(1234), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public Task ApprovePendingVariantRequestsOnConnectivityFailureAsync()
        {
            var variantRequestIdSuccess = Guid.NewGuid();
            var variantRequestSuccess = new WorkItem()
            {
                Id = 1234,
                Fields = new Dictionary<string, object>()
                {
                    { "/fields/System.Title", "CELA Approved" },
                    { "Custom.VariantRequestId", variantRequestIdSuccess.ToString() }
                }
            };
            var listOfMultipleVariants = new List<WorkItem>()
            {
                variantRequestSuccess
            };

            this.pdmsServiceMock.Setup(psMock => psMock.ApproveVariantRequestAsync(variantRequestIdSuccess)).Throws(new Exception());
            this.workItemServiceMock.Setup(wsMock => wsMock.GetPendingVariantRequestWorkItemsAsync()).Returns(Task.FromResult(listOfMultipleVariants));

            // Update the current list of pending variants
            return this.testProcessor.UpdateApprovedVariantRequestWorkItemsAsync();
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public Task ApprovePendingVariantRequestsOnADOFailureAsync()
        {
            var variantRequestIdSuccess = Guid.NewGuid();
            var variantRequestSuccess = new WorkItem()
            {
                Id = 1234,
                Fields = new Dictionary<string, object>()
                {
                    { "/fields/System.Title", "CELA Approved" },
                    { "Custom.VariantRequestId", variantRequestIdSuccess.ToString() }
                }
            };
            var listOfMultipleVariants = new List<WorkItem>()
            {
                variantRequestSuccess
            };

            this.pdmsServiceMock.Setup(psMock => psMock.ApproveVariantRequestAsync(variantRequestIdSuccess)).Returns(Task.FromResult(true));
            this.workItemServiceMock.Setup(wsMock => wsMock.GetPendingVariantRequestWorkItemsAsync()).Throws(new Exception());

            // Update the current list of pending variants
            return this.testProcessor.UpdateApprovedVariantRequestWorkItemsAsync();
        }

        [TestMethod]
        public async Task ApprovePendingVariantRequestNoPendingResultsSuccessAsync()
        {
            this.workItemServiceMock.Setup(wsMock => wsMock.GetPendingVariantRequestWorkItemsAsync()).Returns(Task.FromResult((List<WorkItem>)null));

            // Update the current list of pending variants
            await this.testProcessor.UpdateApprovedVariantRequestWorkItemsAsync().ConfigureAwait(false);

            this.workItemServiceMock.Verify(wsMock => wsMock.GetPendingVariantRequestWorkItemsAsync(), Times.Once);
            this.workItemServiceMock.Verify(wsMock => wsMock.ApproveVariantRequestWorkItemAsync(1234), Times.Never);
        }

        [TestMethod]
        public async Task RemoveRejectedVariantRequestsOnSuccessAsync()
        {
            var variantRequestId = Guid.NewGuid();
            var variantRequest1 = new WorkItem()
            {
                Id = 1234,
                Fields = new Dictionary<string, object>()
                {
                    { "/fields/System.Title", "CELA Approved" },
                    { "Custom.VariantRequestId", variantRequestId.ToString() }
                }
            };
            var listOfMultipleVariants = new List<WorkItem>()
            {
                variantRequest1
            };

            this.pdmsServiceMock.Setup(psMock => psMock.DeleteVariantRequestAsync(variantRequestId)).Returns(Task.FromResult(true));
            this.workItemServiceMock.Setup(wsMock => wsMock.GetRejectedVariantRequestWorkItemsAsync()).Returns(Task.FromResult(listOfMultipleVariants));

            // Update the current list of rejected variants
            await this.testProcessor.RemoveRejectedVariantRequestWorkItemsAsync().ConfigureAwait(false);

            // Only rejects one variant in pdms
            this.pdmsServiceMock.Verify(psMock => psMock.DeleteVariantRequestAsync(variantRequestId), Times.Once);

            // Removes one workitem in ADO
            this.workItemServiceMock.Verify(wsMock => wsMock.RemoveVariantRequestWorkItemAsync(1234), Times.Once);
        }

        [TestMethod]
        public async Task RemoveRejectedVariantRequestsOnFailsToDeleteInPDMSAsync()
        {
            var variantRequestId = Guid.NewGuid();
            var variantRequest1 = new WorkItem()
            {
                Id = 1234,
                Fields = new Dictionary<string, object>()
                {
                    { "/fields/System.Title", "CELA Approved" },
                    { "Custom.VariantRequestId", variantRequestId.ToString() }
                }
            };
            var listOfMultipleVariants = new List<WorkItem>()
            {
                variantRequest1
            };

            this.pdmsServiceMock.Setup(psMock => psMock.DeleteVariantRequestAsync(variantRequestId)).Returns(Task.FromResult(false));
            this.workItemServiceMock.Setup(wsMock => wsMock.GetRejectedVariantRequestWorkItemsAsync()).Returns(Task.FromResult(listOfMultipleVariants));

            // Update the current list of rejected variants
            await this.testProcessor.RemoveRejectedVariantRequestWorkItemsAsync().ConfigureAwait(false);

            // Only deletes one variant in pdms
            this.pdmsServiceMock.Verify(psMock => psMock.DeleteVariantRequestAsync(variantRequestId), Times.Once);

            // Sets the state to Removed even if variant request is not found in PDMS
            this.workItemServiceMock.Verify(wsMock => wsMock.RemoveVariantRequestWorkItemAsync(1234), Times.Once);
        }

        [TestMethod]
        public async Task RemoveRejectedVariantRequestsMultipleRequestsAsync()
        {
            var variantRequestIdSuccess1 = Guid.NewGuid();
            var variantRequestSuccess1 = new WorkItem()
            {
                Id = 1234,
                Fields = new Dictionary<string, object>()
                {
                    { "/fields/System.Title", "Rejected" },
                    { "Custom.VariantRequestId", variantRequestIdSuccess1.ToString() }
                }
            };

            var variantRequestIdSuccess2 = Guid.NewGuid();
            var variantRequestSuccess2 = new WorkItem()
            {
                Id = 4321,
                Fields = new Dictionary<string, object>()
                {
                    { "/fields/System.Title", "Rejected" },
                    { "Custom.VariantRequestId", variantRequestIdSuccess2.ToString() }
                }
            };

            var variantRequestIdFailure = Guid.NewGuid();
            var variantRequestFailure = new WorkItem()
            {
                Id = 4567,
                Fields = new Dictionary<string, object>()
                {
                    { "/fields/System.Title", "Rejected" },
                    { "Custom.VariantRequestId", variantRequestIdFailure.ToString() }
                }
            };
            var listOfMultipleVariants = new List<WorkItem>()
            {
                variantRequestSuccess1,
                variantRequestFailure,
                variantRequestSuccess2
            };

            this.pdmsServiceMock.Setup(psMock => psMock.DeleteVariantRequestAsync(variantRequestIdSuccess1)).Returns(Task.FromResult(true));
            this.pdmsServiceMock.Setup(psMock => psMock.DeleteVariantRequestAsync(variantRequestIdSuccess2)).Returns(Task.FromResult(true));
            this.pdmsServiceMock.Setup(psMock => psMock.DeleteVariantRequestAsync(variantRequestIdFailure)).Returns(Task.FromResult(false));
            this.workItemServiceMock.Setup(wsMock => wsMock.GetRejectedVariantRequestWorkItemsAsync()).Returns(Task.FromResult(listOfMultipleVariants));

            // Update the current list of rejected variants
            await this.testProcessor.RemoveRejectedVariantRequestWorkItemsAsync().ConfigureAwait(false);

            // Only deletes one variant in pdms
            this.pdmsServiceMock.Verify(psMock => psMock.DeleteVariantRequestAsync(variantRequestIdSuccess1), Times.Once);
            this.pdmsServiceMock.Verify(psMock => psMock.DeleteVariantRequestAsync(variantRequestIdSuccess2), Times.Once);
            this.pdmsServiceMock.Verify(psMock => psMock.DeleteVariantRequestAsync(variantRequestIdFailure), Times.Once);

            // Deletes both items in ADO
            this.workItemServiceMock.Verify(wsMock => wsMock.RemoveVariantRequestWorkItemAsync(1234), Times.Once);
            this.workItemServiceMock.Verify(wsMock => wsMock.RemoveVariantRequestWorkItemAsync(4321), Times.Once);
            this.workItemServiceMock.Verify(wsMock => wsMock.RemoveVariantRequestWorkItemAsync(4567), Times.Once);
        }

        [TestMethod]
        public Task RemoveRejectedVariantRequestsEvenIfDeleteVariantFailureAsync()
        {
            var variantRequestIdSuccess = Guid.NewGuid();
            var variantRequestSuccess = new WorkItem()
            {
                Id = 1234,
                Fields = new Dictionary<string, object>()
                {
                    { "/fields/System.Title", "CELA Approved" },
                    { "Custom.VariantRequestId", variantRequestIdSuccess.ToString() }
                }
            };
            var listOfMultipleVariants = new List<WorkItem>()
            {
                variantRequestSuccess
            };

            this.pdmsServiceMock.Setup(psMock => psMock.DeleteVariantRequestAsync(variantRequestIdSuccess)).Throws(new Exception());
            this.workItemServiceMock.Setup(wsMock => wsMock.GetRejectedVariantRequestWorkItemsAsync()).Returns(Task.FromResult(listOfMultipleVariants));

            // Update the current list of rejected variants
            return this.testProcessor.RemoveRejectedVariantRequestWorkItemsAsync();
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public Task RemoveRejectedVariantRequestsOnADOFailureAsync()
        {
            var variantRequestIdSuccess = Guid.NewGuid();
            var variantRequestSuccess = new WorkItem()
            {
                Id = 1234,
                Fields = new Dictionary<string, object>()
                {
                    { "/fields/System.Title", "Rejected" },
                    { "Custom.VariantRequestId", variantRequestIdSuccess.ToString() }
                }
            };
            var listOfMultipleVariants = new List<WorkItem>()
            {
                variantRequestSuccess
            };

            this.pdmsServiceMock.Setup(psMock => psMock.DeleteVariantRequestAsync(variantRequestIdSuccess)).Returns(Task.FromResult(true));
            this.workItemServiceMock.Setup(wsMock => wsMock.GetRejectedVariantRequestWorkItemsAsync()).Throws(new Exception());

            // Update the current list of rejected variants
            return this.testProcessor.RemoveRejectedVariantRequestWorkItemsAsync();
        }

        [TestMethod]
        public async Task RemoveRejectedVariantRequestNoPendingResultsSuccessAsync()
        {
            this.workItemServiceMock.Setup(wsMock => wsMock.GetRejectedVariantRequestWorkItemsAsync()).Returns(Task.FromResult((List<WorkItem>)null));

            // Update the current list of rejected variants
            await this.testProcessor.RemoveRejectedVariantRequestWorkItemsAsync().ConfigureAwait(false);

            this.workItemServiceMock.Verify(wsMock => wsMock.GetRejectedVariantRequestWorkItemsAsync(), Times.Once);
            this.workItemServiceMock.Verify(wsMock => wsMock.RemoveVariantRequestWorkItemAsync(1234), Times.Never);
        }

        [TestMethod]
        public async Task CreateVariantRequestWithVariantThatCoversAllAsync()
        {
            var collectorMock = new Mock<ICollector<string>>();
            var variant = new AssetGroupVariant() { VariantId = Guid.NewGuid().ToString() };
            List<AssetGroupVariant> variants = new List<AssetGroupVariant>()
            {
                variant
            };

            List<VariantRelationship> assetGroups = new List<VariantRelationship>()
            {
                new VariantRelationship() { AssetGroupId = Guid.NewGuid().ToString() },
                new VariantRelationship() { AssetGroupId = Guid.NewGuid().ToString() }
            };

            var variantRequestId = Guid.NewGuid();
            var variantRequest = new VariantRequest()
            {
                Id = variantRequestId.ToString(),
                VariantRelationships = assetGroups,
                RequestedVariants = variants
            };

            ExtendedVariantRequest extendedVariantRequest = this.mapper.Map<VariantRequest, ExtendedVariantRequest>(variantRequest);

            var workItem = new WorkItem() { Id = 1234, Url = "https://someUrl" };
            var variantDefinition = new VariantDefinition()
            {
                Capabilities = new List<string>(),
                DataTypes = new List<string>(),
                SubjectTypes = new List<string>()
            };

            // Configure mocks
            this.mapperMock.Setup(mapMock => mapMock.Map<VariantRequest, ExtendedVariantRequest>(variantRequest)).Returns(extendedVariantRequest);
            this.pdmsServiceMock.Setup(psMock => psMock.GetVariantRequestAsync(variantRequestId)).Returns(Task.FromResult(variantRequest));
            this.workItemServiceMock.Setup(wsMock => wsMock.CreateVariantRequestWorkItemAsync(extendedVariantRequest)).Returns(Task.FromResult(workItem));
            this.pdmsServiceMock.Setup(psMock => psMock.UpdateVariantRequestAsync(variantRequest)).Returns(Task.FromResult(variantRequest));
            this.pdmsServiceMock.Setup(psMock => psMock.GetVariantDefinitionAsync(Guid.Parse(variant.VariantId))).Returns(Task.FromResult(variantDefinition));
            collectorMock.Setup(cMock => cMock.Add($"variantRequestId = {variantRequestId}, workItemId = 1234, workItemUrl = someUrl"));

            // call the processor
            string message = "{ \"variantRequestId\" : \"" + variantRequestId.ToString() + "\" }";
            await this.testProcessor.CreateVariantRequestWorkItemAsync(message, collectorMock.Object);

            // validate variants
            var extVariant = extendedVariantRequest.RequestedVariants.First();
            var unAvailable = "ALL";
            Assert.AreEqual(extVariant.Capabilities.Count(), 1);
            Assert.AreEqual(extVariant.DataTypes.Count(), 1);
            Assert.AreEqual(extVariant.SubjectTypes.Count(), 1);
            Assert.AreEqual(extVariant.Capabilities.First(), unAvailable);
            Assert.AreEqual(extVariant.DataTypes.First(), unAvailable);
            Assert.AreEqual(extVariant.SubjectTypes.First(), unAvailable);

            this.pdmsServiceMock.Verify(psMock => psMock.UpdateVariantRequestAsync(variantRequest), Times.Once);
            collectorMock.Verify(cMock => cMock.Add(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task CreateWorkItemOnCallToGetVariantDefinitionAsyncFailureAsync()
        {
            var collectorMock = new Mock<ICollector<string>>();
            var variant = new AssetGroupVariant() { VariantId = Guid.NewGuid().ToString() };
            List<AssetGroupVariant> variants = new List<AssetGroupVariant>()
            {
                variant
            };

            List<VariantRelationship> assetGroups = new List<VariantRelationship>()
            {
                new VariantRelationship() { AssetGroupId = Guid.NewGuid().ToString() },
                new VariantRelationship() { AssetGroupId = Guid.NewGuid().ToString() }
            };

            var variantRequestId = Guid.NewGuid();
            var variantRequest = new VariantRequest()
            {
                Id = variantRequestId.ToString(),
                VariantRelationships = assetGroups,
                RequestedVariants = variants
            };

            var extendedVariantRequest = this.mapper.Map<VariantRequest, ExtendedVariantRequest>(variantRequest);
            var workItem = new WorkItem() { Id = 1234, Url = "https://someUrl" };

            // Configure mocks
            this.mapperMock.Setup(mapMock => mapMock.Map<VariantRequest, ExtendedVariantRequest>(variantRequest)).Returns(extendedVariantRequest);
            this.pdmsServiceMock.Setup(psMock => psMock.GetVariantRequestAsync(variantRequestId)).Returns(Task.FromResult(variantRequest));
            this.workItemServiceMock.Setup(wsMock => wsMock.CreateVariantRequestWorkItemAsync(extendedVariantRequest)).Returns(Task.FromResult(workItem));
            this.pdmsServiceMock.Setup(psMock => psMock.UpdateVariantRequestAsync(variantRequest));
            collectorMock.Setup(cMock => cMock.Add($"variantRequestId = {variantRequestId}, workItemId = {workItem.Id}, workItemUrl = {workItem.Url}"));

            // call the processor
            string message = "{ \"variantRequestId\" : \"" + variantRequestId.ToString() + "\" }";
            await this.testProcessor.CreateVariantRequestWorkItemAsync(message, collectorMock.Object);

            // validate variants
            var extVariant = extendedVariantRequest.RequestedVariants.First();
            var unAvailable = "UNAVAILABLE";
            Assert.AreEqual(extVariant.Capabilities.Count(), 1);
            Assert.AreEqual(extVariant.DataTypes.Count(), 1);
            Assert.AreEqual(extVariant.SubjectTypes.Count(), 1);
            Assert.AreEqual(extVariant.Capabilities.First(), unAvailable);
            Assert.AreEqual(extVariant.DataTypes.First(), unAvailable);
            Assert.AreEqual(extVariant.SubjectTypes.First(), unAvailable);

            this.pdmsServiceMock.Verify(psMock => psMock.UpdateVariantRequestAsync(variantRequest), Times.Once);
            collectorMock.Verify(cMock => cMock.Add(It.IsAny<string>()), Times.Once);
        }
    }
}
