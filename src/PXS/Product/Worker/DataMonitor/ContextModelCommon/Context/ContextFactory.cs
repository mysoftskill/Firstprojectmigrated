// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.Context
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.Practices.Unity;

    /// <summary>
    ///     creates context objects
    /// </summary>
    public class ContextFactory : IContextFactory
    {
        private readonly IUnityContainer container;

        /// <summary>
        ///     Initializes a new instance of the ContextFactory class
        /// </summary>
        public ContextFactory(IUnityContainer container)
        {
            this.container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>
        ///     Creates a new context object
        /// </summary>
        public T Create<T>(
            CancellationToken cancellationToken, 
            bool isSimulation,
            IDictionary<string, IDictionary<string, string>> extensionProperties,
            string contextHostName)
            where T : IContext
        {
            return this.container.Resolve<T>(
                new ParameterOverride("extensionProperties", extensionProperties),
                new ParameterOverride("cancellationToken", cancellationToken),
                new ParameterOverride("isSimulation", isSimulation),
                new ParameterOverride("contextHostName", contextHostName));
        }

        /// <summary>
        ///     Creates a new context object
        /// </summary>
        /// <typeparam name="T">type of context object</typeparam>
        /// <param name="contextHostName">context host name</param>
        /// <returns>resulting value</returns>
        public T Create<T>(string contextHostName)
            where T : IContext
        {
            return this.container.Resolve<T>(
                new ParameterOverride("extensionProperties", new Dictionary<string, IDictionary<string, string>>()),
                new ParameterOverride("cancellationToken", CancellationToken.None),
                new ParameterOverride("isSimulation", false),
                new ParameterOverride("contextHostName", contextHostName));
        }
    }
}
