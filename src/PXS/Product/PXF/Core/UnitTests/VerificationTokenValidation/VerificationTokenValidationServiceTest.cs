// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.VerificationTokenValidation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.VerificationTokenValidation;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class VerificationTokenValidationServiceTest
    {
        [TestMethod]
        [DataRow(RequestType.AccountClose)]
        [DataRow(RequestType.AgeOut)]
        [DataRow(RequestType.Delete)]
        [DataRow(RequestType.Export)]
        public async Task ShouldHandleExceptionsAsUnknownAdapterError(RequestType requestType)
        {
            // Arrange
            Mock<IValidationServiceFactory> mockValidationServiceFactory = new Mock<IValidationServiceFactory>(MockBehavior.Strict);
            Mock<IValidationService> mockValidationService = new Mock<IValidationService>(MockBehavior.Strict);
            mockValidationService
                .Setup(c => c.EnsureValidAsync(It.IsAny<string>(), It.IsAny<CommandClaims>(), It.IsAny<CancellationToken>()))
                .Throws(new System.Exception("something bad happened."));
            mockValidationServiceFactory.Setup(c => c.Create(It.IsAny<PcvEnvironment>())).Returns(mockValidationService.Object);
            Mock<IPrivacyConfigurationManager> mockConfig = new Mock<IPrivacyConfigurationManager>();
            mockConfig
                .SetupGet(c => c.AdaptersConfiguration.VerificationValidationServiceConfiguration.EnableVerificationCheckAad)
                .Returns(true);
            mockConfig.SetupGet(c => c.PrivacyExperienceServiceConfiguration.CloudInstance).Returns(CloudInstanceType.PublicProd);
            var verificationValidationService = new VerificationTokenValidationService(mockConfig.Object, new ConsoleLogger(), mockValidationServiceFactory.Object);
            var privacyRequest = new PrivacyRequest { IsWatchdogRequest = false, Subject = new AadSubject(), RequestType = requestType };

            // Act
            var validationResponse = await verificationValidationService.ValidateVerifierAsync(privacyRequest, verificationToken: "something").ConfigureAwait(false);

            // Assert
            Assert.IsFalse(validationResponse.IsSuccess);
            Assert.AreEqual(validationResponse.Error.Code, AdapterErrorCode.Unknown);
        }

        [TestMethod, Description("Anything that is not watchdog traffic must have a verifier token.")]
        [DataRow(RequestType.AccountClose)]
        [DataRow(RequestType.AgeOut)]
        [DataRow(RequestType.Delete)]
        [DataRow(RequestType.Export)]
        public async Task ValidateVerifierRejectsNullVerifier(RequestType requestType)
        {
            IList<IPrivacySubject> subjects = new List<IPrivacySubject> { new MsaSubject(), new DeviceSubject() };
            foreach (var subject in subjects)
            {
                // Arrange
                Mock<IValidationServiceFactory> mockValidationServiceFactory = new Mock<IValidationServiceFactory>(MockBehavior.Strict);
                Mock<IValidationService> mockValidationService = new Mock<IValidationService>(MockBehavior.Strict);
                mockValidationServiceFactory.Setup(c => c.Create(It.IsAny<PcvEnvironment>())).Returns(mockValidationService.Object);
                Mock<IPrivacyConfigurationManager> mockConfig = new Mock<IPrivacyConfigurationManager>();
                mockConfig
                    .SetupGet(c => c.AdaptersConfiguration.VerificationValidationServiceConfiguration.TargetMsaKeyDiscoveryEnvironment)
                    .Returns(TargetMsaKeyDiscoveryEnvironment.MsaProd);
                mockConfig
                    .SetupGet(c => c.AdaptersConfiguration.VerificationValidationServiceConfiguration.EnableVerificationCheckMsa)
                    .Returns(true);
                mockConfig.SetupGet(c => c.PrivacyExperienceServiceConfiguration.CloudInstance).Returns(CloudInstanceType.PublicProd);
                var verificationValidationService = new VerificationTokenValidationService(mockConfig.Object, new ConsoleLogger(), mockValidationServiceFactory.Object);
                var privacyRequest = new PrivacyRequest { IsWatchdogRequest = false, Subject = subject, RequestType = requestType };

                // null and empty verifiers get rejected
                var testVerifiers = new[] { string.Empty, null };

                foreach (var testVerifier in testVerifiers)
                {
                    // Act
                    var validationResponse = await verificationValidationService.ValidateVerifierAsync(privacyRequest, verificationToken: testVerifier).ConfigureAwait(false);

                    // Assert
                    Assert.IsFalse(validationResponse.IsSuccess);
                    Assert.AreEqual(AdapterErrorCode.NullVerifier, validationResponse.Error.Code);
                }
            }
        }

        [TestMethod, Description("Watchdog traffic is considered successful without a verifier token.")]
        public async Task ValidateVerifierWatchdogExcluded()
        {
            IList<IPrivacySubject> subjects = new List<IPrivacySubject> { new MsaSubject(), new DeviceSubject() };
            foreach (var subject in subjects)
            {
                // Arrange
                Mock<IValidationServiceFactory> mockValidationServiceFactory = new Mock<IValidationServiceFactory>(MockBehavior.Strict);
                Mock<IValidationService> mockValidationService = new Mock<IValidationService>(MockBehavior.Strict);
                mockValidationServiceFactory.Setup(c => c.Create(It.IsAny<PcvEnvironment>())).Returns(mockValidationService.Object);
                Mock<IPrivacyConfigurationManager> mockConfig = new Mock<IPrivacyConfigurationManager>();
                mockConfig
                    .SetupGet(c => c.AdaptersConfiguration.VerificationValidationServiceConfiguration.TargetMsaKeyDiscoveryEnvironment)
                    .Returns(TargetMsaKeyDiscoveryEnvironment.MsaProd);
                mockConfig
                    .SetupGet(c => c.AdaptersConfiguration.VerificationValidationServiceConfiguration.EnableVerificationCheckMsa)
                    .Returns(true);
                mockConfig.SetupGet(c => c.PrivacyExperienceServiceConfiguration.CloudInstance).Returns(CloudInstanceType.PublicProd);
                var verificationValidationService = new VerificationTokenValidationService(mockConfig.Object, new ConsoleLogger(), mockValidationServiceFactory.Object);
                var privacyRequest = new PrivacyRequest { IsWatchdogRequest = true, Subject = subject };

                // Act
                var validationResponse = await verificationValidationService.ValidateVerifierAsync(privacyRequest, verificationToken: null).ConfigureAwait(false);

                // Assert
                Assert.IsTrue(validationResponse.IsSuccess);
            }
        }

        [TestMethod]
        public async Task ShouldNotValidateAadVerifiersWhenConfigDisabled()
        {
            // Arrange
            Mock<IValidationServiceFactory> mockValidationServiceFactory = new Mock<IValidationServiceFactory>(MockBehavior.Strict);
            Mock<IValidationService> mockValidationService = new Mock<IValidationService>(MockBehavior.Strict);
            mockValidationServiceFactory.Setup(c => c.Create(It.IsAny<PcvEnvironment>())).Returns(mockValidationService.Object);
            Mock<IPrivacyConfigurationManager> mockConfig = new Mock<IPrivacyConfigurationManager>();
            mockConfig
                .SetupGet(c => c.AdaptersConfiguration.VerificationValidationServiceConfiguration.TargetMsaKeyDiscoveryEnvironment)
                .Returns(TargetMsaKeyDiscoveryEnvironment.MsaProd);
            mockConfig.SetupGet(c => c.AdaptersConfiguration.VerificationValidationServiceConfiguration.EnableVerificationCheckAad).Returns(false);
            mockConfig.SetupGet(c => c.AdaptersConfiguration.VerificationValidationServiceConfiguration.EnableVerificationCheckMsa).Returns(true);
            mockConfig.SetupGet(c => c.PrivacyExperienceServiceConfiguration.CloudInstance).Returns(CloudInstanceType.PublicProd);
            var verificationValidationService = new VerificationTokenValidationService(mockConfig.Object, new ConsoleLogger(), mockValidationServiceFactory.Object);
            var privacyRequest = new PrivacyRequest { Subject = new AadSubject() };

            // Act
            var validationResponse = await verificationValidationService.ValidateVerifierAsync(privacyRequest, verificationToken: null).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(validationResponse.IsSuccess);

            // Test for AadSubject2

            privacyRequest = new PrivacyRequest { Subject = new AadSubject2() };

            // Act
            validationResponse = await verificationValidationService.ValidateVerifierAsync(privacyRequest, verificationToken: null).ConfigureAwait(false);

            // Assert
            Assert.IsTrue(validationResponse.IsSuccess);
        }

        [DataRow(RequestType.AccountClose)]
        [DataRow(RequestType.AgeOut)]
        [DataRow(RequestType.Delete)]
        [DataRow(RequestType.Export)]
        [TestMethod]
        public async Task ShouldValidateAadVerifiersWhenConfigEnabled(RequestType requestType)
        {
            // Arrange
            Mock<IValidationServiceFactory> mockValidationServiceFactory = new Mock<IValidationServiceFactory>(MockBehavior.Strict);
            Mock<IValidationService> mockValidationService = new Mock<IValidationService>(MockBehavior.Strict);
            mockValidationService
                .Setup(c => c.EnsureValidAsync(It.IsAny<string>(), It.IsAny<CommandClaims>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockValidationServiceFactory.Setup(c => c.Create(It.IsAny<PcvEnvironment>())).Returns(mockValidationService.Object);
            Mock<IPrivacyConfigurationManager> mockConfig = new Mock<IPrivacyConfigurationManager>();
            mockConfig
                .SetupGet(c => c.AdaptersConfiguration.VerificationValidationServiceConfiguration.TargetMsaKeyDiscoveryEnvironment)
                .Returns(TargetMsaKeyDiscoveryEnvironment.MsaProd);
            mockConfig.SetupGet(c => c.AdaptersConfiguration.VerificationValidationServiceConfiguration.EnableVerificationCheckAad).Returns(true);
            mockConfig.SetupGet(c => c.AdaptersConfiguration.VerificationValidationServiceConfiguration.EnableVerificationCheckMsa).Returns(true);
            mockConfig.SetupGet(c => c.PrivacyExperienceServiceConfiguration.CloudInstance).Returns(CloudInstanceType.PublicProd);
            var verificationValidationService = new VerificationTokenValidationService(mockConfig.Object, new ConsoleLogger(), mockValidationServiceFactory.Object);

            var privacyRequest = new PrivacyRequest { Subject = new AadSubject(), RequestType = requestType };

            // Act
            var validationResponse = await verificationValidationService.ValidateVerifierAsync(
                privacyRequest,
                verificationToken: "some value that is not empty or null").ConfigureAwait(false);

            // Assert
            Assert.IsTrue(validationResponse.IsSuccess);
            mockValidationService
                .Verify(c => c.EnsureValidAsync(It.IsAny<string>(), It.IsAny<CommandClaims>(), It.IsAny<CancellationToken>()), Times.Once);

            // Test for AadSubject2

            // Arrange
            privacyRequest = new PrivacyRequest { Subject = new AadSubject2(), RequestType = requestType };

            // Act
            validationResponse = await verificationValidationService.ValidateVerifierAsync(
                privacyRequest,
                verificationToken: "some value that is not empty or null").ConfigureAwait(false);

            // Assert
            Assert.IsTrue(validationResponse.IsSuccess);
            mockValidationService
                .Verify(c => c.EnsureValidAsync(It.IsAny<string>(), It.IsAny<CommandClaims>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [DataRow(RequestType.AccountClose)]
        [TestMethod]
        public async Task ShouldValidateAadVerifiersForAccountCleanup(RequestType requestType)
        {
            var cachedClaim = new List<CommandClaims>();

            // Arrange
            Mock<IValidationServiceFactory> mockValidationServiceFactory = new Mock<IValidationServiceFactory>(MockBehavior.Strict);
            Mock<IValidationService> mockValidationService = new Mock<IValidationService>(MockBehavior.Strict);
            mockValidationService
                .Setup(c => c.EnsureValidAsync(It.IsAny<string>(), It.IsAny<CommandClaims>(), It.IsAny<CancellationToken>()))
                .Callback<string, CommandClaims, CancellationToken>((verifier, commandClaims, cancellationToken) => cachedClaim.Add(commandClaims))
                .Returns(Task.CompletedTask);
            mockValidationServiceFactory.Setup(c => c.Create(It.IsAny<PcvEnvironment>())).Returns(mockValidationService.Object);
            Mock<IPrivacyConfigurationManager> mockConfig = new Mock<IPrivacyConfigurationManager>();
            mockConfig
                .SetupGet(c => c.AdaptersConfiguration.VerificationValidationServiceConfiguration.TargetMsaKeyDiscoveryEnvironment)
                .Returns(TargetMsaKeyDiscoveryEnvironment.MsaProd);
            mockConfig.SetupGet(c => c.AdaptersConfiguration.VerificationValidationServiceConfiguration.EnableVerificationCheckAad).Returns(true);
            mockConfig.SetupGet(c => c.AdaptersConfiguration.VerificationValidationServiceConfiguration.EnableVerificationCheckMsa).Returns(true);
            mockConfig.SetupGet(c => c.PrivacyExperienceServiceConfiguration.CloudInstance).Returns(CloudInstanceType.PublicProd);
            var verificationValidationService = new VerificationTokenValidationService(mockConfig.Object, new ConsoleLogger(), mockValidationServiceFactory.Object);

            var privacyRequest = new PrivacyRequest { Subject = new AadSubject2(), RequestType = requestType };

            // Act
            var validationResponse = await verificationValidationService.ValidateVerifierAsync(
                privacyRequest,
                verificationToken: "some value that is not empty or null").ConfigureAwait(false);

            // Assert
            Assert.IsTrue(validationResponse.IsSuccess);
            Assert.AreEqual(1, cachedClaim.Count);
            Assert.AreEqual(ValidOperation.AccountClose, cachedClaim[0].Operation);

            cachedClaim.Clear();
            privacyRequest = new PrivacyRequest { Subject = new AadSubject2() { TenantIdType = TenantIdType.Resource }, RequestType = requestType };

            // Act
            validationResponse = await verificationValidationService.ValidateVerifierAsync(
                privacyRequest,
                verificationToken: "some value that is not empty or null").ConfigureAwait(false);

            // Assert
            Assert.IsTrue(validationResponse.IsSuccess);
            Assert.AreEqual(1, cachedClaim.Count);
            Assert.AreEqual(ValidOperation.AccountCleanup, cachedClaim[0].Operation);
        }

        [DataRow(null, "", true)]
        [DataRow(null, "NotADataType", true)]
        [DataRow(null, null, true)]
        [DataRow("SearchRequestsAndQuery", "SearchRequestsAndQuery", true)]
        [DataRow(null, "SearchRequestsAndQuery", false)]
        [TestMethod]
        public void ShouldGetDataTypeFromDeleteRequest(string expectedPrivacyDatatType, string privacyDataType, bool isDeleteRequest)
        {
            // Arrange
            DataTypeId expectedDataTypeId = expectedPrivacyDatatType == null ? null : Policies.Current.DataTypes.Ids.SearchRequestsAndQuery;

            PrivacyRequest request = null;
            if (isDeleteRequest)
            {
                request = new DeleteRequest { PrivacyDataType = privacyDataType };
            }
            else
            {
                request = new ExportRequest();
            }

            // Act
            DataTypeId resultDataType = VerificationTokenValidationService.GetDataTypeIdFromPrivacyRequest(request);

            // Assert
            Assert.AreEqual(expectedDataTypeId, resultDataType);
        }

        [TestMethod]
        public async Task ShouldCatchNewRequestTypeBreaks()
        {
            // Catch any changes to RequestType that don't update the verification service
            IList<RequestType> registeredTypes = Enum.GetValues(typeof(RequestType)).Cast<RequestType>().Where(e => e != RequestType.None).ToList();
            foreach (var requestType in registeredTypes)
            {
                await ShouldHandleExceptionsAsUnknownAdapterError(requestType);
                await ValidateVerifierRejectsNullVerifier(requestType);
                await ShouldValidateAadVerifiersWhenConfigEnabled(requestType);
            }
        }
    }
}
