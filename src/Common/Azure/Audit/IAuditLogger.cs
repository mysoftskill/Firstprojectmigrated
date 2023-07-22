//--------------------------------------------------------------------------------
// <copyright file="IAuditLogger.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure
{
    /// <summary>
    ///     Audit logger interface.
    /// </summary>
    public interface IAuditLogger
    {
        /// <summary>
        ///     Log audit data.
        /// </summary>
        /// <param name="auditData">Audit data.</param>
        /// <returns>If audit data is logged successfully.</returns>
        bool Log(AuditData auditData);
    }
}
