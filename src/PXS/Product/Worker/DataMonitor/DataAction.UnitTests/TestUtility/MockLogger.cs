// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.DataAction.UnitTests
{
    using System;
    using System.Diagnostics;
    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.Membership.MemberServices.Common.Logging;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     MockLogger class
    /// </summary>
    /// <remarks>
    ///     The purpose of explicitly implementing this interface is to ensure we always evaluate format strings so that 
    ///      mistakes in them can be caught in unit testing instead of after deployment
    /// </remarks>
    public class MockLogger : ILogger
    {
        private readonly Mock<ILogger> mockLog = new Mock<ILogger>();

        public ILogger Object => this;

        public void Log(IfxTracingLevel traceLevel, string componentName, string message, params object[] args) =>
            this.ValidateFormatString(message, args);

        public void Error(string componentName, string message, params object[] args) =>
            this.ValidateFormatString(message, args);

        public void Error(string componentName, Exception exception, string message, params object[] args) =>
            this.ValidateFormatString(message, args);

        public void Warning(string componentName, string message, params object[] args) => 
            this.ValidateFormatString(message, args);

        public void Warning(string componentName, Exception exception, string message, params object[] args) =>
            this.ValidateFormatString(message, args);

        public void Information(string componentName, string message, params object[] args) => 
            this.ValidateFormatString(message, args);

        public void Information(string componentName, Exception exception, string message, params object[] args) =>
            this.ValidateFormatString(message, args);

        public void Verbose(string componentName, string message, params object[] args) =>
            this.ValidateFormatString(message, args);

        public void Verbose(string componentName, Exception exception, string message, params object[] args) =>
            this.ValidateFormatString(message, args);

        private void ValidateFormatString(
            string format,
            object[] args)
        {
            // output to the mock logger to ensure the compiler doesn't decide that the code can be tossed because the result is 
            //  never used
            string msg = string.Format(format, args); // lgtm[cs/uncontrolled-format-string] Suppressing warning because format is controlled internally.
            this.mockLog.Object.Verbose("test", msg);
        }
    }
}
