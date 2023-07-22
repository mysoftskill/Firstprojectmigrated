//--------------------------------------------------------------------------------
// <copyright file="AuditOperation.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure
{
    /// <summary>
    ///     Supported audit logging operations.
    /// </summary>
    public static class AuditOperation
    {
        /// <summary>
        ///     Access to key vault operation, such as retrieving key, secret, certificate etc. from key vault.
        /// </summary>
        public const string AccessToKeyVault = "AccessToKeyVault";

        /// <summary>
        ///     Operation to create new data agent.
        /// </summary>
        public const string CreateDataAgent = "CreateDataAgent";
    }
}
