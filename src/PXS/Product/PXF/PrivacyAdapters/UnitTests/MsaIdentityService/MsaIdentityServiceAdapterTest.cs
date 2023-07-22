// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests.MsaIdentityService
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web.Services.Protocols;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.ProfileIdentityService;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PrivacyOperation.Contracts.PrivacySubject;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using tagPASSID = Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService.tagPASSID;

    /// <summary>
    ///     MsaIdentityServiceAdapterTest
    /// </summary>
    [TestClass]
    public class MsaIdentityServiceAdapterTest : TestBase
    {
        private const string ExpectedToken = "I AM A VERIFIER TOKEN";

        private MsaIdentityServiceAdapter adapter;

        private Mock<ICertificateProvider> certProvider;

        private Mock<ICounterFactory> counterFactory;

        private ILogger logger = new ConsoleLogger();

        private Mock<ICredentialServiceClient> mockCredentialServiceClient;

        private Mock<IProfileServiceClient> mockProfileServiceClient;

        private Mock<IMsaIdentityServiceAdapterConfiguration> msaAdapterConfig;

        private Mock<IMsaIdentityServiceClientFactory> msaIdentityServiceClientFactory;

        private Mock<IPrivacyConfigurationManager> pxsConfig;

        [TestMethod]
        public async Task GetGdprAccountCloseVerifierIgnoreErrorsSuccess()
        {
            //Arrange
            Guid expectedCommandId = Guid.NewGuid();
            const long ExpectedPuid = 1225125616;
            const string ExpectedXuid = "not a real xuid";
            const string ExpectedPreverifierToken = "i came from an AQS queue";
            string expectedOptionalParams = $"<RequestPreVerifier>{ExpectedPreverifierToken}</RequestPreVerifier>";
            this.msaAdapterConfig.Setup(c => c.EnableAdapter).Returns(false);
            this.msaAdapterConfig.Setup(c => c.IgnoreErrors).Returns(true);

            //Act
            AdapterResponse<string> response = await this.adapter.GetGdprAccountCloseVerifierAsync(expectedCommandId, ExpectedPuid, ExpectedPreverifierToken, ExpectedXuid)
                .ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess);
            Assert.IsNull(response.Error);
        }

        [TestMethod]
        public async Task GetGdprAccountCloseVerifierNullHandlingSuccess()
        {
            //Arrange
            Guid expectedCommandId = Guid.NewGuid();
            const long ExpectedPuid = 1225125616;
            const string ExpectedXuid = "not a real xuid";
            const string ExpectedPreverifierToken = "i came from an AQS queue";
            string expectedOptionalParams = $"<RequestPreVerifier>{ExpectedPreverifierToken}</RequestPreVerifier>";
            this.msaAdapterConfig.Setup(c => c.EnableAdapter).Returns(false);

            //Act
            AdapterResponse<string> response = await this.adapter.GetGdprAccountCloseVerifierAsync(expectedCommandId, ExpectedPuid, ExpectedPreverifierToken, ExpectedXuid)
                .ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(AdapterErrorCode.PartnerDisabled, response.Error.Code);
            Assert.AreEqual((int)HttpStatusCode.MethodNotAllowed, response.Error.StatusCode);
        }

        [TestMethod]
        public async Task GetGdprAccountCloseVerifierSuccess()
        {
            // Arrange
            Guid expectedCommandId = Guid.NewGuid();
            const long ExpectedPuid = 1225125616;
            const string ExpectedXuid = "not a real xuid";
            const string ExpectedPreverifierToken = "i came from an AQS queue";
            string expectedOptionalParams = $"<RequestPreVerifier>{ExpectedPreverifierToken}</RequestPreVerifier>";

            // Act
            AdapterResponse<string> response = await this.adapter.GetGdprAccountCloseVerifierAsync(expectedCommandId, ExpectedPuid, ExpectedPreverifierToken, ExpectedXuid)
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Result);
            Assert.AreEqual(ExpectedToken, response.Result);

            this.mockCredentialServiceClient
                .Verify(
                    c => c.GetGdprVerifierAsync(
                        It.Is<tagPASSID>(p => string.Equals(p.bstrID, ExpectedPuid.ToString("X16"))),
                        It.Is<eGDPR_VERIFIER_OPERATION>(p => eGDPR_VERIFIER_OPERATION.AccountClose == p),
                        It.Is<IDictionary<string, string>>(
                            p => string.Equals(ExpectedXuid, p[ClaimIdentifier.Xuid.ToString().ToLowerInvariant()]) &&
                                 string.Equals(expectedCommandId.ToString(), p[ClaimIdentifier.Rid.ToString().ToLowerInvariant()])),
                        It.IsAny<string>(),
                        It.Is<string>(p => string.Equals(expectedOptionalParams, p)),
                        null,
                        false),
                    Times.Once);
        }

        [TestMethod]
        public async Task GetGdprDeviceDeleteVerifierNullHandlingSuccess()
        {
            // Arrange
            Guid expectedCommandId = Guid.NewGuid();
            const long ExpectedDeviceId = 0x18B6BF88160680;
            const string ExpectedPredicateValue = "i am predicate";
            this.msaAdapterConfig.Setup(c => c.EnableAdapter).Returns(false);

            // Act
            AdapterResponse<string> response =
                await this.adapter.GetGdprDeviceDeleteVerifierAsync(expectedCommandId, ExpectedDeviceId, ExpectedPredicateValue).ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(AdapterErrorCode.PartnerDisabled, response.Error.Code);
            Assert.AreEqual((int)HttpStatusCode.MethodNotAllowed, response.Error.StatusCode);
        }

        [TestMethod]
        public async Task GetGdprDeviceDeleteVerifierSuccess()
        {
            // Arrange
            Guid expectedCommandId = Guid.NewGuid();
            const long ExpectedDeviceId = 0x18B6BF88160680;
            const string ExpectedPredicateValue = "i am predicate";

            // Act
            AdapterResponse<string> response =
                await this.adapter.GetGdprDeviceDeleteVerifierAsync(expectedCommandId, ExpectedDeviceId, ExpectedPredicateValue).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Result);
            Assert.AreEqual(ExpectedToken, response.Result);

            this.mockCredentialServiceClient
                .Verify(
                    c => c.GetGdprVerifierAsync(
                        It.Is<tagPASSID>(p => string.Equals(p.bstrID, ExpectedDeviceId.ToString("X16"))),
                        It.Is<eGDPR_VERIFIER_OPERATION>(p => eGDPR_VERIFIER_OPERATION.Delete == p),
                        It.Is<IDictionary<string, string>>(
                            p => string.Equals(ExpectedPredicateValue, p[ClaimIdentifier.Pred.ToString().ToLowerInvariant()]) &&
                                 string.Equals(expectedCommandId.ToString(), p[ClaimIdentifier.Rid.ToString().ToLowerInvariant()])),
                        It.IsAny<string>(),
                        null,
                        null,
                        false),
                    Times.Once);
        }

        [TestMethod]
        public async Task GetGdprDeviceDeleteVerifierWithoutPredSuccess()
        {
            // Arrange
            Guid expectedCommandId = Guid.NewGuid();
            const long ExpectedDeviceId = 0x18B6BF88160680;

            // Act
            AdapterResponse<string> response = await this.adapter.GetGdprDeviceDeleteVerifierAsync(expectedCommandId, ExpectedDeviceId, predicate: null).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Result);
            Assert.AreEqual(ExpectedToken, response.Result);

            this.mockCredentialServiceClient
                .Verify(
                    c => c.GetGdprVerifierAsync(
                        It.Is<tagPASSID>(p => string.Equals(p.bstrID, ExpectedDeviceId.ToString("X16"))),
                        It.Is<eGDPR_VERIFIER_OPERATION>(p => eGDPR_VERIFIER_OPERATION.Delete == p),
                        It.Is<IDictionary<string, string>>(p => string.Equals(expectedCommandId.ToString(), p[ClaimIdentifier.Rid.ToString().ToLowerInvariant()])),
                        It.IsAny<string>(),
                        null,
                        null,
                        false),
                    Times.Once);
        }

        [TestMethod]
        public async Task GetGdprExportVerifierNoXuidSuccess()
        {
            // Arrange
            Guid expectedCommandId = Guid.NewGuid();
            const long TargetPuid = 1351235626262;
            const string ExpectedProxyTicket = "proxy ticket goes here";

            var requestContext = new PxfRequestContext(ExpectedProxyTicket, null, TargetPuid, TargetPuid, null, null, false, null);

            // Act
            AdapterResponse<string> response = await this.adapter.GetGdprExportVerifierAsync(expectedCommandId, requestContext, new Uri("https://www.microsoft.com"), xuid: null)
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Result);
            Assert.AreEqual(ExpectedToken, response.Result);

            this.mockCredentialServiceClient
                .Verify(
                    c => c.GetGdprVerifierAsync(
                        It.Is<tagPASSID>(p => string.Equals(p.bstrID, TargetPuid.ToString("X16"))),
                        It.Is<eGDPR_VERIFIER_OPERATION>(p => eGDPR_VERIFIER_OPERATION.Export == p),
                        It.Is<IDictionary<string, string>>(p => string.Equals(expectedCommandId.ToString(), p[ClaimIdentifier.Rid.ToString().ToLowerInvariant()])),
                        It.IsAny<string>(),
                        null,
                        ExpectedProxyTicket,
                        false),
                    Times.Once);
        }

        [TestMethod]
        public async Task GetGdprExportVerifierNullHandlingSuccess()
        {
            // Arrange
            Guid expectedCommandId = Guid.NewGuid();
            const long TargetPuid = 1351235626262;
            const string ExpectedProxyTicket = "proxy ticket goes here";
            this.msaAdapterConfig.Setup(x => x.EnableAdapter).Returns(false);
            var requestContext = new PxfRequestContext(ExpectedProxyTicket, null, TargetPuid, TargetPuid, null, null, false, null);

            // Act
            AdapterResponse<string> response = await this.adapter.GetGdprExportVerifierAsync(expectedCommandId, requestContext, new Uri("https://www.microsoft.com"), xuid: null)
                .ConfigureAwait(false);

            //Assert
            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(AdapterErrorCode.PartnerDisabled, response.Error.Code);
            Assert.AreEqual((int)HttpStatusCode.MethodNotAllowed, response.Error.StatusCode);
        }

        [DataTestMethod]
        [DataRow(1351235626262, 1351235626262, null, true)] // self
        [DataRow(1234567890234, 1351235626262, "jwt", false)] // family
        [DataRow(1234567890234, 1351235626262, "", true)] // self pcd
        [DataRow(1234567890234, 1351235626262, null, true)]

        // self pcd
        public async Task GetGdprExportVerifierSuccess(long targetPuid, long authorizingPuid, string familyJwt, bool isSelf)
        {
            // Arrange
            Guid expectedCommandId = Guid.NewGuid();
            const string ExpectedXuid = "not a real xuid";
            const string ExpectedProxyTicket = "proxy ticket goes here";

            var requestContext = new PxfRequestContext(ExpectedProxyTicket, familyJwt, authorizingPuid, targetPuid, null, null, false, null);

            // Act
            var storageDestination = new Uri("https://www.microsoft.com");
            AdapterResponse<string> response =
                await this.adapter.GetGdprExportVerifierAsync(expectedCommandId, requestContext, storageDestination, ExpectedXuid).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Result);
            Assert.AreEqual(ExpectedToken, response.Result);

            this.mockCredentialServiceClient
                .Verify(
                    c => c.GetGdprVerifierAsync(
                        It.Is<tagPASSID>(p => string.Equals(p.bstrID, targetPuid.ToString("X16"))),
                        It.Is<eGDPR_VERIFIER_OPERATION>(p => eGDPR_VERIFIER_OPERATION.Export == p),
                        It.Is<IDictionary<string, string>>(
                            p => string.Equals(ExpectedXuid, p[ClaimIdentifier.Xuid.ToString().ToLowerInvariant()]) &&
                                 string.Equals(expectedCommandId.ToString(), p[ClaimIdentifier.Rid.ToString().ToLowerInvariant()]) &&
                                 string.Equals(storageDestination.ToString(), p[ClaimIdentifier.Azsp.ToString().ToLowerInvariant()])),
                        It.IsAny<string>(),
                        isSelf ? null : "<FamilyAuth>true</FamilyAuth>",
                        ExpectedProxyTicket,
                        false),
                    Times.Once);
        }

        [TestMethod]
        public async Task GetGdprUserDeleteVerifierAdapterDisabled()
        {
            // Arrange
            this.msaAdapterConfig.Setup(c => c.EnableAdapter).Returns(false);
            this.CreateAdapter();
            Guid expectedCommandId = Guid.NewGuid();
            const long TargetPuid = 1351235626262;
            const string ExpectedXuid = "not a real xuid";
            const string ExpectedProxyTicket = "proxy ticket goes here";
            const string ExpectedPredicateValue = "i am a serialized predicate";
            const string ExpectedDataTypeValue = null;

            var requestContext = new PxfRequestContext(ExpectedProxyTicket, null, 123, TargetPuid, null, null, false, null);

            // Act
            AdapterResponse<string> response = await this.adapter
                .GetGdprUserDeleteVerifierAsync(new List<Guid> { expectedCommandId }, requestContext, ExpectedXuid, ExpectedPredicateValue, ExpectedDataTypeValue)
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(AdapterErrorCode.PartnerDisabled, response.Error.Code);

            this.mockCredentialServiceClient
                .Verify(
                    c => c.GetGdprVerifierAsync(
                        It.IsAny<tagPASSID>(),
                        It.IsAny<eGDPR_VERIFIER_OPERATION>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<bool>()),
                    Times.Never);
        }

        [TestMethod]
        public async Task GetGdprUserDeleteVerifierAdapterDisabledIgnoreErrors()
        {
            // Arrange
            this.msaAdapterConfig.Setup(c => c.EnableAdapter).Returns(false);
            this.msaAdapterConfig.Setup(c => c.IgnoreErrors).Returns(true);
            this.CreateAdapter();
            Guid expectedCommandId = Guid.NewGuid();
            const long TargetPuid = 1351235626262;
            const string ExpectedXuid = "not a real xuid";
            const string ExpectedProxyTicket = "proxy ticket goes here";
            const string ExpectedPredicateValue = "i am a serialized predicate";
            const string ExpectedDataTypeValue = null;

            var requestContext = new PxfRequestContext(ExpectedProxyTicket, null, 123, TargetPuid, null, null, false, null);

            // Act
            AdapterResponse<string> response = await this.adapter
                .GetGdprUserDeleteVerifierAsync(new List<Guid> { expectedCommandId }, requestContext, ExpectedXuid, ExpectedPredicateValue, ExpectedDataTypeValue)
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess);

            // Adapter disabled, but no error returned. Result should be empty string. And no outbound request ever made.
            Assert.IsNull(response.Result);

            this.mockCredentialServiceClient
                .Verify(
                    c => c.GetGdprVerifierAsync(
                        It.IsAny<tagPASSID>(),
                        It.IsAny<eGDPR_VERIFIER_OPERATION>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<bool>()),
                    Times.Never);
        }

        [TestMethod]
        public async Task GetGdprUserDeleteVerifierAdapterError()
        {
            // Arrange
            this.msaAdapterConfig.Setup(c => c.EnableAdapter).Returns(true);
            this.msaAdapterConfig.Setup(c => c.IgnoreErrors).Returns(false);
            this.mockCredentialServiceClient.Setup(
                    c => c.GetGdprVerifierAsync(
                        It.IsAny<tagPASSID>(),
                        It.IsAny<eGDPR_VERIFIER_OPERATION>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<bool>()))
                .Throws<SoapException>();

            this.CreateAdapter();
            Guid expectedCommandId = Guid.NewGuid();
            const long TargetPuid = 1351235626262;
            const string ExpectedXuid = "not a real xuid";
            const string ExpectedProxyTicket = "proxy ticket goes here";
            const string ExpectedPredicateValue = "i am a serialized predicate";
            const string ExpectedDataTypeValue = null;

            var requestContext = new PxfRequestContext(ExpectedProxyTicket, null, 123, TargetPuid, null, null, false, null);

            // Act
            AdapterResponse<string> response = await this.adapter
                .GetGdprUserDeleteVerifierAsync(new List<Guid> { expectedCommandId }, requestContext, ExpectedXuid, ExpectedPredicateValue, ExpectedDataTypeValue)
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess);
            Assert.IsNull(response.Result);

            this.mockCredentialServiceClient
                .Verify(
                    c => c.GetGdprVerifierAsync(
                        It.IsAny<tagPASSID>(),
                        It.IsAny<eGDPR_VERIFIER_OPERATION>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<bool>()),
                    Times.Once);
        }

        [TestMethod]
        public async Task GetGdprUserDeleteVerifierAdapterErrorIgnoreError()
        {
            // Arrange
            this.msaAdapterConfig.Setup(c => c.EnableAdapter).Returns(true);
            this.msaAdapterConfig.Setup(c => c.IgnoreErrors).Returns(true);
            this.mockCredentialServiceClient.Setup(
                    c => c.GetGdprVerifierAsync(
                        It.IsAny<tagPASSID>(),
                        It.IsAny<eGDPR_VERIFIER_OPERATION>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<bool>()))
                .Throws<SoapException>();

            this.CreateAdapter();
            Guid expectedCommandId = Guid.NewGuid();
            const long TargetPuid = 1351235626262;
            const string ExpectedXuid = "not a real xuid";
            const string ExpectedProxyTicket = "proxy ticket goes here";
            const string ExpectedPredicateValue = "i am a serialized predicate";
            const string ExpectedDataTypeValue = null;

            var requestContext = new PxfRequestContext(ExpectedProxyTicket, null, 123, TargetPuid, null, null, false, null);

            // Act
            AdapterResponse<string> response = await this.adapter
                .GetGdprUserDeleteVerifierAsync(new List<Guid> { expectedCommandId }, requestContext, ExpectedXuid, ExpectedPredicateValue, ExpectedDataTypeValue)
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess);
            Assert.IsNull(response.Result);

            this.mockCredentialServiceClient
                .Verify(
                    c => c.GetGdprVerifierAsync(
                        It.IsAny<tagPASSID>(),
                        It.IsAny<eGDPR_VERIFIER_OPERATION>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<bool>()),
                    Times.Once);
        }

        [TestMethod]
        public async Task GetGdprUserDeleteVerifierShouldErrorExceedMaxClaimLength()
        {
            // Arrange
            this.msaAdapterConfig.Setup(c => c.EnableAdapter).Returns(true);
            this.msaAdapterConfig.Setup(c => c.IgnoreErrors).Returns(false);
            this.CreateAdapter();
            const long TargetPuid = 1351235626262;
            const string ExpectedXuid = "not a real xuid";
            const string ExpectedProxyTicket = "proxy ticket goes here";
            const string ExpectedPredicateValue = "i am a serialized predicate";
            const string ExpectedDataTypeValue = null;

            IList<Guid> commandIdList = new List<Guid>();

            do
            {
                commandIdList.Add(Guid.NewGuid());
            } while (string.Join(",", commandIdList).Length < MsaIdentityServiceAdapter.MaxValuePerClaim + 1);

            var requestContext = new PxfRequestContext(ExpectedProxyTicket, null, 123, TargetPuid, null, null, false, null);

            // Act
            try
            {
                await this.adapter
                    .GetGdprUserDeleteVerifierAsync(commandIdList, requestContext, ExpectedXuid, ExpectedPredicateValue, ExpectedDataTypeValue)
                    .ConfigureAwait(false);
                Assert.Fail($"Should have thrown a {nameof(NotSupportedException)} because the command id claim exceeds the limit of MSA.");
            }
            catch (NotSupportedException e)
            {
                Assert.AreEqual("Claim for Rids must not exceed the claim limit of 2048", e.Message);
            }

            this.mockCredentialServiceClient
                .Verify(
                    c => c.GetGdprVerifierAsync(
                        It.IsAny<tagPASSID>(),
                        It.IsAny<eGDPR_VERIFIER_OPERATION>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<bool>()),
                    Times.Never);
        }

        [DataTestMethod]
        [DataRow(1351235626262, 1351235626262, null, true)] // self
        [DataRow(1234567890234, 1351235626262, "jwt", false)] // family
        [DataRow(1234567890234, 1351235626262, "", true)] // pcd
        [DataRow(1234567890234, 1351235626262, null, true)] //pcd and Bing
        public async Task GetGdprUserDeleteVerifierSuccess(long targetPuid, long authorizingPuid, string familyJwt, bool isSelf)
        {
            // Arrange
            Guid expectedCommandId = Guid.NewGuid();
            const string ExpectedXuid = "not a real xuid";
            const string ExpectedProxyTicket = "proxy ticket goes here";
            const string ExpectedPredicateValue = "i am a serialized predicate";
            const string ExpectedDataTypeValue = null;

            var requestContext = new PxfRequestContext(ExpectedProxyTicket, familyJwt, authorizingPuid, targetPuid, null, null, false, null);

            // Act
            AdapterResponse<string> response = await this.adapter
                .GetGdprUserDeleteVerifierAsync(new List<Guid> { expectedCommandId }, requestContext, ExpectedXuid, ExpectedPredicateValue, ExpectedDataTypeValue)
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Result);
            Assert.AreEqual(ExpectedToken, response.Result);

            this.mockCredentialServiceClient
                .Verify(
                    c => c.GetGdprVerifierAsync(
                        It.Is<tagPASSID>(p => string.Equals(p.bstrID, targetPuid.ToString("X16"))),
                        It.Is<eGDPR_VERIFIER_OPERATION>(p => eGDPR_VERIFIER_OPERATION.Delete == p),
                        It.Is<IDictionary<string, string>>(
                            p => string.Equals(ExpectedXuid, p[ClaimIdentifier.Xuid.ToString().ToLowerInvariant()]) &&
                                 string.Equals(expectedCommandId.ToString(), p[ClaimIdentifier.Rid.ToString().ToLowerInvariant()]) &&
                                 string.Equals(ExpectedPredicateValue, p[ClaimIdentifier.Pred.ToString().ToLowerInvariant()])),
                        It.IsAny<string>(),
                        isSelf ? null : "<FamilyAuth>true</FamilyAuth>",
                        ExpectedProxyTicket,
                        false),
                    Times.Once);
        }

        [DataTestMethod]
        public async Task GetGdprUserScopedDeleteVerifierSuccess()
        {
            // Arrange
            Guid expectedCommandId = Guid.NewGuid();
            const string ExpectedXuid = "not a real xuid";
            const string ExpectedProxyTicket = "proxy ticket goes here";
            const string ExpectedPredicateValue = "i am a serialized predicate";
            const string ExpectedDataTypeValue = "i am a serialized dataType"; // Results in a scoped delete
            const long ExpectedPuid = 1351235626262;

            var requestContext = new PxfRequestContext(ExpectedProxyTicket, null, ExpectedPuid, ExpectedPuid, null, null, false, null);

            // Act
            AdapterResponse<string> response = await this.adapter
                .GetGdprUserDeleteVerifierAsync(new List<Guid> { expectedCommandId }, requestContext, ExpectedXuid, ExpectedPredicateValue, ExpectedDataTypeValue)
                .ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Result);
            Assert.AreEqual(ExpectedToken, response.Result);

            this.mockCredentialServiceClient
                .Verify(
                    c => c.GetGdprVerifierAsync(
                        It.Is<tagPASSID>(p => string.Equals(p.bstrID, ExpectedPuid.ToString("X16"))),
                        It.Is<eGDPR_VERIFIER_OPERATION>(p => eGDPR_VERIFIER_OPERATION.ScopedDelete == p),
                        It.Is<IDictionary<string, string>>(
                            p => string.Equals(ExpectedXuid, p[ClaimIdentifier.Xuid.ToString().ToLowerInvariant()]) &&
                                 string.Equals(expectedCommandId.ToString(), p[ClaimIdentifier.Rid.ToString().ToLowerInvariant()]) &&
                                 string.Equals(ExpectedPredicateValue, p[ClaimIdentifier.Pred.ToString().ToLowerInvariant()])),
                        It.IsAny<string>(),
                        null,
                        ExpectedProxyTicket,
                        false),
                    Times.Once);
        }

        [TestMethod]
        public async Task GetProfileAttributesDisabledAdapterSuccess()
        {
            this.msaAdapterConfig.Setup(c => c.EnableAdapter).Returns(false);
            this.msaAdapterConfig.Setup(c => c.IgnoreErrors).Returns(false);
            AdapterResponse<IProfileAttributesUserData> response = await this.adapter.GetProfileAttributesAsync(null).ConfigureAwait(false);
            Assert.IsFalse(response.IsSuccess);
            Assert.IsNull(response.Result);
        }

        [TestMethod]
        public async Task GetProfileAttributesDisabledIgnoreErrorsAdapterSuccess()
        {
            this.msaAdapterConfig.Setup(c => c.EnableAdapter).Returns(false);
            this.msaAdapterConfig.Setup(c => c.IgnoreErrors).Returns(true);
            AdapterResponse<IProfileAttributesUserData> response = await this.adapter.GetProfileAttributesAsync(null).ConfigureAwait(false);
            Assert.IsTrue(response.IsSuccess);
            Assert.IsNull(response.Result);
        }

        [TestMethod]
        public async Task GetUserProfileAttributesNoAddressNoBirthdaySuccess()
        {
            const long TargetPuid = 0xfeedf00d;

            this.mockProfileServiceClient.Setup(
                    c => c.GetProfileByAttributesAsync(It.Is<string>(s => string.Equals(s, TargetPuid.ToString("X16"))), It.IsAny<string>()))
                .ReturnsAsync(
                    $@"<p:userData xmlns:p=""http://schemas.microsoft.com/Passport/User"">
                     <p:dataOwner>{TargetPuid:X16}</p:dataOwner>
                     <p:propertyCollection name=""Personal_CS"">
                       <p:property name=""gender"" datatype=""bstr"">M</p:property>
                       <p:property name=""langpreference"" datatype=""bstr"">1033</p:property>
                       <p:property name=""occupation"" datatype=""bstr"">W</p:property>
                     </p:propertyCollection>
                     <p:propertyCollection name=""Personal2_CS"">
                       <p:property name=""name.first"" datatype=""bstr"">TestFirst</p:property>
                       <p:property name=""name.last"" datatype=""bstr"">TestLast</p:property>
                     </p:propertyCollection>
                     <p:propertyCollection name=""BrandDetails"">
                       <p:property name=""productclassesversion"" datatype=""i4"">4</p:property>
                       <p:property name=""masterbrand"" datatype=""bstr"">VZ01</p:property>
                       <p:property name=""brandinfo"" datatype=""bstr"">WLID:VZ01;UNIP:CHTR</p:property>
                     </p:propertyCollection>
                    </p:userData>");

            AdapterResponse<IProfileAttributesUserData> response = await this.adapter.GetProfileAttributesAsync(
                new PxfRequestContext(null, null, TargetPuid, TargetPuid, null, null, false, null),
                ProfileAttribute.All).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess, response.Error?.Message);
            Assert.IsNull(response.Result.Birthdate);
            Assert.IsNotNull(response.Result.FirstName);
            Assert.IsNotNull(response.Result.LastName);
            Assert.AreNotEqual(response.Result.FirstName, response.Result.LastName);
        }

        [TestMethod]
        public async Task WhenGetUserProfileAttributesReturnsUserNotFoundThrowsBadRequest()
        {
            const long TargetPuid = 0xfeedf00d;

            this.mockProfileServiceClient.Setup(
                    c => c.GetProfileByAttributesAsync(It.Is<string>(s => string.Equals(s, TargetPuid.ToString("X16"))), It.IsAny<string>()))
                .ThrowsAsync(new PrivacySubjectInvalidException("User not found"));

            AdapterResponse<IProfileAttributesUserData> response = await this.adapter.GetProfileAttributesAsync(
                new PxfRequestContext(null, null, TargetPuid, TargetPuid, null, null, false, null),
                ProfileAttribute.All).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(response.Error.Code, AdapterErrorCode.BadRequest);
        }

        [TestMethod]
        public async Task WhenGetUserProfileAttributesReturnsOtherErrorsThrowsUnknownError()
        {
            const long TargetPuid = 0xfeedf00d;

            this.mockProfileServiceClient.Setup(
                    c => c.GetProfileByAttributesAsync(It.Is<string>(s => string.Equals(s, TargetPuid.ToString("X16"))), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Unknown"));

            AdapterResponse<IProfileAttributesUserData> response = await this.adapter.GetProfileAttributesAsync(
                new PxfRequestContext(null, null, TargetPuid, TargetPuid, null, null, false, null),
                ProfileAttribute.All).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(response.Error.Code, AdapterErrorCode.Unknown);
        }

        [TestMethod]
        public async Task GetUserProfileAttributesSuccess()
        {
            const long TargetPuid = 0xcafef00d;

            this.mockProfileServiceClient.Setup(
                    c => c.GetProfileByAttributesAsync(It.Is<string>(s => string.Equals(s, TargetPuid.ToString("X16"))), It.IsAny<string>()))
                .ReturnsAsync(
                    $@"<p:userData xmlns:p=""http://schemas.microsoft.com/Passport/User"">
                     <p:dataOwner>{TargetPuid:X16}</p:dataOwner>
                     <p:propertyCollection name=""Personal_CS"">
                       <p:property name=""gender"" datatype=""bstr"">M</p:property>
                       <p:property name=""birthdate"" datatype=""bstr"">22:3:1970</p:property>
                       <p:property name=""langpreference"" datatype=""bstr"">1033</p:property>
                       <p:property name=""occupation"" datatype=""bstr"">W</p:property>
                     </p:propertyCollection>
                     <p:propertyCollection name=""Personal2_CS"">
                       <p:property name=""name.first"" datatype=""bstr"">TestFirst</p:property>
                       <p:property name=""name.last"" datatype=""bstr"">TestLast</p:property>
                     </p:propertyCollection>
                     <p:propertyCollection name=""Addresses_CS"">
                       <p:property name=""home.city"" datatype=""bstr"">Redmond</p:property>
                       <p:property name=""home.street1"" datatype=""bstr"">2000 Main St.</p:property>
                       <p:property name=""home.country"" datatype=""bstr"">US</p:property>
                       <p:property name=""work.country"" datatype=""bstr"">US</p:property>
                       <p:property name=""home.region"" datatype=""i4"">35841</p:property>
                       <p:property name=""work.postalcode"" datatype=""bstr"">98052</p:property>
                       <p:property name=""home.postalcode"" datatype=""bstr"">98052</p:property>
                       <p:property name=""work.region"" datatype=""i4"">35841</p:property>
                       <p:property name=""home.timezone"" datatype=""i4"">1119</p:property>
                       <p:property name=""work.timezone"" datatype=""i4"">1119</p:property>
                       <p:property name=""work.city"" datatype=""bstr"">Redmond</p:property>
                       <p:property name=""work.street1"" datatype=""bstr"">One Microsoft Way</p:property>
                     </p:propertyCollection>
                     <p:propertyCollection name=""BrandDetails"">
                       <p:property name=""productclassesversion"" datatype=""i4"">4</p:property>
                       <p:property name=""masterbrand"" datatype=""bstr"">VZ01</p:property>
                       <p:property name=""brandinfo"" datatype=""bstr"">WLID:VZ01;UNIP:CHTR</p:property>
                     </p:propertyCollection>
                    </p:userData>");

            AdapterResponse<IProfileAttributesUserData> response = await this.adapter.GetProfileAttributesAsync(
                new PxfRequestContext(null, null, TargetPuid, TargetPuid, null, null, false, null),
                ProfileAttribute.All).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess, response.Error?.Message);
            Assert.IsNotNull(response.Result.Birthdate);
            Assert.IsNotNull(response.Result.FirstName);
            Assert.IsNotNull(response.Result.LastName);
            Assert.AreNotEqual(response.Result.FirstName, response.Result.LastName);
        }

        [TestMethod]
        public async Task GetUserSigninInfoDisabledAdapterSuccess()
        {
            this.msaAdapterConfig.Setup(c => c.EnableAdapter).Returns(false);
            this.msaAdapterConfig.Setup(c => c.IgnoreErrors).Returns(false);
            AdapterResponse<ISigninNameInformation> response = await this.adapter.GetSigninNameInformationAsync(0).ConfigureAwait(false);
            Assert.IsFalse(response.IsSuccess);
            Assert.IsNull(response.Result);
        }

        [TestMethod]
        public async Task GetUserSigninInfoDisabledIgnoreErrorsAdapterSuccess()
        {
            this.msaAdapterConfig.Setup(c => c.EnableAdapter).Returns(false);
            this.msaAdapterConfig.Setup(c => c.IgnoreErrors).Returns(true);
            AdapterResponse<ISigninNameInformation> response = await this.adapter.GetSigninNameInformationAsync(0).ConfigureAwait(false);
            Assert.IsTrue(response.IsSuccess);
            Assert.IsNull(response.Result);
        }

        [TestMethod]
        public async Task GetUserSigninInfoSuccess()
        {
            // Arrange
            const long TargetPuid = 0x0003000080091C04;

            // Act
            AdapterResponse<ISigninNameInformation> response = await this.adapter.GetSigninNameInformationAsync(TargetPuid).ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual(TargetPuid, response.Result.Puid);
            Assert.AreEqual(0x5d0f2e073583de53, response.Result.Cid);

            this.mockCredentialServiceClient
                .Verify(
                    c => c.GetSigninNamesAndCidsForNetIdAsync(
                        It.Is<string>(p => string.Equals(p, TargetPuid.ToString("X16"))),
                        It.IsAny<string>()),
                    Times.Once);
        }

        [TestMethod]
        public async Task GetUserSigninInfoUserNoLongerExists()
        {
            const long TargetPuid = 0;

            AdapterResponse<ISigninNameInformation> response = await this.adapter.GetSigninNameInformationAsync(TargetPuid).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess);
            Assert.IsNull(response.Result);

            this.mockCredentialServiceClient
                .Verify(
                    c => c.GetSigninNamesAndCidsForNetIdAsync(
                        It.Is<string>(p => string.Equals(p, TargetPuid.ToString("X16"))),
                        It.IsAny<string>()),
                    Times.Once);
        }

        [TestInitialize]
        public void Initialize()
        {
            this.msaAdapterConfig = new Mock<IMsaIdentityServiceAdapterConfiguration>();
            this.msaAdapterConfig.Setup(c => c.BaseUrl).Returns("https://www.msa.com");
            this.msaAdapterConfig.Setup(c => c.PartnerId).Returns("MSA");
            this.msaAdapterConfig.Setup(c => c.CounterCategoryName).Returns("IAMPERFCOUNTER");
            this.msaAdapterConfig.Setup(c => c.EnableAdapter).Returns(true);
            this.msaAdapterConfig.Setup(c => c.IgnoreErrors).Returns(false);
            var mockServicePointConfiguration = new Mock<IServicePointConfiguration>(MockBehavior.Strict);
            mockServicePointConfiguration.Setup(c => c.ConnectionLimit).Returns(42);
            mockServicePointConfiguration.Setup(c => c.ConnectionLeaseTimeout).Returns(39);
            mockServicePointConfiguration.Setup(c => c.MaxIdleTime).Returns(98);
            this.msaAdapterConfig.Setup(c => c.ServicePointConfiguration).Returns(mockServicePointConfiguration.Object);
            var mockAdaptersConfiguration = new Mock<IAdaptersConfiguration>(MockBehavior.Strict);
            mockAdaptersConfiguration.Setup(c => c.MsaIdentityServiceAdapterConfiguration).Returns(this.msaAdapterConfig.Object);

            this.pxsConfig = new Mock<IPrivacyConfigurationManager>(MockBehavior.Strict);
            this.pxsConfig.Setup(c => c.MsaIdentityServiceConfiguration).Returns(CreateMsaIdentityConfigMock().Object);
            this.pxsConfig.Setup(c => c.AdaptersConfiguration).Returns(mockAdaptersConfiguration.Object);

            this.certProvider = CreateCertProviderMock();
            this.logger = new ConsoleLogger();
            this.counterFactory = new Mock<ICounterFactory>(MockBehavior.Strict);
            this.counterFactory.Setup(c => c.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>())).Returns(new Mock<ICounter>(MockBehavior.Loose).Object);

            this.mockCredentialServiceClient = new Mock<ICredentialServiceClient>(MockBehavior.Strict);
            this.mockCredentialServiceClient.Setup(
                    c => c.GetGdprVerifierAsync(
                        It.IsAny<tagPASSID>(),
                        It.IsAny<eGDPR_VERIFIER_OPERATION>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<bool>()))
                .ReturnsAsync(ExpectedToken);
            this.mockCredentialServiceClient.Setup(c => c.TargetUri).Returns(new Uri("https://www.msa.com/getverifier"));
            this.mockCredentialServiceClient.Setup(c => c.GetSigninNamesAndCidsForNetIdAsync(It.Is<string>(s => string.Equals(s, "0003000080091C04")),
                It.IsAny<string>())).ReturnsAsync(
                @"<NETID2Name>
                        <SigninName NetID=""0003000080091C04"" CID=""5d0f2e073583de53"" CredFlags=""00000820"">someone@microsoft.com</SigninName>
                    </NETID2Name>");
            this.mockCredentialServiceClient.Setup(c => c.GetSigninNamesAndCidsForNetIdAsync(It.Is<string>(s => string.Equals(s, "0000000000000000")),
                It.IsAny<string>())).ReturnsAsync(
                @"<NETID2Name>
                  </NETID2Name>");

            this.mockProfileServiceClient = new Mock<IProfileServiceClient>(MockBehavior.Strict);

            this.msaIdentityServiceClientFactory = new Mock<IMsaIdentityServiceClientFactory>(MockBehavior.Strict);
            this.msaIdentityServiceClientFactory
                .Setup(
                    c => c.CreateCredentialServiceClient(
                        It.IsAny<IPrivacyPartnerAdapterConfiguration>(),
                        It.IsAny<IMsaIdentityServiceConfiguration>(),
                        It.IsAny<ICertificateProvider>()))
                .Returns(this.mockCredentialServiceClient.Object);
            this.msaIdentityServiceClientFactory.Setup(
                    c => c.CreateProfileServiceClient(
                        It.IsAny<IPrivacyPartnerAdapterConfiguration>(),
                        It.IsAny<IMsaIdentityServiceConfiguration>(),
                        It.IsAny<ICertificateProvider>()))
                .Returns(this.mockProfileServiceClient.Object);

            this.CreateAdapter();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MsaIdentityServiceAdapterCounterFactoryNullFail()
        {
            try
            {
                this.adapter = new MsaIdentityServiceAdapter(
                    this.pxsConfig.Object,
                    this.certProvider.Object,
                    this.logger,
                    null,
                    this.msaIdentityServiceClientFactory.Object);
                Assert.Fail("Should have raised exception");
            }
            catch (ArgumentNullException e)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: counterFactory", e.Message);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MsaIdentityServiceAdapterLoggerNullFail()
        {
            try
            {
                this.adapter = new MsaIdentityServiceAdapter(
                    this.pxsConfig.Object,
                    this.certProvider.Object,
                    null,
                    this.counterFactory.Object,
                    this.msaIdentityServiceClientFactory.Object);
                Assert.Fail("Should have raised exception");
            }
            catch (ArgumentNullException e)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: logger", e.Message);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MsaIdentityServiceAdapterPxsConfigNullFail()
        {
            try
            {
                this.adapter = new MsaIdentityServiceAdapter(
                    null,
                    this.certProvider.Object,
                    this.logger,
                    this.counterFactory.Object,
                    this.msaIdentityServiceClientFactory.Object);
                Assert.Fail("Should have raised exception");
            }
            catch (ArgumentNullException e)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: pxsConfig", e.Message);
                throw;
            }
        }

        [TestMethod]
        public void MsaIdentityServiceAdapterSuccess()
        {
            this.CreateAdapter();
        }

        private void CreateAdapter()
        {
            this.adapter = new MsaIdentityServiceAdapter(
                this.pxsConfig.Object,
                this.certProvider.Object,
                this.logger,
                this.counterFactory.Object,
                this.msaIdentityServiceClientFactory.Object);
        }
    }
}
