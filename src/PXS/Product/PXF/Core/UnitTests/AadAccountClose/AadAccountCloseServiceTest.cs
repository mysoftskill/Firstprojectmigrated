// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.AadAccountClose
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAccountClose;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.VerificationTokenValidation;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     AadAccountCloseService-Test
    /// </summary>
    [TestClass]
    public class AadAccountCloseServiceTest : AadAccountCloseServiceTestBase
    {
        private const string ExpectedV2VerifierToken = "this.is.a.v2.verifier.token";

        private static readonly string[] ExpectedV3VerifierTokens = { "this.is.a.v3.verifier.token" };

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [DynamicData(nameof(AadAccountCloseServiceConstructorTestData), DynamicDataSourceType.Method)]
        public void AadAccountCloseServiceNullHandlingSuccess(
            IPcfAdapter pcfAdapter,
            IVerificationTokenValidationService verificationTokenValidationService,
            IAadRequestVerificationServiceAdapter aadRvsAdapter,
            ILogger logger,
            IAppConfiguration appConfiguration)
        {
            //Act
            new AadAccountCloseService(pcfAdapter, verificationTokenValidationService, aadRvsAdapter, logger, appConfiguration);
        }

        [TestInitialize]
        public void Initialize()
        {
            this.mockAadRvsAdapter
                .Setup(c => c.ConstructAccountCloseAsync(It.IsAny<AadRvsRequest>()))
                .ReturnsAsync(new AdapterResponse<AadRvsVerifiers> { Result = new AadRvsVerifiers { V2 = ExpectedV2VerifierToken } });
            long orgIdPuid;
            this.mockAadRvsAdapter
                .Setup(c => c.TryGetOrgIdPuid(It.IsAny<string>(), out orgIdPuid))
                .Returns(true);

            this.mockVerificationTokenValidationService
                .Setup(c => c.ValidateVerifierAsync(It.IsAny<PrivacyRequest>(), It.IsAny<string>()))
                .ReturnsAsync(new AdapterResponse());

            this.mockPcfAdapter
                .Setup(c => c.PostCommandsAsync(It.IsAny<IList<PrivacyRequest>>()))
                .ReturnsAsync(new AdapterResponse());
        }

        [TestMethod]
        public async Task PostBatchAccountCloseShouldReturnErrorFromAadRvsError()
        {
            this.mockAadRvsAdapter
                .Setup(c => c.ConstructAccountCloseAsync(It.IsAny<AadRvsRequest>()))
                .ReturnsAsync(new AdapterResponse<AadRvsVerifiers> { Error = new AdapterError(AdapterErrorCode.Unknown, "error from the partner", 500) });

            IList<IQueueItem<AccountCloseRequest>> batch = new List<IQueueItem<AccountCloseRequest>>();
            var mockQueueItem = new Mock<IQueueItem<AccountCloseRequest>>(MockBehavior.Strict);
            mockQueueItem
                .Setup(c => c.Data)
                .Returns(new AccountCloseRequest { Subject = new AadSubject { ObjectId = Guid.NewGuid(), OrgIdPUID = 123, TenantId = Guid.NewGuid() } });
            batch.Add(mockQueueItem.Object);

            var aadAccountCloseService = this.CreateAadAccountCloseService();

            var response = await aadAccountCloseService.PostBatchAccountCloseAsync(batch).ConfigureAwait(false);
            Assert.IsNotNull(response);
            Assert.AreEqual(1, response.Count);
            Assert.IsFalse(response[0].IsSuccess);

            this.mockAadRvsAdapter.Verify(c => c.ConstructAccountCloseAsync(It.IsAny<AadRvsRequest>()), Times.Once);
            this.mockVerificationTokenValidationService.Verify(c => c.ValidateVerifierAsync(It.IsAny<PrivacyRequest>(), It.IsAny<string>()), Times.Never);
            this.mockPcfAdapter.Verify(c => c.PostCommandsAsync(It.IsAny<IList<PrivacyRequest>>()), Times.Never);
        }

        [DataTestMethod]
        // The caller provided an invalid input
        [DataRow(AdapterErrorCode.InvalidInput, 400, "InvalidInput")]
        // The caller is not authorized to make a request
        [DataRow(AdapterErrorCode.Unauthorized, 401, "Unauthorized")]
        // RVS determined that the user did not have the appropriate permission to make the call
        [DataRow(AdapterErrorCode.Forbidden, 403, "Forbidden")]
        // RVS did not find the user in the specified tenant
        [DataRow(AdapterErrorCode.ResourceNotFound, 404, "ResourceNotFound")]
        // The caller is not allowed to make this call (at least one case is when a tenant admin tries to use the outbound version of removePersonalData)
        [DataRow(AdapterErrorCode.MethodNotAllowed, 405, "MethodNotAllowed")]
        // The data has changed since the entity was loaded
        [DataRow(AdapterErrorCode.ConcurrencyConflict, 409, "ConcurrencyConflict")]
        // The response contained no content (not a recognized status code so aad account close will return Partner Error)
        [DataRow(AdapterErrorCode.EmptyResponse, 410, "PartnerError")]
        // The caller has made too many repeated requests
        [DataRow(AdapterErrorCode.TooManyRequests, 429, "TooManyRequests")]
        // RVS did not return a V2 Verifier (in the event of no verifier return Partner Error)
        [DataRow(AdapterErrorCode.NullVerifier, 500, "PartnerError")]
        public async Task PostBatchAccountCloseShouldRelayCorrectErrorCodesFromAadRvs(AdapterErrorCode adapterErrorCode, int adapterHttpStatusCode, string accountCloseServiceErrorCode)
        {
            // Setup the Mock Rvs Adapter to return adapter errors based on the data row inputs
            var adapterResponseError = new AdapterResponse<AadRvsVerifiers> { Error = new AdapterError(adapterErrorCode, string.Empty, adapterHttpStatusCode) };
            this.mockAadRvsAdapter
                .Setup(c => c.ConstructAccountCloseAsync(It.IsAny<AadRvsRequest>()))
                .ReturnsAsync(adapterResponseError);

            IList<IQueueItem<AccountCloseRequest>> batch = new List<IQueueItem<AccountCloseRequest>>();
            var mockQueueItem = new Mock<IQueueItem<AccountCloseRequest>>(MockBehavior.Strict);
            mockQueueItem
                .Setup(c => c.Data)
                .Returns(new AccountCloseRequest { Subject = new AadSubject { ObjectId = Guid.NewGuid(), OrgIdPUID = 123, TenantId = Guid.NewGuid() } });
            batch.Add(mockQueueItem.Object);

            var aadAccountCloseService = this.CreateAadAccountCloseService();

            var response = await aadAccountCloseService.PostBatchAccountCloseAsync(batch).ConfigureAwait(false);
            Assert.IsNotNull(response);
            Assert.AreEqual(1, response.Count);
            Assert.AreEqual(accountCloseServiceErrorCode, response[0].Error.Code);

            this.mockAadRvsAdapter.Verify(c => c.ConstructAccountCloseAsync(It.IsAny<AadRvsRequest>()), Times.Once);
            this.mockVerificationTokenValidationService.Verify(c => c.ValidateVerifierAsync(It.IsAny<PrivacyRequest>(), It.IsAny<string>()), Times.Never);
            this.mockPcfAdapter.Verify(c => c.PostCommandsAsync(It.IsAny<IList<PrivacyRequest>>()), Times.Never);
        }

        [TestMethod]
        public async Task PostBatchAccountCloseShouldReturnErrorFromPcfError()
        {
            this.mockPcfAdapter
                .Setup(c => c.PostCommandsAsync(It.IsAny<IList<PrivacyRequest>>()))
                .ReturnsAsync(new AdapterResponse<string> { Error = new AdapterError(AdapterErrorCode.Unknown, "error from the partner", 500) });

            IList<IQueueItem<AccountCloseRequest>> batch = new List<IQueueItem<AccountCloseRequest>>();
            var mockQueueItem = new Mock<IQueueItem<AccountCloseRequest>>(MockBehavior.Strict);
            mockQueueItem
                .Setup(c => c.Data)
                .Returns(new AccountCloseRequest { Subject = new AadSubject { ObjectId = Guid.NewGuid(), OrgIdPUID = 123, TenantId = Guid.NewGuid() } });
            batch.Add(mockQueueItem.Object);

            var aadAccountCloseService = this.CreateAadAccountCloseService();

            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.MultiTenantCollaboration, true)).ReturnsAsync(false);
            var response = await aadAccountCloseService.PostBatchAccountCloseAsync(batch).ConfigureAwait(false);
            Assert.IsNotNull(response);
            Assert.AreEqual(1, response.Count);
            Assert.IsFalse(response[0].IsSuccess);

            this.mockAadRvsAdapter.Verify(c => c.ConstructAccountCloseAsync(It.IsAny<AadRvsRequest>()), Times.Once);
            this.mockVerificationTokenValidationService.Verify(c => c.ValidateVerifierAsync(It.IsAny<PrivacyRequest>(), It.IsAny<string>()), Times.Once);
            this.mockPcfAdapter.Verify(c => c.PostCommandsAsync(It.IsAny<IList<PrivacyRequest>>()), Times.Once);
        }

        [TestMethod]
        public async Task PostBatchAccountCloseShouldReturnErrorFromVerificationtokenValidationService()
        {
            this.mockVerificationTokenValidationService
                .Setup(c => c.ValidateVerifierAsync(It.IsAny<PrivacyRequest>(), It.IsAny<string>()))
                .ReturnsAsync(new AdapterResponse(new AdapterError(AdapterErrorCode.NullVerifier, "failed validation", 500)));

            IList<IQueueItem<AccountCloseRequest>> batch = new List<IQueueItem<AccountCloseRequest>>();
            var mockQueueItem = new Mock<IQueueItem<AccountCloseRequest>>(MockBehavior.Strict);
            mockQueueItem
                .Setup(c => c.Data)
                .Returns(new AccountCloseRequest { Subject = new AadSubject { ObjectId = Guid.NewGuid(), OrgIdPUID = 123, TenantId = Guid.NewGuid() } });
            batch.Add(mockQueueItem.Object);

            var aadAccountCloseService = this.CreateAadAccountCloseService();

            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.MultiTenantCollaboration, true)).ReturnsAsync(false);
            var response = await aadAccountCloseService.PostBatchAccountCloseAsync(batch).ConfigureAwait(false);
            Assert.IsNotNull(response);
            Assert.AreEqual(1, response.Count);
            Assert.IsFalse(response[0].IsSuccess);

            this.mockAadRvsAdapter.Verify(c => c.ConstructAccountCloseAsync(It.IsAny<AadRvsRequest>()), Times.Once);
            this.mockVerificationTokenValidationService.Verify(c => c.ValidateVerifierAsync(It.IsAny<PrivacyRequest>(), It.IsAny<string>()), Times.Once);
            this.mockPcfAdapter.Verify(c => c.PostCommandsAsync(It.IsAny<IList<PrivacyRequest>>()), Times.Never);
        }

        [TestMethod]
        public async Task PostBatchAccountCloseWithV2VerifierShouldReturnSuccess()
        {
            IList<IQueueItem<AccountCloseRequest>> batch = new List<IQueueItem<AccountCloseRequest>>();
            var mockQueueItem = new Mock<IQueueItem<AccountCloseRequest>>(MockBehavior.Strict);
            mockQueueItem
                .Setup(c => c.Data)
                .Returns(new AccountCloseRequest { Subject = new AadSubject { ObjectId = Guid.NewGuid(), OrgIdPUID = 123, TenantId = Guid.NewGuid() } });
            batch.Add(mockQueueItem.Object);

            var aadAccountCloseService = this.CreateAadAccountCloseService();

            // Turn feature flag off to make sure v2 verifier is used
            this.mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.MultiTenantCollaboration, true)).ReturnsAsync(false);

            var response = await aadAccountCloseService.PostBatchAccountCloseAsync(batch).ConfigureAwait(false);
            Assert.IsNotNull(response);
            Assert.AreEqual(1, response.Count);
            Assert.IsTrue(response[0].IsSuccess);

            // And queue item should be the same one.
            Assert.AreEqual(((AadSubject)mockQueueItem.Object.Data.Subject).ObjectId, ((AadSubject)response[0].Result.Data.Subject).ObjectId);
            Assert.AreEqual(((AadSubject)mockQueueItem.Object.Data.Subject).OrgIdPUID, ((AadSubject)response[0].Result.Data.Subject).OrgIdPUID);
            Assert.AreEqual(((AadSubject)mockQueueItem.Object.Data.Subject).TenantId, ((AadSubject)response[0].Result.Data.Subject).TenantId);

            this.mockAadRvsAdapter.Verify(c => c.ConstructAccountCloseAsync(It.IsAny<AadRvsRequest>()), Times.Once);
            this.mockVerificationTokenValidationService.Verify(c => c.ValidateVerifierAsync(It.IsAny<PrivacyRequest>(), It.IsAny<string>()), Times.Once);
            this.mockPcfAdapter.Verify(c => c.PostCommandsAsync(It.IsAny<IList<PrivacyRequest>>()), Times.Once);
        }

        [TestMethod]
        public async Task PostBatchAccountCloseShouldReturnWithZeroBatchItems()
        {
            IList<IQueueItem<AccountCloseRequest>> batch = new List<IQueueItem<AccountCloseRequest>>();

            var aadAccountCloseService = this.CreateAadAccountCloseService();

            var response = await aadAccountCloseService.PostBatchAccountCloseAsync(batch).ConfigureAwait(false);
            Assert.IsNotNull(response);
            Assert.AreEqual(0, response.Count);
        }

        #region Test Data

        public static IEnumerable<object[]> AadAccountCloseServiceConstructorTestData()
        {
            var mockPcfAdapter = CreateMockPcfAdapter();
            var mockVerificationTokenValidationService = CreateMockVerificationTokenValidationService();
            var mockAadRequestVerificationServiceAdapter = CreateMockAadRequestVerificationServiceAdapter();
            var mockLogger = CreateMockGenevaLogger();
            var mockAppConfiguration = new Mock<IAppConfiguration>(MockBehavior.Strict);

            var data = new List<object[]>
            {
                new object[]
                {
                    null,
                    mockVerificationTokenValidationService.Object,
                    mockAadRequestVerificationServiceAdapter.Object,
                    mockLogger.Object,
                    mockAppConfiguration.Object
                },
                new object[]
                {
                    mockPcfAdapter.Object,
                    null,
                    mockAadRequestVerificationServiceAdapter.Object,
                    mockLogger.Object,
                    mockAppConfiguration.Object
                },
                new object[]
                {
                    mockPcfAdapter.Object,
                    mockVerificationTokenValidationService.Object,
                    null,
                    mockLogger.Object,
                    mockAppConfiguration.Object
                },
                new object[]
                {
                    mockPcfAdapter.Object,
                    mockVerificationTokenValidationService.Object,
                    mockAadRequestVerificationServiceAdapter.Object,
                    null,
                    mockAppConfiguration.Object
                },
                new object[]
                {
                    mockPcfAdapter.Object,
                    mockVerificationTokenValidationService.Object,
                    mockAadRequestVerificationServiceAdapter.Object,
                    mockLogger.Object,
                    null
                }
            };
            return data;
        }

        private static Mock<IPcfAdapter> CreateMockPcfAdapter()
        {
            return new Mock<IPcfAdapter>(MockBehavior.Strict);
        }

        private static Mock<IVerificationTokenValidationService> CreateMockVerificationTokenValidationService()
        {
            return new Mock<IVerificationTokenValidationService>(MockBehavior.Strict);
        }

        private static Mock<IAadRequestVerificationServiceAdapter> CreateMockAadRequestVerificationServiceAdapter()
        {
            return new Mock<IAadRequestVerificationServiceAdapter>(MockBehavior.Strict);
        }

        private static Mock<ILogger> CreateMockGenevaLogger()
        {
            return new Mock<ILogger>(MockBehavior.Strict);
        }

        #endregion
    }
}
