//--------------------------------------------------------------------------------
// <copyright file="ConsoleLoggerTest.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Geneva.UnitTests
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Test ConsoleLogger class.
    /// </summary>
    [TestClass]
    public class ConsoleLoggerTest
    {
        [DataTestMethod]
        [DataRow(IfxTracingLevel.Error)]
        [DataRow(IfxTracingLevel.Warning)]
        [DataRow(IfxTracingLevel.Informational)]
        [DataRow(IfxTracingLevel.Verbose)]
        public void ValidateConsoleLoggerMethods(IfxTracingLevel traceLevel)
        {
            ILogger logger = new ConsoleLogger();
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
