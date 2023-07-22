//--------------------------------------------------------------------------------
// <copyright file="AuditLogger.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure
{
    using System;
    using Microsoft.Cloud.InstrumentationFramework;

    /// <summary>
    ///     Log audit data.
    /// </summary>
    public class AuditLogger : IAuditLogger
    {
        private readonly IIfxAuditLogger ifxAuditLogger;
        private readonly ILogger traceLogger;

        /// <summary>
        ///     Initializes a new instance of the AuditLogger class.
        /// </summary>
        /// <param name="ifxAuditLogger">Ifx audit logger.</param>
        /// <param name="traceLogger">Trace logger.</param>
        public AuditLogger(IIfxAuditLogger ifxAuditLogger, ILogger traceLogger)
        {
            this.ifxAuditLogger = ifxAuditLogger ?? throw new ArgumentNullException(nameof(ifxAuditLogger));
            this.traceLogger = traceLogger ?? throw new ArgumentNullException(nameof(traceLogger));
        }

        /// <summary>
        ///     Log audit data.
        /// </summary>
        /// <param name="auditData">Audit data.</param>
        /// <returns>If audit data is logged successfully.</returns>
        public bool Log(AuditData auditData)
        {
            AuditMandatoryProperties mandatoryProperties = auditData.CreateMandatoryProperties();
            AuditOptionalProperties optionalProperties = auditData.CreateOptionalProperties();

            switch (auditData.AuditEventType)
            {
                case AuditEventType.Application:
                    if (auditData.ExtendedProperties == null)
                    {
                        return this.ifxAuditLogger.LogApplicationAudit(mandatoryProperties, optionalProperties);
                    }

                    return this.ifxAuditLogger.LogApplicationAudit(auditData.ExtendedProperties, mandatoryProperties, optionalProperties);

                case AuditEventType.Management:
                    if (auditData.ExtendedProperties == null)
                    {
                        return this.ifxAuditLogger.LogManagementAudit(mandatoryProperties, optionalProperties);
                    }

                    return this.ifxAuditLogger.LogManagementAudit(auditData.ExtendedProperties, mandatoryProperties, optionalProperties);

                default:
                    this.traceLogger.Error(
                        nameof(AuditLogger),
                        $"IFxAudit: AuditData.AuditEventType is not a supported value, the allowed values are 'Application' and 'Management'. Actual value: '{auditData.AuditEventType}'");
                    return false;
            }
        }
    }
}
