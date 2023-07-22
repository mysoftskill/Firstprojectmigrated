namespace Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.PreVerifierWorker.UnitTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.ComplianceServices.Common.Queues;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.ScheduleWorker;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.ScheduleDbClient;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Exceptions;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Model;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class RecurrentDeleteScheduleWorkerTests
    {
        private readonly Mock<ICloudQueue<RecurrentDeleteScheduleDbDocument>> mockCloudQueue = new Mock<ICloudQueue<RecurrentDeleteScheduleDbDocument>>();
        private readonly Mock<ICloudQueueConfiguration> mockCloudQueueConfiguration = new Mock<ICloudQueueConfiguration>();
        private readonly Mock<IScheduleDbConfiguration> mockScheduleDbConfiguration = new Mock<IScheduleDbConfiguration>();
        private readonly Mock<IAppConfiguration> mockAppConfiguration = new Mock<IAppConfiguration>();
        private readonly Mock<IScheduleDbClient> mockScheduleDbClient = new Mock<IScheduleDbClient>();
        private readonly Mock<IPcfProxyService> mockPcfProxyService = new Mock<IPcfProxyService>();
        private readonly Mock<IPxfDispatcher> mockPxfDispatcher = new Mock<IPxfDispatcher>();
        private readonly Mock<ILogger> mockLogger = new Mock<ILogger>();

        private RecurrentDeleteScheduleWorker recurrentDeleteScheduleWorker = null;

        [TestInitialize]
        public void Setup()
        {
            this.mockPcfProxyService.Setup(c => c.PostMsaRecurringDeleteRequestsAsync(It.IsAny<IRequestContext>(), It.IsAny<DeleteRequest>(), It.IsAny<string>()))
                .ReturnsAsync(new ServiceResponse());
            this.mockPxfDispatcher.Setup(c => c.CreateDeletePolicyDataTypeTask(It.IsAny<string>(), It.IsAny<PxfRequestContext>()))
                .Returns<Task<DeletionResponse<DeleteResourceResponse>>>(null);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(It.IsAny<string>(), true)).ReturnsAsync(true);
            this.mockScheduleDbConfiguration.Setup(c => c.MaxNumberOfRetries).Returns(10);
            this.mockScheduleDbConfiguration.Setup(c => c.RetryTimeDurationMinutes).Returns(1);

            this.recurrentDeleteScheduleWorker = new RecurrentDeleteScheduleWorker(
                    cloudQueue: this.mockCloudQueue.Object,
                    cloudQueueConfiguration: this.mockCloudQueueConfiguration.Object,
                    scheduleDbConfiguration: this.mockScheduleDbConfiguration.Object,
                    appConfiguration: this.mockAppConfiguration.Object,
                    scheduleDbClient: this.mockScheduleDbClient.Object,
                    pcfProxyService: this.mockPcfProxyService.Object,
                    pxfDispatcher: this.mockPxfDispatcher.Object,
                    logger: this.mockLogger.Object);
        }

        [TestMethod]
        public async Task ProcessRecurrentDeleteScheduleDbDocumentSucceeds()
        {
            // arrange
            DateTimeOffset nextDeleteOccurrenceTime = DateTimeOffset.UtcNow;
            var scheduleDbDocument = new RecurrentDeleteScheduleDbDocument(
                puidValue: 123,
                dataType: Policies.Current.DataTypes.Ids.BrowsingHistory.Value,
                preVerifier: "preverifier",
                lastDeleteOccurrence: null,
                nextDeleteOccurrence: nextDeleteOccurrenceTime,
                lastSucceededDeleteOccurrence: null,
                numberOfRetries: 0,
                documentId: Guid.NewGuid().ToString(),
                recurringIntervalDays: ExperienceContracts.V2.RecurringIntervalDays.Days2);
            this.mockScheduleDbClient.Setup(c => c.CreateOrUpdateRecurringDeletesScheduleDbAsync(It.IsAny<RecurrentDeleteScheduleDbDocument>())).ReturnsAsync(scheduleDbDocument);

            // act
            await this.recurrentDeleteScheduleWorker.ProcessRecurrentDeleteScheduleDbDocumentInstrumentedAsync(scheduleDbDocument, null).ConfigureAwait(false);

            // assert
            Assert.AreEqual(0, scheduleDbDocument.NumberOfRetries, "Retry shoudn't be triggered");
            Assert.IsNotNull(scheduleDbDocument.LastDeleteOccurrenceUtc, "Should be updated");
            Assert.IsNotNull(scheduleDbDocument.LastSucceededDeleteOccurrenceUtc, "Should be updated");
            Assert.AreNotEqual(nextDeleteOccurrenceTime, scheduleDbDocument.NextDeleteOccurrenceUtc, "Should be different than the old value");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ProcessRecurrentDeleteScheduleDbDocumentRetry()
        {
            // arrange
            DateTimeOffset nextDeleteOccurrenceTime = DateTimeOffset.UtcNow;
            var scheduleDbDocument = new RecurrentDeleteScheduleDbDocument(
                puidValue: 123,
                dataType: Policies.Current.DataTypes.Ids.BrowsingHistory.Value,
                preVerifier: "preverifier",
                lastDeleteOccurrence: null,
                nextDeleteOccurrence: nextDeleteOccurrenceTime,
                lastSucceededDeleteOccurrence: null,
                numberOfRetries: 0,
                documentId: Guid.NewGuid().ToString(),
                recurringIntervalDays: ExperienceContracts.V2.RecurringIntervalDays.Days2);
            this.mockPcfProxyService.Setup(c => c.PostMsaRecurringDeleteRequestsAsync(It.IsAny<IRequestContext>(), It.IsAny<DeleteRequest>(), It.IsAny<string>()))
                .ReturnsAsync(new ServiceResponse { Error = new Error(ErrorCode.PartnerError, "PartnerError") });
            this.mockScheduleDbClient.Setup(c => c.CreateOrUpdateRecurringDeletesScheduleDbAsync(It.IsAny<RecurrentDeleteScheduleDbDocument>())).ReturnsAsync(scheduleDbDocument);

            // act
            try
            {
                await this.recurrentDeleteScheduleWorker.ProcessRecurrentDeleteScheduleDbDocumentInstrumentedAsync(scheduleDbDocument, null).ConfigureAwait(false);
            }
            catch(InvalidOperationException)
            {
                // assert
                Assert.AreEqual(1, scheduleDbDocument.NumberOfRetries, "Retry should be triggered");
                Assert.IsNotNull(scheduleDbDocument.LastDeleteOccurrenceUtc, "Should be updated");
                Assert.IsNull(scheduleDbDocument.LastSucceededDeleteOccurrenceUtc, "Should not be updated");
                Assert.AreNotEqual(nextDeleteOccurrenceTime, scheduleDbDocument.NextDeleteOccurrenceUtc, "Should be different than the old value");
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ScheduleDbClientException))]
        public async Task ProcessRecurrentDeleteScheduleDbDocumentSucceedsButUpdateDbKeepsThrowException()
        {
            // arrange
            DateTimeOffset nextDeleteOccurrenceTime = DateTimeOffset.UtcNow;
            var scheduleDbDocument = new RecurrentDeleteScheduleDbDocument(
                puidValue: 123,
                dataType: Policies.Current.DataTypes.Ids.BrowsingHistory.Value,
                preVerifier: "preverifier",
                lastDeleteOccurrence: null,
                nextDeleteOccurrence: nextDeleteOccurrenceTime,
                lastSucceededDeleteOccurrence: null,
                numberOfRetries: 0,
                documentId: Guid.NewGuid().ToString(),
                recurringIntervalDays: ExperienceContracts.V2.RecurringIntervalDays.Days2);
            this.mockScheduleDbClient.Setup(c => c.CreateOrUpdateRecurringDeletesScheduleDbAsync(It.IsAny<RecurrentDeleteScheduleDbDocument>())).Throws(new ScheduleDbClientException());
            var mostRecentDoc = new RecurrentDeleteScheduleDbDocument(
                puidValue: 123,
                dataType: Policies.Current.DataTypes.Ids.BrowsingHistory.Value,
                preVerifier: "preverifier",
                lastDeleteOccurrence: null,
                nextDeleteOccurrence: nextDeleteOccurrenceTime,
                lastSucceededDeleteOccurrence: null,
                numberOfRetries: 0,
                documentId: Guid.NewGuid().ToString(),
                recurringIntervalDays: ExperienceContracts.V2.RecurringIntervalDays.Days2);
            this.mockScheduleDbClient.Setup(c => c.GetRecurringDeletesScheduleDbDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(mostRecentDoc);
            
            // act
            try
            {
                await this.recurrentDeleteScheduleWorker.ProcessRecurrentDeleteScheduleDbDocumentInstrumentedAsync(scheduleDbDocument, null).ConfigureAwait(false);
            }
            catch (ScheduleDbClientException)
            {
                // assert
                Assert.AreEqual(1, scheduleDbDocument.NumberOfRetries, "Retry should be triggered");
                Assert.IsNotNull(scheduleDbDocument.LastDeleteOccurrenceUtc, "Should be updated");
                Assert.IsNotNull(scheduleDbDocument.LastSucceededDeleteOccurrenceUtc, "Should be updated");
                Assert.AreNotEqual(nextDeleteOccurrenceTime, scheduleDbDocument.NextDeleteOccurrenceUtc, "Should be different than the old value");
                Assert.IsTrue(scheduleDbDocument.NextDeleteOccurrenceUtc < nextDeleteOccurrenceTime.AddMinutes(2), "Should be updated with RetryTimeDurationMinutes instead of RecurringIntervalDays");
                throw;
            }
        }

    }
}
