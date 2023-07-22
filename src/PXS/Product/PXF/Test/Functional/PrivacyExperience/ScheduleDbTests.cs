// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests.PrivacyExperience
{
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Extensions;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using HttpClient = OSGS.HttpClientCommon.HttpClient;
    using TestConfiguration = Test.Common.Config.TestConfiguration;

    /// <summary>
    ///     Schedule Db Functional Tests
    /// </summary>
    [TestClass]
    public class ScheduleDbTests : TestBase
    {
        /// <summary>
        ///     Test client for reaching out to ScheduleDb PXS Library
        /// </summary>
        private static IHttpClient TestHttpClient => httpClient.Value;

        private static Uri TestBaseUrl => TestConfiguration.MockBaseUrl.Value;

        private readonly ILogger logger = DualLogger.Instance;

        private static readonly Lazy<IHttpClient> httpClient = new Lazy<IHttpClient>(
            () =>
            {
                var certHandler = new WebRequestHandler();

                var client = new HttpClient(certHandler) { BaseAddress = TestConfiguration.ServiceEndpoint.Value };

                client.MessageHandler.AttachClientCertificate(TestConfiguration.S2SCert.Value);

                ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;

                return client;
            });

        /// <summary>
        ///     Creates a record in Schedule Db.
        /// </summary>
        /// <returns>test task</returns>
        [TestMethod, TestCategory("FCT")]
        public async Task CreateScheduleDbDocument()
        {
            this.logger.Information(nameof(ScheduleDbTests.CreateScheduleDbDocument), "beginning CreateScheduleDbDocument Test");
            var response = await TestHttpClient.PostAsync(new Uri(TestBaseUrl, "scheduleDb/createRecurringDeleteDocument"), null).ConfigureAwait(false);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        ///     Updates a record in Schedule Db.
        /// </summary>
        /// <returns>test task</returns>
        [TestMethod, TestCategory("FCT")]
        public async Task UpdateScheduleDbDocument()
        {
            this.logger.Information(nameof(ScheduleDbTests.UpdateScheduleDbDocument), "beginning UpdateScheduleDbDocument Test");
            var response = await TestHttpClient.PostAsync(new Uri(TestBaseUrl, "scheduleDb/updateRecurringDeleteDocument"), null).ConfigureAwait(false);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        ///     Creates/Update a record in Schedule Db.
        /// </summary>
        /// <returns>test task</returns>
        [TestMethod, TestCategory("FCT")]
        public async Task CreateUpdateScheduleDbDocument()
        {
            this.logger.Information(nameof(ScheduleDbTests.CreateScheduleDbDocument), "beginning CreateUpdateScheduleDbDocument Test");
            var response = await TestHttpClient.PostAsync(new Uri(TestBaseUrl, "scheduleDb/createUpdateRecurringDeleteDocument"), null).ConfigureAwait(false);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        ///     Delete a record in Schedule Db.
        /// </summary>
        /// <returns>test task</returns>
        [TestMethod, TestCategory("FCT")]
        public async Task DeleteScheduleDbDocument()
        {
            this.logger.Information(nameof(ScheduleDbTests.DeleteScheduleDbDocument), "beginning DeleteScheduleDbDocument Test");
            var response = await TestHttpClient.PostAsync(new Uri(TestBaseUrl, "scheduleDb/deleteRecurringDeleteDocument"), null).ConfigureAwait(false);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        ///     Get all records by puid from Schedule Db.
        /// </summary>
        /// <returns>test task</returns>
        [TestMethod, TestCategory("FCT")]
        public async Task GetScheduleDbDocumentByPuid()
        {
            this.logger.Information(nameof(ScheduleDbTests.GetScheduleDbDocumentByPuid), "beginning GetScheduleDbDocumentByPuid Test");
            var response = await TestHttpClient.PostAsync(new Uri(TestBaseUrl, "scheduleDb/getRecurringDeleteDocumentByPuid"), null).ConfigureAwait(false);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        ///     If there is a record by documentId in Schedule Db.
        /// </summary>
        /// <returns>test task</returns>
        [TestMethod, TestCategory("FCT")]
        public async Task HasScheduleDbDocumentByPuid()
        {
            this.logger.Information(nameof(ScheduleDbTests.HasScheduleDbDocumentByPuid), "beginning HasScheduleDbDocumentByPuid Test");
            var response = await TestHttpClient.PostAsync(new Uri(TestBaseUrl, "scheduleDb/hasRecurringDeleteDocument"), null).ConfigureAwait(false);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        ///     Get expired preVerifiers records from Schedule Db.
        /// </summary>
        /// <returns>test task</returns>
        [TestMethod, TestCategory("FCT")]
        public async Task GetExpiredPreVerifiersRecurringDeletes()
        {
            this.logger.Information(nameof(ScheduleDbTests.GetExpiredPreVerifiersRecurringDeletes), "beginning GetExpiredPreVerifiersRecurringDeletes Test");
            var response = await TestHttpClient.PostAsync(new Uri(TestBaseUrl, "scheduleDb/GetExpiredPreVerifiers"), null).ConfigureAwait(false);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        ///     Get next delete occurance records from Schedule Db.
        /// </summary>
        /// <returns>test task</returns>
        [TestMethod, TestCategory("FCT")]
        public async Task GetApplicableRecurringDeletes()
        {
            this.logger.Information(nameof(ScheduleDbTests.GetExpiredPreVerifiersRecurringDeletes), "beginning GetApplicableRecurringDeletes Test");
            var response = await TestHttpClient.PostAsync(new Uri(TestBaseUrl, "scheduleDb/GetApplicableRecurringDeletes"), null).ConfigureAwait(false);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        ///     Get next delete occurance records from Schedule Db.
        /// </summary>
        /// <returns>test task</returns>
        [TestMethod, TestCategory("FCT")]
        public async Task TestGetRecurringDeleteByPuidAndDataType()
        {
            this.logger.Information(nameof(ScheduleDbTests.TestGetRecurringDeleteByPuidAndDataType), "beginning TestGetRecurringDeleteByPuidAndDataType Test");
            var response = await TestHttpClient.PostAsync(new Uri(TestBaseUrl, "scheduleDb/getrecurringdeletebypuidanddatatype"), null).ConfigureAwait(false);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        ///     Get record from Schedule Db by documentId and puid.
        /// </summary>
        /// <returns>test task</returns>
        [TestMethod, TestCategory("FCT")]
        public async Task GetRecurringDeletesScheduleDbDocument()
        {
            this.logger.Information(nameof(ScheduleDbTests.GetRecurringDeletesScheduleDbDocument), "beginning GetRecurringDeletesScheduleDbDocument Test");
            var response = await TestHttpClient.PostAsync(new Uri(TestBaseUrl, "scheduleDb/getRecurringDeletesScheduleDbDocument"), null).ConfigureAwait(false);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
