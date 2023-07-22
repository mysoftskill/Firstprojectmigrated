// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.Context
{
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    ///     contract for objects than create contexts
    /// </summary>
    public interface IContextFactory
    {
        /// <summary>
        ///     Creates a new context object
        /// </summary>
        /// <typeparam name="T">type of context object</typeparam>
        /// <param name="cancellationToken">cancellation token</param>
        /// <param name="isSimulation">true to is simulation; false otherwise</param>
        /// <param name="extensionProperties">extension properties</param>
        /// <param name="contextHostName">context host name</param>
        /// <returns>resulting value</returns>
        T Create<T>(
            CancellationToken cancellationToken,
            bool isSimulation,
            IDictionary<string, IDictionary<string, string>> extensionProperties,
            string contextHostName)
            where T : IContext;

        /// <summary>
        ///     Creates a new context object
        /// </summary>
        /// <typeparam name="T">type of context object</typeparam>
        /// <param name="contextHostName">context host name</param>
        /// <returns>resulting value</returns>
        T Create<T>(string contextHostName)
            where T : IContext;
    }
}
