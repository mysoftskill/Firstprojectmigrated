// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.ScheduleDbClient.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.ScheduleDbClient;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Exceptions;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Model;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    /// <summary>
    ///     ScheduleDbClient Tests
    /// </summary>
    [TestClass]
    public class ScheduleDbClientTests
    {
        private const int Puid = 1;

        private const string DataType = "dataType";

        private const string InValidDataType = "inValidDataType";
        
        private const string NewDataType = "new Datatype";

        private const string DocumentId = "documentId";

        private const string InValidDocumentId = "inValidDocumentId";
        
        private static DateTimeOffset PreVerifierExpirationDate = new DateTimeOffset();

        private static DateTimeOffset ExpectedNextDeleteOccuranceUtc = new DateTimeOffset();

        private RecurrentDeleteStatus DeleteStatus = RecurrentDeleteStatus.Active;

        private RecurrentDeleteScheduleDbDocument recurrentDeleteScheduleDbDocument;

        private Mock<IPrivacyConfigurationManager> mockPrivacyConfig;

        private IScheduleDbClient scheduleDbClient;

        private Mock<CosmosClient> mockCosmosClient;

        private Mock<Container> mockContainer;

        private Mock<FeedIterator<RecurrentDeleteScheduleDbDocument>> mockFeedIterator;

        private readonly Mock<IAppConfiguration> mockAppConfiguration = new Mock<IAppConfiguration>();

        Mock<ItemResponse<RecurrentDeleteScheduleDbDocument>> mockItemResponse;

        Mock<IRecurringDeleteWorkerConfiguration> mockRecurringDeleteWorkerConfig;

        private List<string> validQueries = new List<string> 
        {
            $"SELECT * FROM ScheduleDb sdb WHERE sdb.puid = {Puid}",
            $"SELECT * FROM ScheduleDb sdb WHERE sdb.id = \"{DocumentId}\"",
            $"SELECT * FROM ScheduleDb sdb WHERE sdb.puid = {Puid} AND sdb.dataType = \"{DataType}\"",
            $"SELECT * FROM ScheduleDb sdb WHERE sdb.preVerifierExpirationDateUtc <= \"{PreVerifierExpirationDate.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")}\" AND sdb.recurrentDeleteStatus = {(int)RecurrentDeleteStatus.Active}",
            $"SELECT * FROM ScheduleDb sdb WHERE sdb.nextDeleteOccurrenceUtc <= \"{ExpectedNextDeleteOccuranceUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")}\" AND sdb.recurrentDeleteStatus = {(int)RecurrentDeleteStatus.Active}"
        };

        private List<string> inValidQueries = new List<string>
        {
            $"SELECT * FROM ScheduleDb sdb WHERE sdb.puid = 0 AND sdb.dataType = \"{InValidDataType}\"",
            $"SELECT * FROM ScheduleDb sdb WHERE sdb.puid = 1 AND sdb.dataType = \"{NewDataType}\"",
            $"SELECT * FROM ScheduleDb sdb WHERE sdb.nextDeleteOccurrenceUtc <= \"0001-01-02T00:00:00.0000000Z\" AND sdb.recurrentDeleteStatus = 1",
            $"SELECT * FROM ScheduleDb sdb WHERE sdb.preVerifierExpirationDateUtc <= \"0001-01-02T00:00:00.0000000Z\" AND sdb.recurrentDeleteStatus = 1",
            $"SELECT * FROM ScheduleDb sdb WHERE sdb.puid = 0",
            $"SELECT * FROM ScheduleDb sdb WHERE sdb.id = \"{InValidDocumentId}\""
        };
        
        [TestInitialize]
        public void Init()
        {
            var mockScheduleDbConfig = new Mock<IScheduleDbConfiguration>();
            mockScheduleDbConfig.Setup(a => a.CosmosDbEndPoint).Returns("CosmosDbEndPoint");
            mockScheduleDbConfig.Setup(a => a.DataBaseName).Returns("Database");
            mockScheduleDbConfig.Setup(a => a.ContainerName).Returns("Container");

            this.mockRecurringDeleteWorkerConfig = new Mock<IRecurringDeleteWorkerConfiguration>();
            this.mockRecurringDeleteWorkerConfig.Setup(a => a.ScheduleDbConfig).Returns(mockScheduleDbConfig.Object);
            this.mockRecurringDeleteWorkerConfig.Setup(a => a.EnablePreVerifierWorker).Returns(true);

            this.mockPrivacyConfig = new Mock<IPrivacyConfigurationManager>();
            this.mockPrivacyConfig.Setup(a => a.RecurringDeleteWorkerConfiguration).Returns(this.mockRecurringDeleteWorkerConfig.Object);

            this.recurrentDeleteScheduleDbDocument = new RecurrentDeleteScheduleDbDocument(
                Puid,
                DataType,
                DocumentId,
                "preVerifier",
                PreVerifierExpirationDate,
                nextDeleteOccurrence : ExpectedNextDeleteOccuranceUtc,
                status : this.DeleteStatus);

            this.mockItemResponse = new Mock<ItemResponse<RecurrentDeleteScheduleDbDocument>>();
            this.mockItemResponse.Setup(a => a.Resource).Returns(this.recurrentDeleteScheduleDbDocument);

            this.mockContainer = new Mock<Container>();
            
            var mockFeedResponse = new Mock<FeedResponse<RecurrentDeleteScheduleDbDocument>>();
            mockFeedResponse.Setup(a => a.GetEnumerator()).Returns(new List<RecurrentDeleteScheduleDbDocument> { this.recurrentDeleteScheduleDbDocument}.GetEnumerator());
            this.mockFeedIterator = new Mock<FeedIterator<RecurrentDeleteScheduleDbDocument>>();
            this.mockFeedIterator.Setup(a => a.HasMoreResults).Returns(true);
            this.mockFeedIterator.Setup(a => a.ReadNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockFeedResponse.Object)
                .Callback(() => this.mockFeedIterator.Setup(f => f.HasMoreResults).Returns(false));

            var emptyFeedIterator = new Mock<FeedIterator<RecurrentDeleteScheduleDbDocument>>();
            emptyFeedIterator.Setup(a => a.HasMoreResults).Returns(false);
            this.inValidQueries.ForEach(q => this.mockContainer.Setup(a => a.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                q,
                null,
                It.IsAny<QueryRequestOptions>())).Returns(emptyFeedIterator.Object));

            this.validQueries.ForEach(q => this.mockContainer.Setup(a => a.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                q,
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>())).Returns(this.mockFeedIterator.Object));

            this.mockContainer.Setup(a => a.ReadItemAsync<RecurrentDeleteScheduleDbDocument>(
                "new DocumentId",
                new PartitionKey("new DocumentId"),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>())).Throws(new CosmosException("Not Found", System.Net.HttpStatusCode.NotFound, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<double>()));
            this.mockContainer.Setup(a => a.ReadItemAsync<RecurrentDeleteScheduleDbDocument>(DocumentId, new PartitionKey(DocumentId), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(this.mockItemResponse.Object);
            this.mockContainer.Setup(a => a.DeleteItemAsync<RecurrentDeleteScheduleDbDocument>(DocumentId, new PartitionKey(DocumentId), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(this.mockItemResponse.Object);
            this.mockCosmosClient = new Mock<CosmosClient>();
            this.mockCosmosClient.Setup(a => a.GetContainer(It.IsAny<string>(), It.IsAny<string>())).Returns(this.mockContainer.Object);
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.ScheduleDbDiagnosticLoggingEnabled, It.IsAny<bool>())).ReturnsAsync(false);

            this.scheduleDbClient = new ScheduleDbCosmosClient(this.mockCosmosClient.Object, this.mockPrivacyConfig.Object, this.mockAppConfiguration.Object, new Mock<ILogger>().Object);
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public void RecurrentDeleteScheduleDbDocumentThrowArgumentNullExceptionInValidArgs()
        {
            try
            {
                var recurrentDeleteRecord = new RecurrentDeleteScheduleDbDocument(
                    Puid,
                    null,
                    DocumentId,
                    "preVerifier",
                    PreVerifierExpirationDate);
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Value cannot be null." + Environment.NewLine + "Parameter name: dataType", ex.Message);
                throw;
            }

            Assert.Fail("Exception should have been thrown.");
        }

        [TestMethod]
        public async Task GetRecurringDeletesScheduleDbAsyncValidArgs()
        {
            try
            {
                this.SetupMockQueryDefinition(true);

                var getResponse = await this.scheduleDbClient.GetRecurringDeletesScheduleDbAsync(Puid, CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(getResponse);
                Assert.AreEqual(1, getResponse.Count);
                Assert.AreEqual(Puid, getResponse.First().Puid);
                Assert.AreEqual(DocumentId, getResponse.First().DocumentId);
                Assert.AreEqual(DataType, getResponse.First().DataType);

                this.mockContainer.Verify(c => c.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                    It.Is<QueryDefinition>(q => (q.QueryText == "SELECT * FROM ScheduleDb sdb WHERE sdb.puid = @puid")),
                    It.IsAny<string>(),
                    It.IsAny<QueryRequestOptions>()), Times.Once);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }

        [TestMethod]
        public async Task GetRecurringDeletesScheduleDbAsyncInValidPuid()
        {
            try
            {
                this.SetupMockQueryDefinition(false);

                var getResponse = await this.scheduleDbClient.GetRecurringDeletesScheduleDbAsync(0, CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(getResponse);
                Assert.AreEqual(0, getResponse.Count);

                this.mockContainer.Verify(c => c.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                     It.Is<QueryDefinition>(q => q.QueryText == "SELECT * FROM ScheduleDb sdb WHERE sdb.puid = @puid"),
                     It.IsAny<string>(),
                     It.IsAny<QueryRequestOptions>()), Times.Once);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }

        [TestMethod]
        public async Task GetRecurringDeletesScheduleDbDocumentByDocumentIdAsyncValidArgs()
        {
            try
            {
                var getResponse = await this.scheduleDbClient.GetRecurringDeletesScheduleDbDocumentAsync(DocumentId, CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(getResponse);
                Assert.AreEqual(Puid, getResponse.Puid);
                Assert.AreEqual(DocumentId, getResponse.DocumentId);
                Assert.AreEqual(DataType, getResponse.DataType);

                this.mockContainer.Verify(c => c.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                     It.Is<string>(q => q.Contains("SELECT * FROM ScheduleDb sdb WHERE sdb.id")),
                     It.IsAny<string>(),
                     It.IsAny<QueryRequestOptions>()), Times.Once);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }

        [TestMethod]
        public async Task GetRecurringDeletesScheduleDbDocumentByDocumentIdAsyncInValidArgs()
        {
            try
            {
                var getResponse = await this.scheduleDbClient.GetRecurringDeletesScheduleDbDocumentAsync(InValidDocumentId, CancellationToken.None).ConfigureAwait(false);

                Assert.IsNull(getResponse);

                this.mockContainer.Verify(c => c.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                     It.Is<string>(q => q.Contains("SELECT * FROM ScheduleDb sdb WHERE sdb.id")),
                     It.IsAny<string>(),
                     It.IsAny<QueryRequestOptions>()), Times.Once);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }

        [TestMethod]
        public async Task GetRecurringDeletesScheduleDbDocumentByPuidAndDataTypeAsyncValidArgs()
        {
            try
            {
                this.SetupMockQueryDefinition(true);

                var getResponse = await this.scheduleDbClient.GetRecurringDeletesScheduleDbDocumentAsync(Puid, DataType, CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(getResponse);
                Assert.AreEqual(Puid, getResponse.Puid);
                Assert.AreEqual(DocumentId, getResponse.DocumentId);
                Assert.AreEqual(DataType, getResponse.DataType);

                this.mockContainer.Verify(c => c.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                     It.Is<QueryDefinition>(q => (q.QueryText == "SELECT * FROM ScheduleDb sdb WHERE sdb.puid = @puid AND sdb.dataType = @dataType")),
                     It.IsAny<string>(),
                     It.IsAny<QueryRequestOptions>()), Times.Once);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }

        [TestMethod]
        public async Task GetRecurringDeletesScheduleDbDocumentByPuidAndDataTypeAsyncInValidArgs()
        {
            try
            {
                this.SetupMockQueryDefinition(false);

                var getResponse = await this.scheduleDbClient.GetRecurringDeletesScheduleDbDocumentAsync(0, InValidDataType, CancellationToken.None).ConfigureAwait(false);

                Assert.IsNull(getResponse);

                this.mockContainer.Verify(c => c.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                     It.Is<QueryDefinition>(q => (q.QueryText == "SELECT * FROM ScheduleDb sdb WHERE sdb.puid = @puid AND sdb.dataType = @dataType")),
                     It.IsAny<string>(),
                     It.IsAny<QueryRequestOptions>()), Times.Once);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }

        [TestMethod]
        public async Task GetExpiredPreVerifiersRecurringDeletesScheduleDbAsyncValidArgs()
        {
            try
            {
                this.SetupMockQueryDefinition(true);

                var getResponse = await this.scheduleDbClient.GetExpiredPreVerifiersRecurringDeletesScheduleDbAsync(PreVerifierExpirationDate).ConfigureAwait(false);

                var items = getResponse.Item1;
                var token = getResponse.Item2;

                Assert.IsNotNull(getResponse);
                Assert.IsNotNull(items);
                Assert.IsNull(token);
                Assert.AreEqual(1, items.Count);
                Assert.AreEqual(Puid, items.First().Puid);
                Assert.AreEqual(DocumentId, items.First().DocumentId);
                Assert.AreEqual(DataType, items.First().DataType);
                Assert.AreEqual(PreVerifierExpirationDate, items.First().PreVerifierExpirationDateUtc);
                Assert.AreEqual(this.DeleteStatus, items.First().RecurrentDeleteStatus);

                this.mockContainer.Verify(c => c.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                     It.Is<QueryDefinition>(q => (q.QueryText == "SELECT * FROM ScheduleDb sdb WHERE sdb.preVerifierExpirationDateUtc <= @preVerifierExpirationDate AND sdb.recurrentDeleteStatus = @status")),
                     It.IsAny<string>(),
                     It.IsAny<QueryRequestOptions>()), Times.Once);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }

        [TestMethod]
        public async Task GetExpiredPreVerifiersRecurringDeletesScheduleDbAsyncInValidArgs()
        {
            try
            {
                this.SetupMockQueryDefinition(false);

                var getResponse = await this.scheduleDbClient.GetExpiredPreVerifiersRecurringDeletesScheduleDbAsync(new DateTimeOffset().AddDays(1)).ConfigureAwait(false);

                var items = getResponse.Item1;
                var token = getResponse.Item2;

                Assert.IsNotNull(getResponse);
                Assert.IsNotNull(items);
                Assert.IsNull(token);
                Assert.AreEqual(0, items.Count);

                this.mockContainer.Verify(c => c.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                     It.Is<QueryDefinition>(q => (q.QueryText == "SELECT * FROM ScheduleDb sdb WHERE sdb.preVerifierExpirationDateUtc <= @preVerifierExpirationDate AND sdb.recurrentDeleteStatus = @status")),
                     It.IsAny<string>(),
                     It.IsAny<QueryRequestOptions>()), Times.Once);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }

        [TestMethod]
        public async Task GetApplicableRecurringDeletesScheduleDbAsyncValidArgs()
        {
            try
            {
                this.SetupMockQueryDefinition(true);

                var getResponse = await this.scheduleDbClient.GetApplicableRecurringDeletesScheduleDbAsync(ExpectedNextDeleteOccuranceUtc).ConfigureAwait(false);

                var items = getResponse.Item1;
                var token = getResponse.Item2;

                Assert.IsNotNull(getResponse);
                Assert.IsNotNull(items);
                Assert.IsNull(token);
                Assert.AreEqual(1, items.Count);
                Assert.AreEqual(Puid, items.First().Puid);
                Assert.AreEqual(DocumentId, items.First().DocumentId);
                Assert.AreEqual(DataType, items.First().DataType);
                Assert.AreEqual(ExpectedNextDeleteOccuranceUtc, items.First().NextDeleteOccurrenceUtc);
                Assert.AreEqual(this.DeleteStatus, items.First().RecurrentDeleteStatus);

                this.mockContainer.Verify(c => c.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                     It.Is<QueryDefinition>(q => (q.QueryText == "SELECT * FROM ScheduleDb sdb WHERE sdb.nextDeleteOccurrenceUtc <= @expectedNextDeleteOccurrenceUtc AND sdb.recurrentDeleteStatus = @status")),
                     It.IsAny<string>(),
                     It.IsAny<QueryRequestOptions>()), Times.Once);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }

        [TestMethod]
        public async Task GetApplicableRecurringDeletesScheduleDbAsyncInValidArgs()
        {
            try
            {
                this.SetupMockQueryDefinition(false);

                var getResponse = await this.scheduleDbClient.GetApplicableRecurringDeletesScheduleDbAsync(new DateTimeOffset().AddDays(1)).ConfigureAwait(false);

                var items = getResponse.Item1;
                var token = getResponse.Item2;

                Assert.IsNotNull(getResponse);
                Assert.IsNotNull(items);
                Assert.IsNull(token);
                Assert.AreEqual(0, items.Count);

                this.mockContainer.Verify(c => c.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                     It.Is<QueryDefinition>(q => (q.QueryText == "SELECT * FROM ScheduleDb sdb WHERE sdb.nextDeleteOccurrenceUtc <= @expectedNextDeleteOccurrenceUtc AND sdb.recurrentDeleteStatus = @status")),
                     It.IsAny<string>(),
                     It.IsAny<QueryRequestOptions>()), Times.Once);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }

        [TestMethod]
        public async Task DeleteRecurringDeletesScheduleDbByPuidAndDataTypeAsyncValidArgs()
        {
            try
            {
                this.SetupMockQueryDefinition(true);

                await this.scheduleDbClient.DeleteRecurringDeletesScheduleDbAsync(Puid, DataType, CancellationToken.None).ConfigureAwait(false);

                this.mockContainer.Verify(c => c.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                     It.Is<QueryDefinition>(q => (q.QueryText == "SELECT * FROM ScheduleDb sdb WHERE sdb.puid = @puid AND sdb.dataType = @dataType")),
                     It.IsAny<string>(),
                     It.IsAny<QueryRequestOptions>()), Times.Once);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }

        [ExpectedException(typeof(ScheduleDbClientException))]
        [TestMethod]
        public async Task DeleteRecurringDeletesScheduleDbByPuidAndDataTypeAsyncInValidArgs()
        {
            try 
            {
                this.SetupMockQueryDefinition(false);

                await this.scheduleDbClient.DeleteRecurringDeletesScheduleDbAsync(0, InValidDataType, CancellationToken.None).ConfigureAwait(false);
            }
            catch (ScheduleDbClientException ex)
            {
                Assert.AreEqual("No record found in scheduledb for delete operation", ex.Message);
                throw;
            }

            Assert.Fail("Exception should have been thrown.");
        }

        [TestMethod]
        public async Task DeleteRecurringDeletesScheduleDbByPuidAsyncValidArgs()
        {
            try
            {
                this.SetupMockQueryDefinition(true);

                await this.scheduleDbClient.DeleteRecurringDeletesByPuidScheduleDbAsync(Puid, CancellationToken.None).ConfigureAwait(false);

                this.mockContainer.Verify(c => c.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                    It.Is<QueryDefinition>(q => (q.QueryText == "SELECT * FROM ScheduleDb sdb WHERE sdb.puid = @puid")),
                    It.IsAny<string>(),
                    It.IsAny<QueryRequestOptions>()), Times.Once);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }

        [TestMethod]
        public async Task HasRecurringDeletesScheduleDbRecordAsyncValidArgs()
        {
            try
            {
                this.SetupMockQueryDefinition(true);

                var hasResponse = await this.scheduleDbClient.HasRecurringDeletesScheduleDbRecordAsync(Puid, DataType, CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(hasResponse);
                Assert.IsTrue(hasResponse);

                this.mockContainer.Verify(c => c.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                     It.Is<QueryDefinition>(q => (q.QueryText == "SELECT * FROM ScheduleDb sdb WHERE sdb.puid = @puid AND sdb.dataType = @dataType")),
                     It.IsAny<string>(),
                     It.IsAny<QueryRequestOptions>()), Times.Once);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }

        [TestMethod]
        public async Task HasRecurringDeletesScheduleDbRecordAsyncInValidArgs()
        {
            try
            {
                this.SetupMockQueryDefinition(false);

                var hasResponse = await this.scheduleDbClient.HasRecurringDeletesScheduleDbRecordAsync(0, InValidDataType, CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(hasResponse);
                Assert.IsFalse(hasResponse);

                this.mockContainer.Verify(c => c.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                     It.Is<QueryDefinition>(q => (q.QueryText == "SELECT * FROM ScheduleDb sdb WHERE sdb.puid = @puid AND sdb.dataType = @dataType")),
                     It.IsAny<string>(),
                     It.IsAny<QueryRequestOptions>()), Times.Once);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }

        [TestMethod]
        public async Task CreateRecurringDeletesScheduleDbAsyncValidArgs()
        {
            try
            {
                this.SetupMockQueryDefinition(false);

                var newDocumentId = "new DocumentId";
                var inputArguement = this.recurrentDeleteScheduleDbDocument;
                inputArguement.DataType = NewDataType;
                var document = this.recurrentDeleteScheduleDbDocument;
                document.DocumentId = newDocumentId;
                
                this.mockItemResponse.SetupGet(x => x.Resource).Returns(document);
                
                this.mockContainer.Setup(a => a.CreateItemAsync<RecurrentDeleteScheduleDbDocument>(
                    inputArguement,
                    new PartitionKey(newDocumentId),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>())).ReturnsAsync(this.mockItemResponse.Object);

                var createResponse = await this.scheduleDbClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(inputArguement).ConfigureAwait(false);

                Assert.IsNotNull(createResponse);
                Assert.AreEqual(Puid, createResponse.Puid);
                Assert.AreEqual(newDocumentId, createResponse.DocumentId);
                Assert.AreEqual(NewDataType, createResponse.DataType);
                Assert.IsNotNull(createResponse.CreateDateUtc);

                this.mockContainer.Verify(c => c.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                     It.Is<QueryDefinition>(q => (q.QueryText == "SELECT * FROM ScheduleDb sdb WHERE sdb.puid = @puid AND sdb.dataType = @dataType")),
                     It.IsAny<string>(),
                     It.IsAny<QueryRequestOptions>()), Times.Once);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }

        [ExpectedException(typeof(ScheduleDbClientException))]
        [DataTestMethod]
        [DataRow(nameof(RecurrentDeleteScheduleDbDocument.PreVerifier))]
        [DataRow(nameof(RecurrentDeleteScheduleDbDocument.PreVerifierExpirationDateUtc))]
        public async Task CreateRecurringDeletesScheduleDbAsyncInValidArgs(string arguement)
        {
            try
            {
                this.SetupMockQueryDefinition(false);

                var newDocumentId = "new DocumentId";
                var document = this.recurrentDeleteScheduleDbDocument;
                document.DocumentId = newDocumentId;
                document.DataType = NewDataType;

                switch (arguement)
                {
                    case nameof(RecurrentDeleteScheduleDbDocument.PreVerifier):
                        document.PreVerifier = null;
                        break;
                    case nameof(RecurrentDeleteScheduleDbDocument.PreVerifierExpirationDateUtc):
                        document.PreVerifierExpirationDateUtc = null;
                        break;
                }

                var createResponse = await this.scheduleDbClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(document).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains(arguement));
                throw;
            }

            Assert.Fail("Exception should have been thrown.");
        }

        [ExpectedException(typeof(ArgumentNullException))]
        public async Task CreateRecurringDeletesScheduleDbAsyncInValidDatatype()
        {
            try
            {
                var document = this.recurrentDeleteScheduleDbDocument;
                document.DataType = null;

                var createResponse = await this.scheduleDbClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(document).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Datatype"));
                throw;
            }

            Assert.Fail("Exception should have been thrown.");
        }

        [TestMethod]
        public async Task UpdateRecurringDeletesScheduleDbAsyncValidArgs()
        {
            try
            {
                this.SetupMockQueryDefinition(true);
                var status = RecurrentDeleteStatus.Paused;
                var document = this.recurrentDeleteScheduleDbDocument;
                document.RecurrentDeleteStatus = status;
                var docId = this.recurrentDeleteScheduleDbDocument.DocumentId;

                this.mockItemResponse.SetupGet(x => x.Resource).Returns(document);

                this.mockContainer.Setup(a => a.ReplaceItemAsync<RecurrentDeleteScheduleDbDocument>(
                    this.recurrentDeleteScheduleDbDocument,
                    docId,
                    new PartitionKey(docId),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>())).ReturnsAsync(this.mockItemResponse.Object);

                var updateResponse = await this.scheduleDbClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(document).ConfigureAwait(false);

                Assert.IsNotNull(updateResponse);
                Assert.AreEqual(Puid, updateResponse.Puid);
                Assert.AreEqual(docId, updateResponse.DocumentId);
                Assert.AreEqual(DataType, updateResponse.DataType);
                Assert.AreEqual(status, updateResponse.RecurrentDeleteStatus);
                Assert.IsNotNull(updateResponse.UpdateDateUtc);

                this.mockContainer.Verify(c => c.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                     It.Is<QueryDefinition>(q => (q.QueryText == "SELECT * FROM ScheduleDb sdb WHERE sdb.puid = @puid AND sdb.dataType = @dataType")),
                     It.IsAny<string>(),
                     It.IsAny<QueryRequestOptions>()), Times.Once);
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }

        private void SetupMockQueryDefinition(bool valid)
        {
            if (!valid)
            {
                var emptyFeedIterator = new Mock<FeedIterator<RecurrentDeleteScheduleDbDocument>>();
                emptyFeedIterator.Setup(a => a.HasMoreResults).Returns(false);

                this.mockContainer.Setup(a => a.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                                It.IsAny<QueryDefinition>(),
                                null,
                                It.IsAny<QueryRequestOptions>())).Returns(emptyFeedIterator.Object);
            }
            else
            {
                this.mockContainer.Setup(a => a.GetItemQueryIterator<RecurrentDeleteScheduleDbDocument>(
                                It.IsAny<QueryDefinition>(),
                                It.IsAny<string>(),
                                It.IsAny<QueryRequestOptions>())).Returns(this.mockFeedIterator.Object);
            }
        }
    }
}
