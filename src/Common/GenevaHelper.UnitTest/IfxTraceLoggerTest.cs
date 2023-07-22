//--------------------------------------------------------------------------------
// <copyright file="IfxTraceLoggerTest.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Geneva.UnitTests
{
    using System;

    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///     Test IfxTraceLogger class.
    /// </summary>
    [TestClass]
    public class IfxTraceLoggerTest
    {
        [TestMethod]
        public void ValidateIfxTraceLoggerInitialization()
        {
            IfxTraceLogger logger = IfxTraceLogger.Instance;
            Assert.IsNotNull(logger);
        }

        [DataTestMethod]
        [DataRow(IfxTracingLevel.Error)]
        [DataRow(IfxTracingLevel.Warning)]
        [DataRow(IfxTracingLevel.Informational)]
        [DataRow(IfxTracingLevel.Verbose)]
        public void ValidateIfxTraceMethods(IfxTracingLevel traceLevel)
        {
            IfxTraceLogger logger = IfxTraceLogger.Instance;
            try
            {
                switch(traceLevel)
                {
                    case IfxTracingLevel.Error:
                        logger.Error("foo", "bar", new string[] { "test" });
                        break;
                    case IfxTracingLevel.Warning:
                        logger.Warning("foo", new Exception(), "bar", new string[] { "test" });
                        break;
                    case IfxTracingLevel.Informational:
                        logger.Information("foo", new Exception(), "bar", new string[] { "test" });
                        break;
                    case IfxTracingLevel.Verbose:
                        logger.Verbose("foo", "bar", new string[] { "test" });
                        break;
                }
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }
    }
}
