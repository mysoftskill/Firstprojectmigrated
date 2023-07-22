//--------------------------------------------------------------------------------
// <copyright file="IfxAuditLogger.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure
{
    using System;
    using Ifx;
    using Microsoft.Cloud.InstrumentationFramework;

    /// <summary>
    ///     IfxAudit logger to log application and management data.
    /// </summary>
    public class IfxAuditLogger : IIfxAuditLogger
    {
        private readonly ILogger traceLogger;

        /// <summary>
        ///     Initializes a new instance of the IfxAuditLogger class.
        /// </summary>
        /// <param name="traceLogger">Trace logger.</param>
        public IfxAuditLogger(ILogger traceLogger)
        {
            this.traceLogger = traceLogger ?? throw new ArgumentNullException(nameof(traceLogger));
        }

        /// <summary>
        ///     Log application data.
        /// </summary>
        /// <param name="auditMandatoryProperties">Mandatory properties of audit data.</param>
        /// <param name="auditOptionalProperties">Optional properties of audit data.</param>
        public bool LogApplicationAudit(
            AuditMandatoryProperties auditMandatoryProperties,
            AuditOptionalProperties auditOptionalProperties = null)
        {
            return this.LogAudit(
                AuditEventType.Application,
                () => IfxAudit.LogApplicationAudit(auditMandatoryProperties, auditOptionalProperties));
        }

        /// <summary>
        ///     Log application data with custom information.
        /// </summary>
        /// <typeparam name="T">Type of AuditSchema.</typeparam>
        /// <param name="blob">Custom information.</param>
        /// <param name="auditMandatoryProperties">Mandatory properties of audit data.</param>
        /// <param name="auditOptionalProperties">Optional properties of audit data.</param>
        public bool LogApplicationAudit<T>(
            T blob,
            AuditMandatoryProperties auditMandatoryProperties,
            AuditOptionalProperties auditOptionalProperties = null) where T : AuditSchema
        {
            return this.LogAudit(
                AuditEventType.Application,
                () => IfxAudit.LogExtendedApplicationAudit(blob, auditMandatoryProperties, auditOptionalProperties));
        }

        /// <summary>
        ///     Log management data.
        /// </summary>
        /// <param name="auditMandatoryProperties">Mandatory properties of audit data.</param>
        /// <param name="auditOptionalProperties">Optional properties of audit data.</param>
        public bool LogManagementAudit(
            AuditMandatoryProperties auditMandatoryProperties,
            AuditOptionalProperties auditOptionalProperties = null)
        {
            return this.LogAudit(
                AuditEventType.Management,
                () => IfxAudit.LogManagementAudit(auditMandatoryProperties, auditOptionalProperties));
        }

        /// <summary>
        ///     Log management data with custom information.
        /// </summary>
        /// <typeparam name="T">Type of AuditSchema.</typeparam>
        /// <param name="blob">Custom information.</param>
        /// <param name="auditMandatoryProperties">Mandatory properties of audit data.</param>
        /// <param name="auditOptionalProperties">Optional properties of audit data.</param>
        public bool LogManagementAudit<T>(
            T blob,
            AuditMandatoryProperties auditMandatoryProperties,
            AuditOptionalProperties auditOptionalProperties = null) where T : AuditSchema
        {
            return this.LogAudit(
                AuditEventType.Management,
                () => IfxAudit.LogExtendedManagementAudit(blob, auditMandatoryProperties, auditOptionalProperties));
        }

        private bool LogAudit(AuditEventType auditEventType, Func<bool> auditFunc)
        {
            try
            {
                bool auditLogSuccessful = auditFunc.Invoke();
                return auditLogSuccessful;
            }
            catch (Exception ex)
            {
                this.traceLogger.Error(nameof(IfxAuditLogger), $"AuditEventType: {auditEventType} Exception: {ex}");
                return false;
            }
        }
    }
}
