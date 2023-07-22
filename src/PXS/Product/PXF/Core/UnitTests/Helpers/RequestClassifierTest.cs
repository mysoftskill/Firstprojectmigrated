// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Principal;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    ///     RequestClassifierTest
    /// </summary>
    [TestClass]
    public class RequestClassifierTest
    {
        private readonly Mock<IPrivacyConfigurationManager> mockConfiguration = new Mock<IPrivacyConfigurationManager>(MockBehavior.Strict);

        private readonly Mock<IPrivacyExperienceServiceConfiguration> mockPrivacyExperienceServiceConfiguration =
            new Mock<IPrivacyExperienceServiceConfiguration>(MockBehavior.Strict);

        private readonly Mock<ITestRequestClassifierConfiguration> mockTestRequestClassifierConfiguration = new Mock<ITestRequestClassifierConfiguration>(MockBehavior.Strict);

        [TestInitialize]
        public void TestInitialize()
        {
            this.ResetMocks();
            this.mockTestRequestClassifierConfiguration.Setup(c => c.AllowedListAadObjectIds).Returns(new List<string>());
            this.mockTestRequestClassifierConfiguration.Setup(c => c.AllowedListAadTenantIds).Returns(new List<string>());
            this.mockTestRequestClassifierConfiguration.Setup(c => c.AllowedListMsaPuids).Returns(new List<long>());
            this.mockTestRequestClassifierConfiguration.Setup(c => c.CorrelationContextBaseOperationNames).Returns(new List<string>());
            this.mockPrivacyExperienceServiceConfiguration.Setup(c => c.TestRequestClassifierConfig).Returns(this.mockTestRequestClassifierConfiguration.Object);
            this.mockConfiguration.Setup(c => c.PrivacyExperienceServiceConfiguration).Returns(this.mockPrivacyExperienceServiceConfiguration.Object);
        }

        [TestMethod]
        public void ConstructorShouldAllowNoConfiguration()
        {
            this.ResetMocks();

            this.mockConfiguration.Setup(c => c.PrivacyExperienceServiceConfiguration).Returns((IPrivacyExperienceServiceConfiguration)null);
            RequestClassifier classifier = new RequestClassifier(this.mockConfiguration.Object);
            Assert.IsNotNull(classifier);

            this.ResetMocks();

            this.mockPrivacyExperienceServiceConfiguration.Setup(c => c.TestRequestClassifierConfig).Returns((ITestRequestClassifierConfiguration)null);
            this.mockConfiguration.Setup(c => c.PrivacyExperienceServiceConfiguration).Returns(this.mockPrivacyExperienceServiceConfiguration.Object);
            classifier = new RequestClassifier(this.mockConfiguration.Object);
            Assert.IsNotNull(classifier);

            this.ResetMocks();

            this.mockTestRequestClassifierConfiguration.Setup(c => c.AllowedListAadObjectIds).Returns(new List<string>());
            this.mockTestRequestClassifierConfiguration.Setup(c => c.AllowedListAadTenantIds).Returns(new List<string>());
            this.mockTestRequestClassifierConfiguration.Setup(c => c.AllowedListMsaPuids).Returns(new List<long>());
            this.mockTestRequestClassifierConfiguration.Setup(c => c.CorrelationContextBaseOperationNames).Returns(new List<string>());
            this.mockPrivacyExperienceServiceConfiguration.Setup(c => c.TestRequestClassifierConfig).Returns(this.mockTestRequestClassifierConfiguration.Object);
            this.mockConfiguration.Setup(c => c.PrivacyExperienceServiceConfiguration).Returns(this.mockPrivacyExperienceServiceConfiguration.Object);
            classifier = new RequestClassifier(this.mockConfiguration.Object);
            Assert.IsNotNull(classifier);
        }

        [TestMethod]
        [DataRow("something", "", false)]
        [DataRow("", "", false)]
        [DataRow("", "valueinconfig", false)]
        [DataRow("testvalue", "valueinconfig", false)]
        [DataRow("valueinconfig", "valueinconfig", true)]
        [DataRow("VALUEINCONFIG", "valueinconfig", true)]
        [DataRow("valueinconfig", "VALUEINCONFIG", true)]
        [DataRow("valueinconfig2", "valueinconfig1,valueinconfig2", true)]
        public void ShouldClassifyBasedOnCorrelationContextOperation(string correlationContextOperationName, string configuredCorrelationContextOperationNames, bool expectedResult)
        {
            this.mockTestRequestClassifierConfiguration
                .Setup(c => c.CorrelationContextBaseOperationNames)
                .Returns(new List<string>(configuredCorrelationContextOperationNames?.Split(',') ?? Enumerable.Empty<string>()));
            RequestClassifier classifier = new RequestClassifier(this.mockConfiguration.Object);
            var identity = new AadIdentity(Guid.NewGuid(), Guid.NewGuid(), null);

            bool result = classifier.IsTestRequest(Portals.AadAccountCloseEventSource, identity, correlationContextOperationName);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        [DataRow("83d544b8-581e-4995-8383-631a0bacf2bf", true)]
        [DataRow("a884fd2e-34e5-4cb7-990a-cc4bb853eb84", false)]
        public void ShouldClassifyCorrectlyForAadIdentityObjectId(string id, bool expectedResult)
        {
            this.mockTestRequestClassifierConfiguration.Setup(c => c.AllowedListAadObjectIds).Returns(new List<string> { "83d544b8-581e-4995-8383-631a0bacf2bf" });
            RequestClassifier classifier = new RequestClassifier(this.mockConfiguration.Object);

            var identity = new AadIdentity(Guid.Parse(id), Guid.NewGuid(), null);

            var result = classifier.IsTestRequest(Portals.AadAccountCloseEventSource, identity);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        [DataRow("3796d3d0-7f44-4021-ad41-a8ad286ffd16", true)]
        [DataRow("0beb205e-813b-4196-8636-c3d34c56cd1a", false)]
        public void ShouldClassifyCorrectlyForAadIdentityTenantId(string id, bool expectedResult)
        {
            this.mockTestRequestClassifierConfiguration.Setup(c => c.AllowedListAadTenantIds).Returns(new List<string> { "3796d3d0-7f44-4021-ad41-a8ad286ffd16" });
            RequestClassifier classifier = new RequestClassifier(this.mockConfiguration.Object);

            var identity = new AadIdentity(Guid.NewGuid(), Guid.Parse(id), null);

            var result = classifier.IsTestRequest(Portals.MsGraph, identity);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        [DataRow("f5af6e8a-c7b1-4db4-be15-a9097a51fdb0", true)]
        [DataRow("c53144ce-8677-41e8-bc12-dc4a62423cbc", false)]
        public void ShouldClassifyCorrectlyForAadIdentityWithMsaProxyTicketObjectId(string id, bool expectedResult)
        {
            this.mockTestRequestClassifierConfiguration.Setup(c => c.AllowedListAadObjectIds).Returns(new List<string> { "f5af6e8a-c7b1-4db4-be15-a9097a51fdb0" });
            RequestClassifier classifier = new RequestClassifier(this.mockConfiguration.Object);

            var identity = new AadIdentityWithMsaUserProxyTicket(
                Guid.NewGuid().ToString(),
                Guid.Parse(id),
                Guid.NewGuid(),
                string.Empty,
                string.Empty,
                0,
                string.Empty,
                null);

            var result = classifier.IsTestRequest(Portals.Amc, identity);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        [DataRow("a19e4a33-ded5-474a-a68a-ff70ae3e4ea8", true)]
        [DataRow("ecc62c76-dabe-4684-b0eb-fc138b9cfd64", false)]
        public void ShouldClassifyCorrectlyForAadIdentityWithMsaProxyTicketTenantId(string id, bool expectedResult)
        {
            this.mockTestRequestClassifierConfiguration.Setup(c => c.AllowedListAadTenantIds).Returns(new List<string> { "a19e4a33-ded5-474a-a68a-ff70ae3e4ea8" });
            RequestClassifier classifier = new RequestClassifier(this.mockConfiguration.Object);

            var identity = new AadIdentityWithMsaUserProxyTicket(
                Guid.NewGuid().ToString(),
                Guid.NewGuid(),
                Guid.Parse(id),
                string.Empty,
                string.Empty,
                0,
                string.Empty,
                null);

            var result = classifier.IsTestRequest(Portals.Amc, identity);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        [DataRow(0xAF346E532235225, true)]
        [DataRow(0xFEABC0236212478, false)]
        public void ShouldClassifyCorrectlyForMsaIdentity(long puid, bool expectedResult)
        {
            this.mockTestRequestClassifierConfiguration.Setup(c => c.AllowedListMsaPuids).Returns(new List<long> { 0xAF346E532235225 });
            RequestClassifier classifier = new RequestClassifier(this.mockConfiguration.Object);
            var identity = new MsaSelfIdentity(
                string.Empty,
                string.Empty,
                puid,
                0x0,
                null,
                "callerName",
                123,
                null,
                string.Empty,
                null,
                false,
                AuthType.MsaSelf,
                LegalAgeGroup.Undefined,
                null);

            bool result = classifier.IsTestRequest(Portals.Amc, identity);
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        [DataRow(Portals.AadAccountCloseEventSource, false)]
        [DataRow(Portals.Amc, false)]
        [DataRow(Portals.MsaAccountCloseEventSource, false)]
        [DataRow(Portals.MsGraph, false)]
        [DataRow(Portals.PartnerTestPage, true)]
        [DataRow(Portals.Pcd, false)]
        [DataRow(Portals.Unknown, false)]
        [DataRow(Portals.VortexDeviceDeleteSignal, false)]
        public void ShouldClassifyCorrectlyForPortal(string requestOriginPortal, bool expectedResult)
        {
            RequestClassifier classifier = new RequestClassifier(this.mockConfiguration.Object);
            bool result = classifier.IsTestRequest(requestOriginPortal, new GenericIdentity(string.Empty));
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void ShouldClassifyCorrectlyForUnknownIdentity()
        {
            RequestClassifier classifier = new RequestClassifier(this.mockConfiguration.Object);
            Assert.IsFalse(classifier.IsTestRequest(Portals.Unknown, new GenericIdentity(string.Empty)));
        }

        private void ResetMocks()
        {
            this.mockConfiguration.Reset();
            this.mockPrivacyExperienceServiceConfiguration.Reset();
            this.mockTestRequestClassifierConfiguration.Reset();
        }
    }
}
