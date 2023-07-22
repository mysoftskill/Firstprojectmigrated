namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Export
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Azure.Storage.Blob;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Export;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class ExportServiceTests
    {
        private Mock<IPrivacyConfigurationManager> mockConfigurationManager = new Mock<IPrivacyConfigurationManager>();

        private Mock<ICounterFactory> mockCounterFactory = new Mock<ICounterFactory>();

        private readonly ILogger logger = new ConsoleLogger();

        private Mock<IExportStorageProvider> mockExportStorage = new Mock<IExportStorageProvider>();

        private Mock<IPcfProxyService> mockPcfProxyService = new Mock<IPcfProxyService>();

        private Mock<IRequestContext> mockRequestContext = new Mock<IRequestContext>();

        private Mock<ICloudBlobFactory> mockCloudBlobFactory = new Mock<ICloudBlobFactory>();

        [TestInitialize]
        public void Initialize()
        {
            // Setup Mock for IPrivacyConfigurationManager
            mockConfigurationManager
                .SetupGet(p => p.PrivacyExperienceServiceConfiguration)
                .Returns(GeneratePXSConfigForPrivacyExport());

            // Setup Mock for IExportStorageProvider
            mockExportStorage
                .Setup(p => p.CreateStatusHistoryRecordHelperAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(GenerateExportHistoryRecordHelper()));

            // Setup Mock for ICloudBlobFactory
            mockCloudBlobFactory
                 .Setup(p => p.GetCloudBlob(It.IsAny<Uri>()))
                 .Returns(GenerateMockBlob());

            // Setup a mock request context with watchdog parameter set to true to allow
            // the mock pcf proxy service call to response for the ListRequestsByCallerMsaAsync operation
            mockRequestContext
                .SetupGet(p => p.IsWatchdogRequest)
                .Returns(false);
        }

        /// <summary>
        /// This test will cover the return values from PXS when we are asked
        /// for a list of export requests. We will verify that we don't send
        /// back bad blob uri's for failed requests and that we return all
        /// good requests in the expected results.
        /// </summary>
        [DataTestMethod]
        [DataRow(3,0,0,0)]
        [DataRow(6,6,0,0)]
        [DataRow(3,0,3,0)]
        [DataRow(5,3,5,3)]
        public void ExportServiceListTest(
            int numberOfCompletedExportRequests, 
            int numberOfCompletedExportRequestsWitMissingURI,
            int numberOfIncompleteExportRequests,
            int numberOfIncompleteExportRequestsWithMissingURI)
        {
            // Calculate the expected number of Complete/Incomplete/Total Export Requests
            int totalCompletedRequests = numberOfCompletedExportRequests;
            int totalIncompletedRequests = numberOfCompletedExportRequestsWitMissingURI + 
                numberOfIncompleteExportRequests + numberOfIncompleteExportRequestsWithMissingURI;
            int totalRequests = totalCompletedRequests + totalIncompletedRequests;

            // Setup the PCF Proxy Service to return a test service response
            mockPcfProxyService
                .Setup(p => p.ListRequestsByCallerMsaAsync(It.IsAny<IRequestContext>(), It.IsAny<RequestType[]>()))
                .Returns(Task.FromResult(CreateTestServiceResponse(numberOfCompletedExportRequests, numberOfCompletedExportRequestsWitMissingURI,
                                                                   numberOfIncompleteExportRequests, numberOfIncompleteExportRequestsWithMissingURI)));

            // Setup the PXS Export Service using mocked objects
            ExportService exportService = new ExportService(mockConfigurationManager.Object, 
                mockCounterFactory.Object, logger, mockExportStorage.Object, mockPcfProxyService.Object, mockCloudBlobFactory.Object);
            
            // Verify the export service list history response contains all the expected records
            var response = exportService.ListExportHistoryAsync(mockRequestContext.Object);
            Assert.AreEqual(totalRequests, response.Result.Result.Exports.Count);

            // Verify the response contains the expected number completed export requests
            Assert.AreEqual(totalCompletedRequests, response.Result.Result.Exports.Count(p => p.IsComplete == true));
            Assert.AreEqual(totalCompletedRequests, response.Result.Result.Exports.Count(p => p.ZipFileUri != null));

            // Verify the response contains the expected number of incomplete export requests
            Assert.AreEqual(totalIncompletedRequests, response.Result.Result.Exports.Count(p => p.IsComplete == false));
            Assert.AreEqual(totalIncompletedRequests, response.Result.Result.Exports.Count(p => p.ZipFileUri == null));
        }

        /// <summary>
        /// Create a Privacy Experience Service Configuration Mock Object
        /// </summary>
        private static IPrivacyExperienceServiceConfiguration GeneratePXSConfigForPrivacyExport(
            int exportQueueTimeToLiveHours = 1,
            int exportQueueMessageInitialVisibilitySeconds = 120,
            int exportQueueMessageSubsequentVisibilitySeconds = 120,
            bool exportRequestThrottleEnabled = false,
            int exportRequestThrottleWindowInHours = 1,
            int exportRequestThrottleMaxCompleted = 12,
            int exportRequestThrottleMaxCancelled = 12,
            int listExportHistoryMax = 12)
        {
            // Mock the export configuration
            var mockExportConfiguration = new Mock<IPrivacyExportConfiguration>();

            // Setup property 'ExportQueueTimeToLiveHours'.
            mockExportConfiguration
                .SetupGet(p => p.ExportQueueTimeToLiveHours)
                .Returns(exportQueueTimeToLiveHours);

            // Setup property 'ExportQueueMessageInitialVisibilitySeconds'.
            mockExportConfiguration
                .SetupGet(p => p.ExportQueueMessageInitialVisibilitySeconds)
                .Returns(exportQueueMessageInitialVisibilitySeconds);

            // Setup property 'ExportQueueMessageSubsequentVisibilitySeconds'.
            mockExportConfiguration
                .SetupGet(p => p.ExportQueueMessageSubsequentVisibilitySeconds)
                .Returns(exportQueueMessageSubsequentVisibilitySeconds);

            // Setup property 'ExportRequestThrottleEnabled'.
            mockExportConfiguration
                .SetupGet(p => p.ExportRequestThrottleEnabled)
                .Returns(exportRequestThrottleEnabled);

            // Setup property 'ExportRequestThrottleWindowInHours'.
            mockExportConfiguration
                .SetupGet(p => p.ExportRequestThrottleWindowInHours)
                .Returns(exportRequestThrottleWindowInHours);

            // Setup property 'ExportQueueTimeToLiveHours'.
            mockExportConfiguration
                .SetupGet(p => p.ExportRequestThrottleMaxCompleted)
                .Returns(exportRequestThrottleMaxCompleted);

            // Setup property 'ExportRequestThrottleMaxCancelled'.
            mockExportConfiguration
                .SetupGet(p => p.ExportRequestThrottleMaxCancelled)
                .Returns(exportRequestThrottleMaxCancelled);

            // Setup property 'ListExportHistoryMax'.
            mockExportConfiguration
                .SetupGet(p => p.ListExportHistoryMax)
                .Returns(listExportHistoryMax);

            // Setup Mock for IPrivacyExperienceServiceConfiguration
            var mockConfiguration = new Mock<IPrivacyExperienceServiceConfiguration>();
            mockConfiguration
                .SetupGet(p => p.PrivacyExportConfiguration)
                .Returns(mockExportConfiguration.Object);

            return mockConfiguration.Object;
        }

        /// <summary>
        /// Create a Cloud Blob Mock Object
        /// </summary>
        private static CloudBlob GenerateMockBlob()
        {
            Mock<CloudBlob> mockCloudBlob = new Mock<CloudBlob>(new Uri("https://tempuri.org/blob"));
            mockCloudBlob
                .Setup(p => p.FetchAttributesAsync())
                .Returns(Task.FromResult(true));
            return mockCloudBlob.Object;
        }

        /// <summary>
        /// Create a Export History Record Helper Mock Object
        /// </summary>
        private static IExportHistoryRecordHelper GenerateExportHistoryRecordHelper()
        {
            var mockExportHistoryRecordHelper = new Mock<IExportHistoryRecordHelper>();
            mockExportHistoryRecordHelper
                .Setup(p => p.GetHistoryRecordsAsync(It.IsAny<bool>()))
                .Returns(Task.FromResult<ExportStatusHistoryRecordCollection>(null));
            return mockExportHistoryRecordHelper.Object;
        }

        /// <summary>
        /// Generate a test service response with a given number of completed requests and incompleted requests
        /// </summary>
        private static ServiceResponse<IList<PrivacyRequestStatus>> CreateTestServiceResponse(
            int numberOfCompletedExportRequests,
            int numberOfCompletedExportRequestsWithMissingURI,
            int numberOfIncompleteExportRequests,
            int numberOfIncompleteExportRequestsWithMissingURI)
        {
            // Allocate a full list of all privacy requests
            int totalRequests = numberOfCompletedExportRequests + numberOfCompletedExportRequestsWithMissingURI +
                numberOfIncompleteExportRequests + numberOfIncompleteExportRequestsWithMissingURI;
            var privacyRequests = new List<PrivacyRequestStatus>(totalRequests);

            // Add the privacy requests list all types of test export
            privacyRequests.AddRange(CreateTestExports(numberOfCompletedExportRequests, true, true));
            privacyRequests.AddRange(CreateTestExports(numberOfCompletedExportRequestsWithMissingURI, false, true));
            privacyRequests.AddRange(CreateTestExports(numberOfIncompleteExportRequests, true, false));
            privacyRequests.AddRange(CreateTestExports(numberOfIncompleteExportRequestsWithMissingURI, false, false));

            return new ServiceResponse<IList<PrivacyRequestStatus>> { Result = privacyRequests };
        }

        /// <summary>
        /// Generate a test export privacy request with/without destination uri in/out of the completed state
        /// </summary>
        private static List<PrivacyRequestStatus> CreateTestExports(int numberOfRequests, bool setDestinationURI, bool isComplete)
        {
            // Add a given number completed test export requests
            var privacyRequests = new List<PrivacyRequestStatus>(numberOfRequests);
            Guid[] completedGuids = new Guid[numberOfRequests];
            completedGuids = completedGuids.Select(id => Guid.NewGuid()).ToArray();
            foreach (Guid guid in completedGuids)
            {
                if (setDestinationURI)
                {
                    privacyRequests.Add(CreateSingleTestExportRequestStatus(guid, new Uri("https://azurestorage"), isComplete));
                }
                else
                {
                    privacyRequests.Add(CreateSingleTestExportRequestStatus(guid, null, isComplete));

                }
            }
            return privacyRequests;
        }

        /// <summary>
        /// Create a test export request for a given id and state.
        /// </summary>
        private static PrivacyRequestStatus CreateSingleTestExportRequestStatus(Guid id, Uri destinationUri, bool isCompleted)
        {
            var requestType = PrivacyRequestType.Export;
            var submittedTime = DateTimeOffset.UtcNow.AddDays(-2);
            var subject = new Mock<IPrivacySubject>().Object;
            var dataTypes = new[] { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value };
            string context = "testcontext";
            double completionSuccessRate = 4.3;

            // All test privacy requests in both the completed and submitted state to be created
            PrivacyRequestState state;
            DateTimeOffset completedTime;

            if (isCompleted)
            {
                state = PrivacyRequestState.Completed;
                completedTime = DateTimeOffset.UtcNow.AddDays(-1);
            }
            else
            {
                state = PrivacyRequestState.Submitted;
                completedTime = DateTimeOffset.UtcNow.AddDays(1);
            }

            return new PrivacyRequestStatus(id,
                requestType,
                submittedTime,
                completedTime,
                subject,
                dataTypes,
                context,
                state,
                destinationUri,
                completionSuccessRate);
        }
    }
}
