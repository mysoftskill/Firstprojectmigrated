//--------------------------------------------------------------------------------
// <copyright file="AuditEventType.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure
{
    /// <summary>
    ///     Enumeration of Ifx audit event types.
    /// </summary>
    public enum AuditEventType
    {
        /// <summary>
        ///     Unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     Application.
        /// </summary>
        Application = 1,

        /// <summary>
        ///     Management.
        /// </summary>
        Management = 2
    }
}
