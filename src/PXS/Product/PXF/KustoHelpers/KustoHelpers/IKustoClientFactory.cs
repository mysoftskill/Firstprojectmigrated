// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.PrivacyServices.KustoHelpers
{
    /// <summary>
    ///     contact for objects that can create Kusto clients
    /// </summary>
    public interface IKustoClientFactory
    {
        /// <summary>
        ///     Creates a Kusto client
        /// </summary>
        /// <param name="clusterUrl">Kusto cluster</param>
        /// <param name="databaseName">Kusto database</param>
        /// <param name="queryTag">query tag to be used for tracing / logging purposes</param>
        /// <returns>resulting value</returns>
        IKustoClient CreateClient(
            string clusterUrl,
            string databaseName,
            string queryTag);
    }
}
