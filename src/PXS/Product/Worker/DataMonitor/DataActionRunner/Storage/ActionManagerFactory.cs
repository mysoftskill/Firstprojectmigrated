// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Storage
{
    using System;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Practices.Unity;

    /// <summary>
    ///     action manager factory
    /// </summary>
    public class ActionManagerFactory : IActionManagerFactory
    {
        private readonly IUnityContainer rootContainer;

        /// <summary>
        ///     Initializes a new instance of the ActionManagerFactory class
        /// </summary>
        /// <param name="rootContainer">root container</param>
        public ActionManagerFactory(IUnityContainer rootContainer)
        {
            this.rootContainer = rootContainer ?? throw new ArgumentNullException(nameof(rootContainer));
        }

        /// <summary>
        ///     Creates a store manager instance
        /// </summary>
        /// <returns>resulting instance</returns>
        public IActionAccessor CreateStoreManager()
        {
            return this.rootContainer.Resolve<ActionManager>();
        }
    }
}
