// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.PreVerifierWorker.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens;
    using System.IdentityModel.Tokens.Jwt;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.Documents;
    using Microsoft.ComplianceServices.Common.Queues;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.ScheduleDbClient;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Model;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class PreVerifierWorkerTests
    {
        private readonly Mock<ICloudQueue<RecurrentDeleteScheduleDbDocument>> mockCloudQueue = new Mock<ICloudQueue<RecurrentDeleteScheduleDbDocument>>();
        private readonly Mock<ICloudQueueConfiguration> mockCloudQueueConfiguration = new Mock<ICloudQueueConfiguration>();
        private readonly Mock<IScheduleDbConfiguration> mockScheduleDbConfiguration = new Mock<IScheduleDbConfiguration>();
        private readonly Mock<IAppConfiguration> mockAppConfiguration = new Mock<IAppConfiguration>();
        private readonly Mock<IScheduleDbClient> mockScheduleDbClient = new Mock<IScheduleDbClient>();
        private readonly Mock<IMsaIdentityServiceAdapter> mockMsaIdentityServiceAdapter = new Mock<IMsaIdentityServiceAdapter>();
        private readonly Mock<ILogger> mockLogger = new Mock<ILogger>();

        private PreVerifierWorker preVerifierWorker = null;
        private RecurrentDeleteScheduleDbDocument scheduleDbDocument = null;

        [TestInitialize]
        public void Setup()
        {
            var token = this.CreateDummyJwtToken();

            this.mockMsaIdentityServiceAdapter.Setup(c => c.RenewGdprUserDeleteVerifierUsingPreverifierAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<string>()))
                .ReturnsAsync(new AdapterResponse<string> { Result = token });

            this.scheduleDbDocument = new RecurrentDeleteScheduleDbDocument(
                puidValue: 123,
                dataType: Policies.Current.DataTypes.Ids.BrowsingHistory.Value,
                preVerifier: token,
                preVerifierExpirationDateUtc: DateTimeOffset.UtcNow.AddDays(3),
                documentId: Guid.NewGuid().ToString()
            );

            this.preVerifierWorker = new PreVerifierWorker(
                    cloudQueue: this.mockCloudQueue.Object,
                    cloudQueueConfiguration: this.mockCloudQueueConfiguration.Object,
                    scheduleDbConfiguration: this.mockScheduleDbConfiguration.Object,
                    appConfiguration: this.mockAppConfiguration.Object,
                    scheduleDbClient: this.mockScheduleDbClient.Object,
                    msaIdentityServiceAdapter: this.mockMsaIdentityServiceAdapter.Object,
                    logger: this.mockLogger.Object);
        }

        [TestMethod]
        public async Task ProcessRecurrentDeleteScheduleDbDocumentSuccessTest()
        {
            OutgoingApiEventWrapper outgoingApi = null;
            this.mockScheduleDbClient.Setup(c => c.CreateOrUpdateRecurringDeletesScheduleDbAsync(It.IsAny<RecurrentDeleteScheduleDbDocument>())).ReturnsAsync(this.scheduleDbDocument);

            await this.preVerifierWorker.ProcessRecurrentDeleteScheduleDbDocumentInstrumentedAsync(this.scheduleDbDocument, outgoingApi).ConfigureAwait(false);

            this.mockMsaIdentityServiceAdapter.Verify(c => c.RenewGdprUserDeleteVerifierUsingPreverifierAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<string>()), Times.Once);
            this.mockScheduleDbClient.Verify(c => c.CreateOrUpdateRecurringDeletesScheduleDbAsync(It.IsAny<RecurrentDeleteScheduleDbDocument>()), Times.Once);
        }

        [TestMethod]
        public async Task ProcessRecurrentDeleteScheduleDbDocumentRetryTest()
        {
            try
            {
                OutgoingApiEventWrapper outgoingApi = null;
                var exception = FormatterServices.GetUninitializedObject(typeof(DocumentClientException)) as DocumentClientException;
                this.mockScheduleDbClient.Setup(c => c.CreateOrUpdateRecurringDeletesScheduleDbAsync(It.IsAny<RecurrentDeleteScheduleDbDocument>())).Throws(exception);
                this.mockScheduleDbClient.Setup(c => c.GetRecurringDeletesScheduleDbAsync(It.IsAny<long>(), CancellationToken.None)).ReturnsAsync(new List<RecurrentDeleteScheduleDbDocument> { this.scheduleDbDocument });

                await this.preVerifierWorker.ProcessRecurrentDeleteScheduleDbDocumentInstrumentedAsync(this.scheduleDbDocument, outgoingApi).ConfigureAwait(false);
            }
            catch (DocumentClientException)
            {
                this.mockMsaIdentityServiceAdapter.Verify(c => c.RenewGdprUserDeleteVerifierUsingPreverifierAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<string>()), Times.Once);
                this.mockScheduleDbClient.Verify(c => c.CreateOrUpdateRecurringDeletesScheduleDbAsync(It.IsAny<RecurrentDeleteScheduleDbDocument>()), Times.Exactly(2));
            }
        }

        private string CreateDummyJwtToken()
        {
            // Define private dummy key. There's length requirement for this key.
            string key = "1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef";

            // Create Security key and credential using private key above.
            var securityKey = new IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new IdentityModel.Tokens.SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);
            var header = new JwtHeader(credentials);

            // Create payload that only contains the expiration time which is valid for 100 days.
            var payload = new JwtPayload
            {
                { "refresh_token_expiry", DateTimeOffset.Now.AddDays(100).ToUnixTimeSeconds().ToString()}
            };

            var token = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();

            return handler.WriteToken(token);
        }
    }
}