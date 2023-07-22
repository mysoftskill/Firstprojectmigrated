//--------------------------------------------------------------------------------
// <copyright file="TraceTagPrefixes.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure
{
    /// <summary>
    /// Enumeration of Ifx Trace Tag prefixes used across NGP compliance services.
    /// </summary>
    public enum TraceTagPrefixes
    {
        /// <summary>
        /// Geneal tag prefix.
        /// </summary>
        ADGCS,

        /// <summary>
        /// Tag prefix for PXS.
        /// </summary>
        ADGCS_PXS,

        /// <summary>
        /// Tag prefix for PCF.
        /// </summary>
        ADGCS_PCF,

        /// <summary>
        /// Tag prefix for PDMS.
        /// </summary>
        ADGCS_PDMS,

        /// <summary>
        /// Tag prefix for PCD.
        /// </summary>
        ADGCS_PCD,

        /// <summary>
        /// Tag prefix for PAF.
        /// </summary>
        ADGCS_PAF,
    }
}
