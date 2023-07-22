//--------------------------------------------------------------------------------
// <copyright file="LoggingHelperTest.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Geneva.UnitTests
{
    using System;

    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///     Test LoggingHelper class.
    /// </summary>
    [TestClass]
    public class LoggingHelperTest
    {
        [TestMethod]
        public void ValidateLoggingExtentionMethods()
        {
            try
            {
                var logger = IfxTraceLogger.Instance;
                logger.MethodEnter("component", "method");
                logger.MethodEnter("component", "method", "test");
                logger.MethodExit("component", "method");
                logger.MethodSuccess("component", "method", "test");
                logger.MethodException("component", "method", new Exception());
                logger.MethodWarning("component", "method", "test");
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }
    }
}
