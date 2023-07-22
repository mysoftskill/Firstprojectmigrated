//--------------------------------------------------------------------------------
// <copyright file="AuditLoggerFactory.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure
{
    /// <summary>
    ///     Create an instance of AuditLogger.
    /// </summary>
    public static class AuditLoggerFactory
    {
        /// <summary>
        ///     Create an instance of AuditLogger.
        /// </summary>
        /// <param name="traceLogger">Trace logger.</param>
        public static AuditLogger CreateAuditLogger(ILogger traceLogger)
        {
            var ifxAuditLogger = new IfxAuditLogger(traceLogger);
            return new AuditLogger(ifxAuditLogger, traceLogger);
        }
    }
}
