//--------------------------------------------------------------------------------
// <copyright file="AuditLoggerFactoryTest.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure.UnitTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///     Test AuditLoggerFactory class.
    /// </summary>
    [TestClass]
    public class AuditLoggerFactoryTest
    {
        [TestMethod]
        public void ValidateCreateAuditLogger()
        {
            var auditLogger = AuditLoggerFactory.CreateAuditLogger(IfxTraceLogger.Instance);
            Assert.IsNotNull(auditLogger);
        }
    }
}
