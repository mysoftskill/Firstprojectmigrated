//--------------------------------------------------------------------------------
// <copyright file="AuditLoggerTest.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure.UnitTests
{
    using System;
    using System.Collections.Generic;
    using Ifx;
    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    /// <summary>
    ///     Test AuditLogger class.
    /// </summary>
    [TestClass]
    public class AuditLoggerTest
    {
        [DataTestMethod]
        [DataRow(AuditEventType.Application)]
        [DataRow(AuditEventType.Management)]
        public void ValidateLogAuditDataWithoutExtendedProperties(AuditEventType auditEventType)
        {
            var auditData = this.CreateAuditData(auditEventType, false);
            Mock<IIfxAuditLogger> mock = this.CreateIfxAuditLoggerMock(auditData);
            var auditLogger = new AuditLogger(mock.Object, IfxTraceLogger.Instance);

            bool result = auditLogger.Log(auditData);

            Assert.IsTrue(result, "AuditLogger.Log() should have successfully logged the audit data.");
            switch (auditEventType)
            {
                case AuditEventType.Application:
                    this.VerifyApplicationAudit(mock, false);
                    break;
                case AuditEventType.Management:
                    this.VerifyManagementAudit(mock, false);
                    break;
            }
        }

        [DataTestMethod]
        [DataRow(AuditEventType.Application)]
        [DataRow(AuditEventType.Management)]
        public void ValidateLogAuditDataWithExtendedProperties(AuditEventType auditEventType)
        {
            var auditData = this.CreateAuditData(auditEventType, true);
            Mock<IIfxAuditLogger> mock = this.CreateIfxAuditLoggerMock(auditData);
            var auditLogger = new AuditLogger(mock.Object, IfxTraceLogger.Instance);

            bool result = auditLogger.Log(auditData);

            Assert.IsTrue(result, "AuditLogger.Log() should have successfully logged the audit data.");
            switch (auditEventType)
            {
                case AuditEventType.Application:
                    this.VerifyApplicationAudit(mock, true);
                    break;
                case AuditEventType.Management:
                    this.VerifyManagementAudit(mock, true);
                    break;
            }
        }

        [TestMethod]
        public void VerifyLogAuditDataFailForUnknownAuditEventType()
        {
            var auditData = this.CreateAuditData(AuditEventType.Unknown, true);
            Mock<IIfxAuditLogger> mock = this.CreateIfxAuditLoggerMock(auditData);
            var auditLogger = new AuditLogger(mock.Object, IfxTraceLogger.Instance);

            bool result = auditLogger.Log(auditData);

            Assert.IsFalse(result, "AuditLogger.Log() should have failed for unknown event type.");
        }

        [TestMethod]
        public void ThrowsForNullIfxAuditLogger()
        {
            try
            {
                var logger = new AuditLogger(null, IfxTraceLogger.Instance);
                Assert.Fail("expected exception to be thrown");
            }
            catch (ArgumentNullException ex)
            {
                Assert.AreEqual("ifxAuditLogger", ex.ParamName);
            }
        }

        [TestMethod]
        public void ThrowsForNullTraceLogger()
        {
            try
            {
                var logger = new AuditLogger(Mock.Of<IIfxAuditLogger>(), null);
                Assert.Fail("expected exception to be thrown");
            }
            catch (ArgumentNullException ex)
            {
                Assert.AreEqual("traceLogger", ex.ParamName);
            }
        }

        private AuditData CreateAuditData(AuditEventType auditEventType, bool hasExtenedProperties)
        {
            var auditData = new AuditData(auditEventType)
            {
                OperationName = "Test Operation",
                ResultType = OperationResult.Failure,
                ResultDescription = "Operation failed",
                AuditCategories = new HashSet<AuditEventCategory>() { AuditEventCategory.Authentication },
                CallerIdentities = new List<CallerIdentity> { new CallerIdentity(CallerIdentityType.ApplicationID, "foo") },
                TargetResources = new List<TargetResource> {
                    new TargetResource("TestCert", "foo"),
                    new TargetResource("UserName", "bar") },
            };

            if (hasExtenedProperties)
            {
                auditData.ExtendedProperties = new DataAgentAudit
                {
                    Id = Guid.NewGuid().ToString(),
                    Capabilities = "Delete",
                    OwnerId = Guid.NewGuid().ToString()
                };
            }

            return auditData;
        }

        private Mock<IIfxAuditLogger> CreateIfxAuditLoggerMock(AuditData auditData)
        {
            var mock = new Mock<IIfxAuditLogger>();
            switch (auditData.AuditEventType)
            {
                case AuditEventType.Application:
                    if (auditData.ExtendedProperties == null)
                    {
                        mock.Setup(
                            m => m.LogApplicationAudit(
                                 It.IsAny<AuditMandatoryProperties>(),
                                 It.IsAny<AuditOptionalProperties>())).Returns(true);
                    }
                    else
                    {
                        mock.Setup(
                            m => m.LogApplicationAudit(
                                 It.IsAny<AuditSchema>(),
                                 It.IsAny<AuditMandatoryProperties>(),
                                 It.IsAny<AuditOptionalProperties>())).Returns(true);
                    }

                    break;

                case AuditEventType.Management:
                    if (auditData.ExtendedProperties == null)
                    {
                        mock.Setup(
                            m => m.LogManagementAudit(
                                 It.IsAny<AuditMandatoryProperties>(),
                                 It.IsAny<AuditOptionalProperties>())).Returns(true);
                    }
                    else
                    {
                        mock.Setup(
                            m => m.LogManagementAudit(
                                 It.IsAny<AuditSchema>(),
                                 It.IsAny<AuditMandatoryProperties>(),
                                 It.IsAny<AuditOptionalProperties>())).Returns(true);
                    }

                    break;
            }

            return mock;
        }

        private void VerifyApplicationAudit(Mock<IIfxAuditLogger> mock, bool hasExtenedProperties)
        {
            if (hasExtenedProperties)
            {
                mock.Verify(m => m.LogApplicationAudit(It.IsAny<AuditSchema>(), It.IsAny<AuditMandatoryProperties>(), It.IsAny<AuditOptionalProperties>()), Times.Once);
                mock.Verify(m => m.LogApplicationAudit(It.IsAny<AuditMandatoryProperties>(), It.IsAny<AuditOptionalProperties>()), Times.Never);
            }
            else
            {
                mock.Verify(m => m.LogApplicationAudit(It.IsAny<AuditMandatoryProperties>(), It.IsAny<AuditOptionalProperties>()), Times.Once);
                mock.Verify(m => m.LogApplicationAudit(It.IsAny<AuditSchema>(), It.IsAny<AuditMandatoryProperties>(), It.IsAny<AuditOptionalProperties>()), Times.Never);
            }

            mock.Verify(m => m.LogManagementAudit(It.IsAny<AuditSchema>(), It.IsAny<AuditMandatoryProperties>(), It.IsAny<AuditOptionalProperties>()), Times.Never);
            mock.Verify(m => m.LogManagementAudit(It.IsAny<AuditMandatoryProperties>(), It.IsAny<AuditOptionalProperties>()), Times.Never);
        }

        private void VerifyManagementAudit(Mock<IIfxAuditLogger> mock, bool hasExtenedProperties)
        {
            if (hasExtenedProperties)
            {
                mock.Verify(m => m.LogManagementAudit(It.IsAny<AuditSchema>(), It.IsAny<AuditMandatoryProperties>(), It.IsAny<AuditOptionalProperties>()), Times.Once);
                mock.Verify(m => m.LogManagementAudit(It.IsAny<AuditMandatoryProperties>(), It.IsAny<AuditOptionalProperties>()), Times.Never);
            }
            else
            {
                mock.Verify(m => m.LogManagementAudit(It.IsAny<AuditMandatoryProperties>(), It.IsAny<AuditOptionalProperties>()), Times.Once);
                mock.Verify(m => m.LogManagementAudit(It.IsAny<AuditSchema>(), It.IsAny<AuditMandatoryProperties>(), It.IsAny<AuditOptionalProperties>()), Times.Never);
            }

            mock.Verify(m => m.LogApplicationAudit(It.IsAny<AuditMandatoryProperties>(), It.IsAny<AuditOptionalProperties>()), Times.Never);
            mock.Verify(m => m.LogApplicationAudit(It.IsAny<AuditSchema>(), It.IsAny<AuditMandatoryProperties>(), It.IsAny<AuditOptionalProperties>()), Times.Never);
        }
    }
}