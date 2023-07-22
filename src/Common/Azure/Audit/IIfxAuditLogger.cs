//--------------------------------------------------------------------------------
// <copyright file="IIfxAuditLogger.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure
{
    using Ifx;
    using Microsoft.Cloud.InstrumentationFramework;

    /// <summary>
    ///     IIfxAudit logger interface.
    /// </summary>
    public interface IIfxAuditLogger
    {
        /// <summary>
        ///     Log application data.
        /// </summary>
        /// <param name="auditMandatoryProperties">Mandatory properties of audit data.</param>
        /// <param name="auditOptionalProperties">Optional properties of audit data.</param>
        bool LogApplicationAudit(
            AuditMandatoryProperties auditMandatoryProperties,
            AuditOptionalProperties auditOptionalProperties = null);

        /// <summary>
        ///     Log application data with custom information.
        /// </summary>
        /// <typeparam name="T">Type of AuditSchema.</typeparam>
        /// <param name="blob">Custom information.</param>
        /// <param name="auditMandatoryProperties">Mandatory properties of audit data.</param>
        /// <param name="auditOptionalProperties">Optional properties of audit data.</param>
        bool LogApplicationAudit<T>(
            T blob,
            AuditMandatoryProperties auditMandatoryProperties,
            AuditOptionalProperties auditOptionalProperties = null) where T : AuditSchema;

        /// <summary>
        ///     Log management data.
        /// </summary>
        /// <param name="auditMandatoryProperties">Mandatory properties of audit data.</param>
        /// <param name="auditOptionalProperties">Optional properties of audit data.</param>
        bool LogManagementAudit(
            AuditMandatoryProperties auditMandatoryProperties,
            AuditOptionalProperties auditOptionalProperties = null);

        /// <summary>
        ///     Log management data with custom information.
        /// </summary>
        /// <typeparam name="T">Type of AuditSchema.</typeparam>
        /// <param name="blob">Custom information.</param>
        /// <param name="auditMandatoryProperties">Mandatory properties of audit data.</param>
        /// <param name="auditOptionalProperties">Optional properties of audit data.</param>
        bool LogManagementAudit<T>(
            T blob,
            AuditMandatoryProperties auditMandatoryProperties,
            AuditOptionalProperties auditOptionalProperties = null) where T : AuditSchema;
    }
}
